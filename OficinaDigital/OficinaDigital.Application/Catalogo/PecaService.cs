using FluentValidation;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Common.Exceptions;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Application.Catalogo;

public sealed class PecaService(
    IPecaRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<CriarPecaRequest> criarValidator,
    IValidator<AtualizarPecaRequest> atualizarValidator,
    IValidator<ReporEstoqueRequest> reporEstoqueValidator) : IPecaService
{
    public async Task<PecaDto> CriarAsync(CriarPecaRequest request, CancellationToken cancellationToken = default)
    {
        await criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var peca = Peca.Criar(request.Nome, request.Preco, request.QuantidadeEmEstoque);

        await repository.AdicionarAsync(peca, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(peca);
    }

    public async Task<PecaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ParaDto(await ObterOuFalharAsync(id, cancellationToken));

    public async Task<IReadOnlyList<PecaDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var pecas = await repository.ListarAsync(cancellationToken);
        return pecas.Select(ParaDto).ToList();
    }

    public async Task<PecaDto> AtualizarAsync(Guid id, AtualizarPecaRequest request, CancellationToken cancellationToken = default)
    {
        await atualizarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var peca = await ObterOuFalharAsync(id, cancellationToken);
        peca.AtualizarDados(request.Nome, request.Preco);

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(peca);
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var peca = await ObterOuFalharAsync(id, cancellationToken);

        repository.Remover(peca);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<PecaDto> ReporEstoqueAsync(Guid id, ReporEstoqueRequest request, CancellationToken cancellationToken = default)
    {
        await reporEstoqueValidator.ValidateAndThrowAsync(request, cancellationToken);

        var peca = await ObterOuFalharAsync(id, cancellationToken);
        peca.ReporEstoque(request.Quantidade);

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(peca);
    }

    private async Task<Peca> ObterOuFalharAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.ObterPorIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Peça '{id}' não encontrada.");

    private static PecaDto ParaDto(Peca peca) =>
        new(peca.Id, peca.Nome, peca.Preco.Valor, peca.QuantidadeEmEstoque);
}
