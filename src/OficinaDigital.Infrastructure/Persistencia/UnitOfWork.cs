using Microsoft.EntityFrameworkCore;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Infrastructure.Persistencia;

public class UnitOfWork(OficinaDbContext contexto, IDomainEventDispatcher despachante) : IUnitOfWork
{
    private bool _despachando;

    private const int MaximoDeRodadas = 10;

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        if (_despachando)
            return await contexto.SaveChangesAsync(ct);

        var afetados = await contexto.SaveChangesAsync(ct);

        _despachando = true;
        try
        {
            var rodada = 0;
            var eventos = ColherEventos();

            while (eventos.Count > 0)
            {
                if (++rodada > MaximoDeRodadas)
                    throw new InvalidOperationException(
                        $"O encadeamento de políticas passou de {MaximoDeRodadas} rodadas sem estabilizar. " +
                        $"Eventos pendentes: {string.Join(", ", eventos.Select(e => e.GetType().Name).Distinct())}.");

                await despachante.DespacharAsync(eventos, ct);
                afetados += await contexto.SaveChangesAsync(ct);
                eventos = ColherEventos();
            }
        }
        finally
        {
            _despachando = false;
        }

        return afetados;
    }

    private List<IDomainEvent> ColherEventos()
    {
        var agregados = contexto.ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<IPublicadorDeEventos>()
            .Where(a => a.EventosDeDominio.Count > 0)
            .ToList();

        var eventos = agregados.SelectMany(a => a.EventosDeDominio).ToList();

        foreach (var agregado in agregados)
            agregado.LimparEventos();

        return eventos;
    }
}
