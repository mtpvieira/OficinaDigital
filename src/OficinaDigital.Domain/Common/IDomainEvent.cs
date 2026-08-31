namespace OficinaDigital.Domain.Common;

public interface IDomainEvent
{
    Guid EventoId { get; }
    DateTime OcorridoEm { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventoId { get; } = Guid.NewGuid();
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
