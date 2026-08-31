using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Cadastro;
using OficinaDigital.Domain.Cadastro.Repositories;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Infrastructure.Persistencia.Repositorios;

public class ClienteRepository(OficinaDbContext ctx) : IClienteRepository
{
    public Task<Cliente?> ObterPorIdAsync(ClienteId id, CancellationToken ct = default) =>
        ctx.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cliente?> ObterPorDocumentoAsync(CpfCnpj documento, CancellationToken ct = default) =>
        ctx.Clientes.FirstOrDefaultAsync(c => c.Documento.Numero == documento.Numero, ct);

    public Task<bool> ExisteComDocumentoAsync(CpfCnpj documento, CancellationToken ct = default) =>
        ctx.Clientes.AnyAsync(c => c.Documento.Numero == documento.Numero, ct);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(string? filtroNome, bool? ativo, int pagina,
        int tamanhoPagina, CancellationToken ct = default) =>
        await Filtrar(filtroNome, ativo)
            .OrderBy(c => c.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

    public Task<int> ContarAsync(string? filtroNome, bool? ativo, CancellationToken ct = default) =>
        Filtrar(filtroNome, ativo).CountAsync(ct);

    public async Task AdicionarAsync(Cliente cliente, CancellationToken ct = default) =>
        await ctx.Clientes.AddAsync(cliente, ct);

    public void Remover(Cliente cliente) => ctx.Clientes.Remove(cliente);

    private IQueryable<Cliente> Filtrar(string? filtroNome, bool? ativo)
    {
        var consulta = ctx.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtroNome))
            consulta = consulta.Where(c => EF.Functions.Like(c.Nome, $"%{filtroNome.Trim()}%"));

        if (ativo is not null)
            consulta = consulta.Where(c => c.Ativo == ativo);

        return consulta;
    }
}

public class VeiculoRepository(OficinaDbContext ctx) : IVeiculoRepository
{
    public Task<Veiculo?> ObterPorIdAsync(VeiculoId id, CancellationToken ct = default) =>
        ctx.Veiculos.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Veiculo?> ObterPorPlacaAsync(Placa placa, CancellationToken ct = default) =>
        ctx.Veiculos.FirstOrDefaultAsync(v => v.Placa.Valor == placa.Valor, ct);

    public Task<bool> ExisteComPlacaAsync(Placa placa, CancellationToken ct = default) =>
        ctx.Veiculos.AnyAsync(v => v.Placa.Valor == placa.Valor, ct);

    public async Task<IReadOnlyList<Veiculo>> ListarPorClienteAsync(ClienteId clienteId,
        CancellationToken ct = default) =>
        await ctx.Veiculos.Where(v => v.ClienteId == clienteId).OrderBy(v => v.Modelo).ToListAsync(ct);

    public async Task<IReadOnlyList<Veiculo>> ListarAsync(int pagina, int tamanhoPagina,
        CancellationToken ct = default) =>
        await ctx.Veiculos
            .OrderBy(v => v.Marca).ThenBy(v => v.Modelo)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

    public Task<int> ContarAsync(CancellationToken ct = default) => ctx.Veiculos.CountAsync(ct);

    public async Task AdicionarAsync(Veiculo veiculo, CancellationToken ct = default) =>
        await ctx.Veiculos.AddAsync(veiculo, ct);

    public void Remover(Veiculo veiculo) => ctx.Veiculos.Remove(veiculo);
}
