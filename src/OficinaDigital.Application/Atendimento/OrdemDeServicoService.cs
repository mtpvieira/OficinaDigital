using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Atendimento;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Cadastro.Repositories;
using OficinaDigital.Domain.Catalogo.Repositories;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.Repositories;

namespace OficinaDigital.Application.Atendimento;

public class OrdemDeServicoService(
    IOrdemDeServicoRepository ordens,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    IServicoRepository servicos,
    IPecaRepository pecas,
    IOrcamentoRepository orcamentos,
    IUnitOfWork uow)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<OrdemDeServicoResponse> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        OrdemDeServicoResponse.De(await CarregarAsync(id, ct));

    public async Task<OrdemDeServicoResponse> ObterPorNumeroAsync(long numero, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorNumeroAsync(numero, ct)
                 ?? throw new RecursoNaoEncontradoException("Ordem de serviço", numero);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<ResultadoPaginado<OrdemDeServicoResumoResponse>> ListarAsync(
        StatusOS? status, Guid? clienteId, Guid? veiculoId, Paginacao paginacao, CancellationToken ct = default)
    {
        var p = paginacao.Normalizar();

        var cliente = clienteId is null ? (ClienteId?)null : new ClienteId(clienteId.Value);
        var veiculo = veiculoId is null ? (VeiculoId?)null : new VeiculoId(veiculoId.Value);

        var itens = await ordens.ListarAsync(status, cliente, veiculo, p.Pagina, p.TamanhoPagina, ct);
        var total = await ordens.ContarAsync(status, cliente, veiculo, ct);

        return new ResultadoPaginado<OrdemDeServicoResumoResponse>(
            itens.Select(OrdemDeServicoResumoResponse.De).ToList(), p.Pagina, p.TamanhoPagina, total);
    }

    public async Task<IReadOnlyDictionary<string, int>> ContarPorStatusAsync(CancellationToken ct = default)
    {
        var contagem = await ordens.ContarPorStatusAsync(ct);

        return Enum.GetValues<StatusOS>()
            .ToDictionary(
                s => OrdemDeServicoResponse.DescreverStatus(s),
                s => contagem.TryGetValue(s, out var q) ? q : 0);
    }

    public async Task<OrdemDeServicoResponse> AbrirAsync(AbrirOsRequest request, CancellationToken ct = default)
    {
        var clienteId = new ClienteId(request.ClienteId);
        var veiculoId = new VeiculoId(request.VeiculoId);

        var cliente = await clientes.ObterPorIdAsync(clienteId, ct)
                      ?? throw new RecursoNaoEncontradoException("Cliente", request.ClienteId);

        if (!cliente.Ativo)
            throw new ConflitoDeNegocioException("Não é possível abrir OS para um cliente inativo.");

        var veiculo = await veiculos.ObterPorIdAsync(veiculoId, ct)
                      ?? throw new RecursoNaoEncontradoException("Veículo", request.VeiculoId);

        if (veiculo.ClienteId != clienteId)
            throw new ConflitoDeNegocioException(
                $"O veículo {veiculo.Placa.Valor} não pertence ao cliente informado.");

        var osAtiva = await ordens.ObterAtivaPorVeiculoAsync(veiculoId, ct);
        if (osAtiva is not null)
            throw new ConflitoDeNegocioException(
                $"O veículo {veiculo.Placa.Valor} já possui a OS {osAtiva.Numero} ativa " +
                $"(status {OrdemDeServicoResponse.DescreverStatus(osAtiva.Status)}).");

        var os = OrdemDeServico.Abrir(clienteId, veiculoId, request.DescricaoDoProblema);

        await ordens.AdicionarAsync(os, ct);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> IniciarDiagnosticoAsync(Guid id, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.IniciarDiagnostico();
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> RegistrarServicosAsync(Guid id, RegistrarServicosRequest request,
        CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);
        var itens = await ResolverServicosAsync(request.Servicos, ct);

        os.RegistrarServicos(itens);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> RemoverServicoAsync(Guid id, Guid itemId,
        CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.RemoverServico(itemId);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> RegistrarPecasAsync(Guid id, RegistrarPecasRequest request,
        CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);
        var itens = await ResolverPecasAsync(request.Pecas, ct);

        os.RegistrarPecas(itens);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(await CarregarAsync(id, ct));
    }

    public async Task<OrdemDeServicoResponse> RemoverPecaAsync(Guid id, Guid itemId, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.RemoverPeca(itemId);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> FinalizarDiagnosticoAsync(Guid id, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.FinalizarDiagnostico();
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(await CarregarAsync(id, ct));
    }

    public async Task<OrdemDeServicoResponse> IniciarServicoAsync(Guid id, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        await GarantirOrcamentoAprovadoEVigenteAsync(os, ct);

        os.IniciarServico();
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(await CarregarAsync(id, ct));
    }

    public async Task<OrdemDeServicoResponse> RegistrarProblemaAdicionalAsync(Guid id,
        RegistrarProblemaAdicionalRequest request, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.RegistrarProblemaAdicional(request.Descricao);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> RegistrarItensAdicionaisAsync(Guid id,
        RegistrarItensAdicionaisRequest request, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        var itensServico = await ResolverServicosAsync(request.Servicos ?? [], ct);
        var itensPeca = await ResolverPecasAsync(request.Pecas ?? [], ct);

        os.RegistrarItensAdicionais(itensServico, itensPeca);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(await CarregarAsync(id, ct));
    }

    public async Task<OrdemDeServicoResponse> FinalizarServicoAsync(Guid id, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.FinalizarServico();
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> RegistrarEntregaAsync(Guid id, CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.RegistrarEntrega();
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    public async Task<OrdemDeServicoResponse> CancelarAsync(Guid id, CancelarOsRequest request,
        CancellationToken ct = default)
    {
        var os = await CarregarAsync(id, ct);

        os.Cancelar(request.Motivo, request.Observacao);
        await uow.CommitAsync(ct);

        return OrdemDeServicoResponse.De(os);
    }

    private async Task<OrdemDeServico> CarregarAsync(Guid id, CancellationToken ct) =>
        await ordens.ObterPorIdAsync(new OrdemDeServicoId(id), ct)
        ?? throw new RecursoNaoEncontradoException("Ordem de serviço", id);

    private async Task GarantirOrcamentoAprovadoEVigenteAsync(OrdemDeServico os, CancellationToken ct)
    {
        if (os.OrcamentoVigenteId is null)
            throw new ConflitoDeNegocioException("Esta OS ainda não possui orçamento.");

        var orcamento = await orcamentos.ObterPorIdAsync(os.OrcamentoVigenteId.Value, ct)
                        ?? throw new RecursoNaoEncontradoException("Orçamento", os.OrcamentoVigenteId.Value.Valor);

        var versao = orcamento.ObterVersaoVigente();

        if (versao.Resposta?.Aprovou != true)
            throw new ConflitoDeNegocioException("O serviço só inicia com o orçamento aprovado pelo cliente.");
    }

    private async Task<List<(ServicoId, string, Dinheiro, int)>> ResolverServicosAsync(
        IReadOnlyList<ItemDeServicoRequest> pedidos, CancellationToken ct)
    {
        if (pedidos.Count == 0) return [];

        var ids = pedidos.Select(s => new ServicoId(s.ServicoId)).ToList();
        var encontrados = (await servicos.ObterPorIdsAsync(ids, ct)).ToDictionary(s => s.Id);

        var itens = new List<(ServicoId, string, Dinheiro, int)>();

        foreach (var id in ids)
        {
            if (!encontrados.TryGetValue(id, out var servico))
                throw new RecursoNaoEncontradoException("Serviço", id.Valor);

            if (!servico.Ativo)
                throw new ConflitoDeNegocioException(
                    $"O serviço {servico.Nome} está inativo e não entra em novos diagnósticos.");

            itens.Add((servico.Id, servico.Nome, servico.PrecoVigenteEm(Hoje),
                servico.TempoPadraoExecucao.Minutos));
        }

        return itens;
    }

    private async Task<List<(PecaId, string, decimal, Dinheiro)>> ResolverPecasAsync(
        IReadOnlyList<ItemDePecaRequest> pedidos, CancellationToken ct)
    {
        if (pedidos.Count == 0) return [];

        var ids = pedidos.Select(p => new PecaId(p.PecaId)).ToList();
        var encontradas = (await pecas.ObterPorIdsAsync(ids, ct)).ToDictionary(p => p.Id);

        var itens = new List<(PecaId, string, decimal, Dinheiro)>();

        foreach (var pedido in pedidos)
        {
            var pecaId = new PecaId(pedido.PecaId);

            if (!encontradas.TryGetValue(pecaId, out var peca))
                throw new RecursoNaoEncontradoException("Peça", pedido.PecaId);

            if (!peca.Ativa)
                throw new ConflitoDeNegocioException(
                    $"A peça {peca.Sku.Codigo} está inativa e não entra em novos orçamentos.");

            itens.Add((peca.Id, $"{peca.Sku.Codigo} - {peca.Descricao}", pedido.Quantidade,
                peca.PrecoVigenteEm(Hoje)));
        }

        return itens;
    }
}
