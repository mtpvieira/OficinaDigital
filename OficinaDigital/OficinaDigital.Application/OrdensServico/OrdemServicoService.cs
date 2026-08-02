using FluentValidation;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Common.Exceptions;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Clientes;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.OrdensServico;
using OficinaDigital.Domain.Veiculos;

namespace OficinaDigital.Application.OrdensServico;

public sealed class OrdemServicoService(
    IOrdemServicoRepository repository,
    IClienteRepository clienteRepository,
    IVeiculoRepository veiculoRepository,
    IServicoRepository servicoRepository,
    IPecaRepository pecaRepository,
    IUnitOfWork unitOfWork,
    IValidator<CriarOrdemServicoRequest> criarValidator,
    IValidator<AprovarPublicoRequest> aprovarPublicoValidator) : IOrdemServicoService
{
    public async Task<OrdemServicoDto> CriarAsync(CriarOrdemServicoRequest request, CancellationToken cancellationToken = default)
    {
        await criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken)
            ?? throw new NotFoundException($"Cliente '{request.ClienteId}' não encontrado.");

        var veiculo = await veiculoRepository.ObterPorIdAsync(request.VeiculoId, cancellationToken)
            ?? throw new NotFoundException($"Veículo '{request.VeiculoId}' não encontrado.");

        if (veiculo.ClienteId != request.ClienteId)
            throw new DomainException("O veículo informado não pertence ao cliente informado.");

        var numero = await GerarProximoNumeroAsync(cancellationToken);
        var ordem = OrdemDeServico.Criar(numero, request.ClienteId, request.VeiculoId);

        foreach (var itemServico in request.Servicos)
        {
            var servico = await servicoRepository.ObterPorIdAsync(itemServico.ServicoId, cancellationToken)
                ?? throw new NotFoundException($"Serviço '{itemServico.ServicoId}' não encontrado.");

            ordem.AdicionarItemServico(servico.Id, servico.Nome, servico.PrecoBase);
        }

        foreach (var itemPeca in request.Pecas)
        {
            var peca = await pecaRepository.ObterPorIdAsync(itemPeca.PecaId, cancellationToken)
                ?? throw new NotFoundException($"Peça '{itemPeca.PecaId}' não encontrada.");

            ordem.AdicionarItemPeca(peca.Id, peca.Nome, itemPeca.Quantidade, peca.Preco);
        }

        await repository.AdicionarAsync(ordem, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(ordem);
    }

    public async Task<OrdemServicoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ParaDto(await ObterOuFalharAsync(id, cancellationToken));

    public async Task<IReadOnlyList<OrdemServicoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var ordens = await repository.ListarAsync(cancellationToken);
        return ordens.Select(ParaDto).ToList();
    }

    public async Task<OrdemServicoDto> IniciarDiagnosticoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ordem = await ObterOuFalharAsync(id, cancellationToken);
        ordem.IniciarDiagnostico();

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ParaDto(ordem);
    }

    public async Task<OrdemServicoDto> EnviarOrcamentoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ordem = await ObterOuFalharAsync(id, cancellationToken);
        ordem.EnviarOrcamento();

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ParaDto(ordem);
    }

    public async Task<OrdemServicoDto> AprovarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ordem = await ObterOuFalharAsync(id, cancellationToken);
        await AprovarEBaixarEstoqueAsync(ordem, cancellationToken);

        return ParaDto(ordem);
    }

    public async Task<OrdemServicoDto> ConcluirExecucaoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ordem = await ObterOuFalharAsync(id, cancellationToken);
        ordem.ConcluirExecucao();

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ParaDto(ordem);
    }

    public async Task<OrdemServicoDto> EntregarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ordem = await ObterOuFalharAsync(id, cancellationToken);
        ordem.Entregar();

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ParaDto(ordem);
    }

    public async Task<TempoMedioExecucaoDto> ObterTempoMedioExecucaoAsync(CancellationToken cancellationToken = default)
    {
        var finalizadas = await repository.ListarFinalizadasAsync(cancellationToken);

        var tempos = finalizadas
            .Where(ordem => ordem.TempoDeExecucao.HasValue)
            .Select(ordem => ordem.TempoDeExecucao!.Value.TotalMinutes)
            .ToList();

        var media = tempos.Count > 0 ? tempos.Average() : (double?)null;

        return new TempoMedioExecucaoDto(media, tempos.Count);
    }

    public async Task<OrdemServicoDto> ConsultarPublicoAsync(string numero, string documento, CancellationToken cancellationToken = default)
    {
        var ordem = await ObterPorNumeroOuFalharAsync(numero, cancellationToken);
        await ValidarDocumentoDoClienteAsync(ordem, documento, cancellationToken);

        return ParaDto(ordem);
    }

    public async Task<OrdemServicoDto> AprovarPublicoAsync(string numero, AprovarPublicoRequest request, CancellationToken cancellationToken = default)
    {
        await aprovarPublicoValidator.ValidateAndThrowAsync(request, cancellationToken);

        var ordem = await ObterPorNumeroOuFalharAsync(numero, cancellationToken);
        await ValidarDocumentoDoClienteAsync(ordem, request.Documento, cancellationToken);

        await AprovarEBaixarEstoqueAsync(ordem, cancellationToken);

        return ParaDto(ordem);
    }

    private async Task AprovarEBaixarEstoqueAsync(OrdemDeServico ordem, CancellationToken cancellationToken)
    {
        ordem.Aprovar();

        foreach (var itemPeca in ordem.ItensPeca)
        {
            var peca = await pecaRepository.ObterPorIdAsync(itemPeca.PecaId, cancellationToken)
                ?? throw new NotFoundException($"Peça '{itemPeca.PecaId}' não encontrada.");

            peca.BaixarEstoque(itemPeca.Quantidade);
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<string> GerarProximoNumeroAsync(CancellationToken cancellationToken)
    {
        var quantidadeExistente = (await repository.ListarAsync(cancellationToken)).Count;
        return $"OS-{quantidadeExistente + 1:D4}";
    }

    private async Task<OrdemDeServico> ObterOuFalharAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.ObterPorIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Ordem de serviço '{id}' não encontrada.");

    private async Task<OrdemDeServico> ObterPorNumeroOuFalharAsync(string numero, CancellationToken cancellationToken) =>
        await repository.ObterPorNumeroAsync(numero, cancellationToken)
        ?? throw new NotFoundException($"Ordem de serviço '{numero}' não encontrada.");

    private async Task ValidarDocumentoDoClienteAsync(OrdemDeServico ordem, string documento, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(ordem.ClienteId, cancellationToken);
        var digitosInformados = new string(documento.Where(char.IsDigit).ToArray());

        if (cliente is null || cliente.Documento.Numero != digitosInformados)
            throw new NotFoundException("Ordem de serviço não encontrada para os dados informados.");
    }

    private static OrdemServicoDto ParaDto(OrdemDeServico ordem) => new(
        ordem.Id,
        ordem.Numero,
        ordem.ClienteId,
        ordem.VeiculoId,
        ordem.Status.ToString(),
        ordem.Orcamento.Valor,
        ordem.ItensServico.Select(item => new ItemServicoDto(item.ServicoId, item.Descricao, item.Preco.Valor)).ToList(),
        ordem.ItensPeca.Select(item => new ItemPecaDto(item.PecaId, item.Descricao, item.Quantidade, item.PrecoUnitario.Valor, item.Subtotal.Valor)).ToList(),
        ordem.CriadaEm,
        ordem.EnviadaAprovacaoEm,
        ordem.AprovadaEm,
        ordem.ExecucaoIniciadaEm,
        ordem.FinalizadaEm,
        ordem.EntregueEm);
}
