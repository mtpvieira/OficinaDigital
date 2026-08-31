using System.Text.RegularExpressions;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro.ValueObjects;

public enum PadraoPlaca
{
    ANTIGO = 1,
    MERCOSUL = 2
}

public sealed partial class Placa : ValueObject
{
    public string Valor { get; }
    public PadraoPlaca Padrao { get; }

    private Placa(string valor, PadraoPlaca padrao)
    {
        Valor = valor;
        Padrao = padrao;
    }

    public static Placa Criar(string? entrada)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(entrada), "Placa é obrigatória.");

        var normalizada = NaoAlfanumerico().Replace(entrada!, string.Empty).ToUpperInvariant();

        if (PadraoAntigo().IsMatch(normalizada)) return new Placa(normalizada, PadraoPlaca.ANTIGO);
        if (PadraoMercosul().IsMatch(normalizada)) return new Placa(normalizada, PadraoPlaca.MERCOSUL);

        throw new DomainException(
            $"Placa {entrada} inválida. Use o padrão antigo (AAA0000) ou Mercosul (AAA0A00).");
    }

    public static bool EhValida(string? entrada)
    {
        try
        {
            Criar(entrada);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;

    [GeneratedRegex(@"[^A-Za-z0-9]")]
    private static partial Regex NaoAlfanumerico();

    [GeneratedRegex(@"^[A-Z]{3}[0-9]{4}$")]
    private static partial Regex PadraoAntigo();

    [GeneratedRegex(@"^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex PadraoMercosul();
}
