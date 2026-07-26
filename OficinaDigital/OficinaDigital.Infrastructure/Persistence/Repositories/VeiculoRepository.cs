using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Veiculos;

namespace OficinaDigital.Infrastructure.Persistence.Repositories;

public sealed class VeiculoRepository(OficinaDigitalDbContext context) : IVeiculoRepository
{
    public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Veiculos.FirstOrDefaultAsync(veiculo => veiculo.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Veiculo>> ListarPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
        await context.Veiculos.Where(veiculo => veiculo.ClienteId == clienteId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.Veiculos.ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken = default) =>
        await context.Veiculos.AddAsync(veiculo, cancellationToken);

    public void Remover(Veiculo veiculo) => context.Veiculos.Remove(veiculo);
}
