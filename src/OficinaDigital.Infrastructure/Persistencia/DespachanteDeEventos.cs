using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Infrastructure.Persistencia;

public class DespachanteDeEventos(IServiceProvider provedor, ILogger<DespachanteDeEventos> logger)
    : IDomainEventDispatcher
{
    public async Task DespacharAsync(IReadOnlyCollection<IDomainEvent> eventos, CancellationToken ct = default)
    {
        foreach (var evento in eventos)
        {
            var tipoPolitica = typeof(IPoliticaDeDominio<>).MakeGenericType(evento.GetType());
            var politicas = provedor.GetServices(tipoPolitica).Where(p => p is not null).ToList();

            if (politicas.Count == 0)
            {
                logger.LogDebug("Evento {Evento} publicado sem política associada.", evento.GetType().Name);
                continue;
            }

            foreach (var politica in politicas)
            {
                var metodo = tipoPolitica.GetMethod(nameof(IPoliticaDeDominio<IDomainEvent>.ExecutarAsync))!;

                logger.LogDebug("Executando política {Politica} para o evento {Evento}.",
                    politica!.GetType().Name, evento.GetType().Name);

                await (Task)metodo.Invoke(politica, [evento, ct])!;
            }
        }
    }
}
