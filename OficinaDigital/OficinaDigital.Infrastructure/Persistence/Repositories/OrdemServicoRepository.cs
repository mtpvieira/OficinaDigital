using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.OrdensServico;

namespace OficinaDigital.Infrastructure.Persistence.Repositories;

public sealed class OrdemServicoRepository(OficinaDigitalDbContext context) : IOrdemServicoRepository
{
    public Task<OrdemDeServico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.OrdensServico.FirstOrDefaultAsync(ordem => ordem.Id == id, cancellationToken);

    public Task<OrdemDeServico?> ObterPorNumeroAsync(string numero, CancellationToken cancellationToken = default) =>
        context.OrdensServico.FirstOrDefaultAsync(ordem => ordem.Numero == numero, cancellationToken);

    public async Task<IReadOnlyList<OrdemDeServico>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.OrdensServico.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrdemDeServico>> ListarFinalizadasAsync(CancellationToken cancellationToken = default) =>
        await context.OrdensServico.Where(ordem => ordem.FinalizadaEm != null).ToListAsync(cancellationToken);

    public async Task AdicionarAsync(OrdemDeServico ordemDeServico, CancellationToken cancellationToken = default) =>
        await context.OrdensServico.AddAsync(ordemDeServico, cancellationToken);
}
