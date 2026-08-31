namespace OficinaDigital.Domain.Common;

public sealed class Duracao : ValueObject
{
    public int Minutos { get; }

    private Duracao(int minutos) => Minutos = minutos;

    public static Duracao DeMinutos(int minutos)
    {
        DomainException.Se(minutos <= 0, "Duração deve ser maior que zero minutos.");
        return new Duracao(minutos);
    }

    public static Duracao DeHoras(decimal horas) => DeMinutos((int)Math.Round(horas * 60));

    protected override IEnumerable<object?> ComponentesDeIgualdade() { yield return Minutos; }

    public override string ToString() => $"{Minutos} min";
}
