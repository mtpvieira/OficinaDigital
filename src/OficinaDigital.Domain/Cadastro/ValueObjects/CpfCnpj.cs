using System.Text.RegularExpressions;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro.ValueObjects;

public sealed partial class CpfCnpj : ValueObject
{
    public string Numero { get; }
    public TipoPessoa Tipo { get; }

    private CpfCnpj(string numero, TipoPessoa tipo)
    {
        Numero = numero;
        Tipo = tipo;
    }

    public static CpfCnpj Criar(string? entrada)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(entrada), "CPF/CNPJ é obrigatório.");

        var digitos = ApenasDigitos().Replace(entrada!, string.Empty);

        return digitos.Length switch
        {
            11 when CpfValido(digitos) => new CpfCnpj(digitos, TipoPessoa.PF),
            14 when CnpjValido(digitos) => new CpfCnpj(digitos, TipoPessoa.PJ),
            11 or 14 => throw new DomainException($"CPF/CNPJ {entrada} possui dígito verificador inválido."),
            _ => throw new DomainException($"CPF/CNPJ {entrada} deve conter 11 (CPF) ou 14 (CNPJ) dígitos.")
        };
    }

    public static bool EhValido(string? entrada)
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

    private static bool CpfValido(string cpf)
    {
        if (cpf.Distinct().Count() == 1) return false;

        var pesos1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var pesos2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var dv1 = CalcularDigito(cpf[..9], pesos1);
        var dv2 = CalcularDigito(cpf[..9] + dv1, pesos2);

        return cpf.EndsWith($"{dv1}{dv2}", StringComparison.Ordinal);
    }

    private static bool CnpjValido(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1) return false;

        var pesos1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var pesos2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var dv1 = CalcularDigito(cnpj[..12], pesos1);
        var dv2 = CalcularDigito(cnpj[..12] + dv1, pesos2);

        return cnpj.EndsWith($"{dv1}{dv2}", StringComparison.Ordinal);
    }

    private static int CalcularDigito(string baseNumerica, int[] pesos)
    {
        var soma = baseNumerica.Select((c, i) => (c - '0') * pesos[i]).Sum();
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public string Formatado() => Tipo == TipoPessoa.PF
        ? Convert.ToUInt64(Numero).ToString(@"000\.000\.000\-00")
        : Convert.ToUInt64(Numero).ToString(@"00\.000\.000\/0000\-00");

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Numero;
    }

    public override string ToString() => Numero;

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex ApenasDigitos();
}
