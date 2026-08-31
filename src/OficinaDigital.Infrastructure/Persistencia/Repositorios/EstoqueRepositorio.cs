using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Estoque;
using OficinaDigital.Domain.Estoque.Repositories;

namespace OficinaDigital.Infrastructure.Persistencia.Repositorios;

public class ItemEstoqueRepository(OficinaDbContext ctx) : IItemEstoqueRepository
{
    private IQueryable<ItemEstoque> Completo => ctx.ItensDeEstoque
        .Include(i => i.Reservas)
        .Include(i => i.Movimentos);

    public Task<ItemEstoque?> ObterPorIdAsync(ItemEstoqueId id, CancellationToken ct = default) =>
        Completo.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<ItemEstoque?> ObterPorPecaAsync(PecaId pecaId, CancellationToken ct = default) =>
        Completo.FirstOrDefaultAsync(i => i.PecaId == pecaId, ct);

    public async Task<IReadOnlyList<ItemEstoque>> ObterPorPecasAsync(IEnumerable<PecaId> pecaIds,
        CancellationToken ct = default)
    {
        var lista = pecaIds.Distinct().ToList();
        if (lista.Count == 0) return [];

        return await Completo.Where(i => lista.Contains(i.PecaId)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ItemEstoque>> ObterComReservaDaOsAsync(OrdemDeServicoId ordemDeServicoId,
        CancellationToken ct = default) =>
        await Completo
            .Where(i => i.Reservas.Any(r => r.OrdemDeServicoId == ordemDeServicoId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ItemEstoque>> ListarAsync(int pagina, int tamanhoPagina,
        CancellationToken ct = default) =>
        await Completo
            .OrderBy(i => i.PecaId)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

    public Task<int> ContarAsync(CancellationToken ct = default) => ctx.ItensDeEstoque.CountAsync(ct);

    public async Task<IReadOnlyList<ItemEstoque>> ListarAbaixoDoMinimoAsync(CancellationToken ct = default) =>
        await Completo
            .Where(i => EF.Property<decimal>(i, "_saldo") - EF.Property<decimal>(i, "_reservado")
                        < EF.Property<decimal>(i, "_estoqueMinimo"))
            .OrderBy(i => i.PecaId)
            .ToListAsync(ct);

    public async Task AdicionarAsync(ItemEstoque item, CancellationToken ct = default) =>
        await ctx.ItensDeEstoque.AddAsync(item, ct);
}
