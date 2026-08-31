using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Identidade;
using OficinaDigital.Domain.Identidade.Repositories;
using OficinaDigital.Domain.Orcamentos;
using OficinaDigital.Domain.Orcamentos.Repositories;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Infrastructure.Persistencia.Repositorios;

public class OrcamentoRepository(OficinaDbContext ctx) : IOrcamentoRepository
{
    private IQueryable<Orcamento> Completo => ctx.Orcamentos
        .Include(o => o.Versoes)
        .ThenInclude(v => v.Itens);

    public Task<Orcamento?> ObterPorIdAsync(OrcamentoId id, CancellationToken ct = default) =>
        Completo.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Orcamento>> ListarPorOsAsync(OrdemDeServicoId ordemDeServicoId,
        CancellationToken ct = default) =>
        await Completo.Where(o => o.OrdemDeServicoId == ordemDeServicoId).ToListAsync(ct);

    public Task<Orcamento?> ObterPrincipalDaOsAsync(OrdemDeServicoId ordemDeServicoId,
        CancellationToken ct = default) =>
        Completo.FirstOrDefaultAsync(
            o => o.OrdemDeServicoId == ordemDeServicoId && o.Tipo == TipoOrcamento.PRINCIPAL, ct);

    public async Task AdicionarAsync(Orcamento orcamento, CancellationToken ct = default) =>
        await ctx.Orcamentos.AddAsync(orcamento, ct);
}

public class UsuarioRepository(OficinaDbContext ctx) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken ct = default) =>
        ctx.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default) =>
        ctx.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExisteComEmailAsync(string email, CancellationToken ct = default) =>
        ctx.Usuarios.AnyAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
        await ctx.Usuarios.OrderBy(u => u.Nome).ToListAsync(ct);

    public async Task AdicionarAsync(Usuario usuario, CancellationToken ct = default) =>
        await ctx.Usuarios.AddAsync(usuario, ct);
}
