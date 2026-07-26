using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Infrastructure.Persistence.Repositories;

public sealed class PecaRepository(OficinaDigitalDbContext context) : IPecaRepository
{
    public Task<Peca?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Pecas.FirstOrDefaultAsync(peca => peca.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Peca>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.Pecas.ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Peca peca, CancellationToken cancellationToken = default) =>
        await context.Pecas.AddAsync(peca, cancellationToken);

    public void Remover(Peca peca) => context.Pecas.Remove(peca);
}
