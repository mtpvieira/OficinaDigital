using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Infrastructure.Persistence.Repositories;

public sealed class ServicoRepository(OficinaDigitalDbContext context) : IServicoRepository
{
    public Task<Servico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Servicos.FirstOrDefaultAsync(servico => servico.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Servico>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.Servicos.ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Servico servico, CancellationToken cancellationToken = default) =>
        await context.Servicos.AddAsync(servico, cancellationToken);

    public void Remover(Servico servico) => context.Servicos.Remove(servico);
}
