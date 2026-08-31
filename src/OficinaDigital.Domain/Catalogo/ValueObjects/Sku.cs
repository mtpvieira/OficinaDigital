using System.Text.RegularExpressions;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Catalogo.ValueObjects;

public sealed partial class Sku : ValueObject
{
    public string Codigo { get; }

    private Sku(string codigo) => Codigo = codigo;

    public static Sku Criar(string? entrada)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(entrada), "SKU é obrigatório.");

        var normalizado = entrada!.Trim().ToUpperInvariant();

        DomainException.Se(!FormatoValido().IsMatch(normalizado),
            "SKU deve ter de 3 a 30 caracteres, contendo apenas letras, números, hífen ou ponto.");

        return new Sku(normalizado);
    }

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Codigo;
    }

    public override string ToString() => Codigo;

    [GeneratedRegex(@"^[A-Z0-9\.\-]{3,30}$")]
    private static partial Regex FormatoValido();
}
