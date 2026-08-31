namespace OficinaDigital.Domain.Common;

public sealed class PrecoVigente : ValueObject
{
    public Dinheiro Valor { get; }
    public DateOnly VigenteDe { get; }

    private PrecoVigente(Dinheiro valor, DateOnly vigenteDe)
    {
        Valor = valor;
        VigenteDe = vigenteDe;
    }

    public static PrecoVigente De(Dinheiro valor, DateOnly vigenteDe)
    {
        DomainException.Se(valor.Valor <= 0, "Preço deve ser maior que zero.");
        return new PrecoVigente(valor, vigenteDe);
    }

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Valor;
        yield return VigenteDe;
    }
}
