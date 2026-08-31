using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Catalogo.Repositories;
using OficinaDigital.Domain.Catalogo.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Infrastructure.Persistencia.Repositorios;

public class ServicoRepository(OficinaDbContext ctx) : IServicoRepository
{
    public Task<Servico?> ObterPorIdAsync(ServicoId id, CancellationToken ct = default) =>
        ctx.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Servico>> ObterPorIdsAsync(IEnumerable<ServicoId> ids,
        CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return [];

        return await ctx.Servicos.Where(s => lista.Contains(s.Id)).ToListAsync(ct);
    }

    public Task<bool> ExisteComNomeAsync(string nome, ServicoId? exceto = null, CancellationToken ct = default) =>
        ctx.Servicos.AnyAsync(s => s.Nome == nome && (exceto == null || s.Id != exceto), ct);

    public async Task<IReadOnlyList<Servico>> ListarAsync(bool? ativo, int pagina, int tamanhoPagina,
        CancellationToken ct = default) =>
        await Filtrar(ativo)
            .OrderBy(s => s.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

    public Task<int> ContarAsync(bool? ativo, CancellationToken ct = default) => Filtrar(ativo).CountAsync(ct);

    public async Task AdicionarAsync(Servico servico, CancellationToken ct = default) =>
        await ctx.Servicos.AddAsync(servico, ct);

    private IQueryable<Servico> Filtrar(bool? ativo) =>
        ativo is null ? ctx.Servicos : ctx.Servicos.Where(s => s.Ativo == ativo);
}

public class PecaRepository(OficinaDbContext ctx) : IPecaRepository
{
    public Task<Peca?> ObterPorIdAsync(PecaId id, CancellationToken ct = default) =>
        ctx.Pecas.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Peca>> ObterPorIdsAsync(IEnumerable<PecaId> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return [];

        return await ctx.Pecas.Where(p => lista.Contains(p.Id)).ToListAsync(ct);
    }

    public Task<bool> ExisteComSkuAsync(Sku sku, CancellationToken ct = default) =>
        ctx.Pecas.AnyAsync(p => p.Sku == sku, ct);

    public async Task<IReadOnlyList<Peca>> ListarAsync(bool? ativa, int pagina, int tamanhoPagina,
        CancellationToken ct = default) =>
        await Filtrar(ativa)
            .OrderBy(p => p.Descricao)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

    public Task<int> ContarAsync(bool? ativa, CancellationToken ct = default) => Filtrar(ativa).CountAsync(ct);

    public async Task AdicionarAsync(Peca peca, CancellationToken ct = default) =>
        await ctx.Pecas.AddAsync(peca, ct);

    private IQueryable<Peca> Filtrar(bool? ativa) =>
        ativa is null ? ctx.Pecas : ctx.Pecas.Where(p => p.Ativa == ativa);
}
