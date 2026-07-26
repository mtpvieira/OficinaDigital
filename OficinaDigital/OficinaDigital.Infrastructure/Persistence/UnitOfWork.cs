using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Infrastructure.Persistence;

public sealed class UnitOfWork(OficinaDigitalDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        DespacharEventosDeDominio();
        return context.SaveChangesAsync(cancellationToken);
    }

    private void DespacharEventosDeDominio()
    {
        var agregadosComEventos = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in agregadosComEventos)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                logger.LogInformation(
                    "Evento de domínio despachado: {EventoTipo} ocorrido em {OcorridoEm}",
                    domainEvent.GetType().Name,
                    domainEvent.OcorridoEm);
            }

            aggregate.ClearDomainEvents();
        }
    }
}
