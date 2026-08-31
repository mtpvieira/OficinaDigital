using System.Globalization;

namespace OficinaDigital.Domain.Common;

public sealed class Dinheiro : ValueObject
{
    public const string MoedaPadrao = "BRL";

    public decimal Valor { get; }
    public string Moeda { get; }

    private Dinheiro(decimal valor, string moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public static Dinheiro Zero => new(0m, MoedaPadrao);

    public static Dinheiro De(decimal valor, string moeda = MoedaPadrao)
    {
        DomainException.Se(valor < 0, "Valor monetário não pode ser negativo.");
        DomainException.Se(string.IsNullOrWhiteSpace(moeda), "Moeda é obrigatória.");
        return new Dinheiro(Math.Round(valor, 2, MidpointRounding.AwayFromZero), moeda.ToUpperInvariant());
    }

    public Dinheiro Somar(Dinheiro outro)
    {
        GarantirMesmaMoeda(outro);
        return De(Valor + outro.Valor, Moeda);
    }

    public Dinheiro Subtrair(Dinheiro outro)
    {
        GarantirMesmaMoeda(outro);
        DomainException.Se(outro.Valor > Valor, "Subtração resultaria em valor monetário negativo.");
        return De(Valor - outro.Valor, Moeda);
    }

    public Dinheiro Multiplicar(decimal fator)
    {
        DomainException.Se(fator < 0, "Fator de multiplicação não pode ser negativo.");
        return De(Valor * fator, Moeda);
    }

    public bool MaiorQue(Dinheiro outro)
    {
        GarantirMesmaMoeda(outro);
        return Valor > outro.Valor;
    }

    private void GarantirMesmaMoeda(Dinheiro outro) =>
        DomainException.Se(Moeda != outro.Moeda, $"Não é possível operar moedas diferentes ({Moeda} e {outro.Moeda}).");

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Valor;
        yield return Moeda;
    }

    public override string ToString() => Valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
}
