using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.OrdensServico;

public sealed class Money : ValueObject
{
    public const string MoedaPadrao = "BRL";

    public decimal Valor { get; }
    public string Moeda { get; }

    private Money(decimal valor, string moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public static Money Zero => new(0m, MoedaPadrao);

    public static Money Criar(decimal valor)
    {
        if (valor < 0)
            throw new DomainException("Valor monetário não pode ser negativo.");

        return new Money(valor, MoedaPadrao);
    }

    public static Money operator +(Money left, Money right)
    {
        GarantirMesmaMoeda(left, right);
        return new Money(left.Valor + right.Valor, left.Moeda);
    }

    public static Money operator *(Money money, int quantidade)
    {
        if (quantidade < 0)
            throw new DomainException("Quantidade não pode ser negativa.");

        return new Money(money.Valor * quantidade, money.Moeda);
    }

    private static void GarantirMesmaMoeda(Money left, Money right)
    {
        if (left.Moeda != right.Moeda)
            throw new DomainException("Não é possível somar valores em moedas diferentes.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
        yield return Moeda;
    }

    public override string ToString() => $"{Moeda} {Valor:F2}";
}
