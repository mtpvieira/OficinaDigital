namespace OficinaDigital.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> ComponentesDeIgualdade();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return ComponentesDeIgualdade().SequenceEqual(((ValueObject)obj).ComponentesDeIgualdade());
    }

    public override int GetHashCode() =>
        ComponentesDeIgualdade().Aggregate(new HashCode(), (h, c) => { h.Add(c); return h; }).ToHashCode();

    public static bool operator ==(ValueObject? a, ValueObject? b) => Equals(a, b);
    public static bool operator !=(ValueObject? a, ValueObject? b) => !Equals(a, b);
}
