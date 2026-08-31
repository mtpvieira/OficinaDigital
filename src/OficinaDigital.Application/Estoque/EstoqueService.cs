using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Catalogo.Repositories;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Estoque;
using OficinaDigital.Domain.Estoque.Repositories;

namespace OficinaDigital.Application.Estoque;

public class EstoqueService(
    IItemEstoqueRepository itens,
    IPecaRepository pecas,
    IUnitOfWork uow)
{
    public async Task<ResultadoPaginado<ItemEstoqueResponse>> ListarAsync(Paginacao paginacao,
        CancellationToken ct = default)
    {
        var p = paginacao.Normalizar();

        var lista = await itens.ListarAsync(p.Pagina, p.TamanhoPagina, ct);
        var total = await itens.ContarAsync(ct);

        return new ResultadoPaginado<ItemEstoqueResponse>(
            await MapearAsync(lista, ct), p.Pagina, p.TamanhoPagina, total);
    }

    public async Task<IReadOnlyList<ItemEstoqueResponse>> ListarAlertasDeEstoqueMinimoAsync(
        CancellationToken ct = default)
    {
        var lista = await itens.ListarAbaixoDoMinimoAsync(ct);
        return await MapearAsync(lista, ct);
    }

    public async Task<ItemEstoqueDetalhadoResponse> ObterPorPecaAsync(Guid pecaId, CancellationToken ct = default)
    {
        var item = await itens.ObterPorPecaAsync(new PecaId(pecaId), ct)
                   ?? throw new RecursoNaoEncontradoException("Posição de estoque da peça", pecaId);

        var peca = await pecas.ObterPorIdAsync(item.PecaId, ct)
                   ?? throw new RecursoNaoEncontradoException("Peça", pecaId);

        return new ItemEstoqueDetalhadoResponse(
            ItemEstoqueResponse.De(item, peca.Sku.Codigo, peca.Descricao),
            item.Reservas
                .Select(r => new ReservaDto(r.OrdemDeServicoId.Valor, r.Quantidade, r.ReservadaEm))
                .ToList(),
            item.Movimentos
                .OrderByDescending(m => m.RegistradoEm)
                .Select(m => new MovimentoDto(m.Tipo, m.Quantidade, m.SaldoResultante,
                    m.OrdemDeServicoId?.Valor, m.Motivo, m.RegistradoEm))
                .ToList());
    }

    public async Task<IReadOnlyList<DisponibilidadeDto>> ConsultarDisponibilidadeAsync(
        IReadOnlyDictionary<Guid, decimal> quantidadesPorPeca, CancellationToken ct = default)
    {
        var pecaIds = quantidadesPorPeca.Keys.Select(id => new PecaId(id)).ToList();

        var itensEstoque = (await itens.ObterPorPecasAsync(pecaIds, ct)).ToDictionary(i => i.PecaId);
        var catalogo = (await pecas.ObterPorIdsAsync(pecaIds, ct)).ToDictionary(p => p.Id);

        var resultado = new List<DisponibilidadeDto>();

        foreach (var (id, quantidade) in quantidadesPorPeca)
        {
            var pecaId = new PecaId(id);

            if (!catalogo.TryGetValue(pecaId, out var peca))
                throw new RecursoNaoEncontradoException("Peça", id);

            var disponivel = itensEstoque.TryGetValue(pecaId, out var item) ? item.Disponivel.Valor : 0m;

            resultado.Add(new DisponibilidadeDto(id, peca.Sku.Codigo, peca.Descricao,
                quantidade, disponivel, disponivel >= quantidade));
        }

        return resultado;
    }

    public async Task<ItemEstoqueResponse> RegistrarEntradaAsync(Guid pecaId, RegistrarEntradaRequest request,
        CancellationToken ct = default)
    {
        var item = await ObterOuAbrirPosicaoAsync(new PecaId(pecaId), ct);

        item.RegistrarEntrada(request.Quantidade, Dinheiro.De(request.CustoUnitario));

        await uow.CommitAsync(ct);
        return await MapearUmAsync(item, ct);
    }

    public async Task<ItemEstoqueResponse> DefinirEstoqueMinimoAsync(Guid pecaId,
        DefinirEstoqueMinimoRequest request, CancellationToken ct = default)
    {
        var item = await ObterOuAbrirPosicaoAsync(new PecaId(pecaId), ct);

        item.DefinirEstoqueMinimo(request.EstoqueMinimo);

        await uow.CommitAsync(ct);
        return await MapearUmAsync(item, ct);
    }

    private async Task<ItemEstoque> ObterOuAbrirPosicaoAsync(PecaId pecaId, CancellationToken ct)
    {
        var item = await itens.ObterPorPecaAsync(pecaId, ct);
        if (item is not null) return item;

        var peca = await pecas.ObterPorIdAsync(pecaId, ct)
                   ?? throw new RecursoNaoEncontradoException("Peça", pecaId.Valor);

        item = ItemEstoque.Abrir(peca.Id, peca.Unidade, 0m);
        await itens.AdicionarAsync(item, ct);

        return item;
    }

    private async Task<ItemEstoqueResponse> MapearUmAsync(ItemEstoque item, CancellationToken ct)
    {
        var peca = await pecas.ObterPorIdAsync(item.PecaId, ct);
        return ItemEstoqueResponse.De(item, peca?.Sku.Codigo ?? "?", peca?.Descricao ?? "?");
    }

    private async Task<IReadOnlyList<ItemEstoqueResponse>> MapearAsync(IReadOnlyList<ItemEstoque> lista,
        CancellationToken ct)
    {
        if (lista.Count == 0) return [];

        var catalogo = (await pecas.ObterPorIdsAsync(lista.Select(i => i.PecaId), ct))
            .ToDictionary(p => p.Id);

        return lista
            .Select(i =>
            {
                catalogo.TryGetValue(i.PecaId, out var peca);
                return ItemEstoqueResponse.De(i, peca?.Sku.Codigo ?? "?", peca?.Descricao ?? "?");
            })
            .ToList();
    }
}
