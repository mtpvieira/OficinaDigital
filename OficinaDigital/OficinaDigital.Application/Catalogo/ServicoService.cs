using FluentValidation;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Common.Exceptions;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Application.Catalogo;

public sealed class ServicoService(
    IServicoRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<CriarServicoRequest> criarValidator,
    IValidator<AtualizarServicoRequest> atualizarValidator) : IServicoService
{
    public async Task<ServicoDto> CriarAsync(CriarServicoRequest request, CancellationToken cancellationToken = default)
    {
        await criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var servico = Servico.Criar(request.Nome, request.PrecoBase, request.Descricao, request.TempoEstimadoMinutos);

        await repository.AdicionarAsync(servico, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(servico);
    }

    public async Task<ServicoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ParaDto(await ObterOuFalharAsync(id, cancellationToken));

    public async Task<IReadOnlyList<ServicoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var servicos = await repository.ListarAsync(cancellationToken);
        return servicos.Select(ParaDto).ToList();
    }

    public async Task<ServicoDto> AtualizarAsync(Guid id, AtualizarServicoRequest request, CancellationToken cancellationToken = default)
    {
        await atualizarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var servico = await ObterOuFalharAsync(id, cancellationToken);
        servico.AtualizarDados(request.Nome, request.PrecoBase, request.Descricao, request.TempoEstimadoMinutos);

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(servico);
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var servico = await ObterOuFalharAsync(id, cancellationToken);

        repository.Remover(servico);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Servico> ObterOuFalharAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.ObterPorIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Serviço '{id}' não encontrado.");

    private static ServicoDto ParaDto(Servico servico) =>
        new(servico.Id, servico.Nome, servico.Descricao, servico.PrecoBase.Valor, servico.TempoEstimadoMinutos);
}
