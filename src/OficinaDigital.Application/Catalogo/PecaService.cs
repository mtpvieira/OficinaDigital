using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Catalogo.Repositories;
using OficinaDigital.Domain.Catalogo.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Application.Catalogo;

public class PecaService(IPecaRepository pecas, IUnitOfWork uow)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<PecaResponse> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var peca = await pecas.ObterPorIdAsync(new PecaId(id), ct)
                   ?? throw new RecursoNaoEncontradoException("Peça", id);

        return PecaResponse.De(peca, Hoje);
    }

    public async Task<ResultadoPaginado<PecaResponse>> ListarAsync(bool? ativa, Paginacao paginacao,
        CancellationToken ct = default)
    {
        var p = paginacao.Normalizar();

        var itens = await pecas.ListarAsync(ativa, p.Pagina, p.TamanhoPagina, ct);
        var total = await pecas.ContarAsync(ativa, ct);

        return new ResultadoPaginado<PecaResponse>(
            itens.Select(x => PecaResponse.De(x, Hoje)).ToList(), p.Pagina, p.TamanhoPagina, total);
    }

    public async Task<PecaResponse> CadastrarAsync(CadastrarPecaRequest request, CancellationToken ct = default)
    {
        var sku = Sku.Criar(request.Sku);

        if (await pecas.ExisteComSkuAsync(sku, ct))
            throw new ConflitoDeNegocioException($"Já existe uma peça com o SKU {sku.Codigo} no catálogo.");

        var peca = Peca.Cadastrar(request.Sku, request.Descricao, request.Unidade,
            Dinheiro.De(request.PrecoVenda), request.EstoqueMinimo, Hoje);

        await pecas.AdicionarAsync(peca, ct);
        await uow.CommitAsync(ct);

        return PecaResponse.De(peca, Hoje);
    }

    public async Task<PecaResponse> AlterarAsync(Guid id, AlterarPecaRequest request, CancellationToken ct = default)
    {
        var peca = await pecas.ObterPorIdAsync(new PecaId(id), ct)
                   ?? throw new RecursoNaoEncontradoException("Peça", id);

        peca.AlterarDados(request.Descricao);

        await uow.CommitAsync(ct);
        return PecaResponse.De(peca, Hoje);
    }

    public async Task<PecaResponse> AlterarPrecoAsync(Guid id, AlterarPrecoRequest request,
        CancellationToken ct = default)
    {
        var peca = await pecas.ObterPorIdAsync(new PecaId(id), ct)
                   ?? throw new RecursoNaoEncontradoException("Peça", id);

        peca.AlterarPreco(Dinheiro.De(request.Preco), request.VigenteDe ?? Hoje);

        await uow.CommitAsync(ct);
        return PecaResponse.De(peca, Hoje);
    }

    public async Task InativarAsync(Guid id, CancellationToken ct = default)
    {
        var peca = await pecas.ObterPorIdAsync(new PecaId(id), ct)
                   ?? throw new RecursoNaoEncontradoException("Peça", id);

        peca.Inativar();
        await uow.CommitAsync(ct);
    }
}
