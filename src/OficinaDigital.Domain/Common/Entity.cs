namespace OficinaDigital.Domain.Common;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> outra && outra.GetType() == GetType() && outra.Id.Equals(Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

public interface IPublicadorDeEventos
{
    IReadOnlyCollection<IDomainEvent> EventosDeDominio { get; }
    void LimparEventos();
}

public abstract class AggregateRoot<TId> : Entity<TId>, IPublicadorDeEventos where TId : struct
{
    private readonly List<IDomainEvent> _eventos = [];

    public IReadOnlyCollection<IDomainEvent> EventosDeDominio => _eventos.AsReadOnly();

    protected void Publicar(IDomainEvent evento) => _eventos.Add(evento);

    public void LimparEventos() => _eventos.Clear();
}
