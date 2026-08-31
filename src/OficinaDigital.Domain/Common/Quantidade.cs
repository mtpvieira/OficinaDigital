namespace OficinaDigital.Domain.Common;

public sealed class Quantidade : ValueObject
{
    public decimal Valor { get; }
    public UnidadeDeMedida Unidade { get; }

    private Quantidade(decimal valor, UnidadeDeMedida unidade)
    {
        Valor = valor;
        Unidade = unidade;
    }

    public static Quantidade Zero(UnidadeDeMedida unidade) => new(0m, unidade);

    public static Quantidade De(decimal valor, UnidadeDeMedida unidade)
    {
        DomainException.Se(valor < 0, "Quantidade não pode ser negativa.");
        return new Quantidade(Math.Round(valor, 3, MidpointRounding.AwayFromZero), unidade);
    }

    public Quantidade Somar(Quantidade outra)
    {
        GarantirMesmaUnidade(outra);
        return De(Valor + outra.Valor, Unidade);
    }

    public Quantidade Subtrair(Quantidade outra)
    {
        GarantirMesmaUnidade(outra);
        DomainException.Se(outra.Valor > Valor, "Quantidade resultante não pode ser negativa.");
        return De(Valor - outra.Valor, Unidade);
    }

    public bool MaiorQue(Quantidade outra)
    {
        GarantirMesmaUnidade(outra);
        return Valor > outra.Valor;
    }

    public bool EhZero => Valor == 0m;

    private void GarantirMesmaUnidade(Quantidade outra) =>
        DomainException.Se(Unidade != outra.Unidade,
            $"Não é possível operar quantidades de unidades diferentes ({Unidade} e {outra.Unidade}).");

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Valor;
        yield return Unidade;
    }

    public override string ToString() => $"{Valor:0.###} {Unidade}";
}
