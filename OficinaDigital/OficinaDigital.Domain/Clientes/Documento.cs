using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Clientes;

public enum TipoDocumento
{
    Cpf,
    Cnpj
}

public sealed class Documento : ValueObject
{
    private static readonly int[] MultiplicadoresCpf1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] MultiplicadoresCpf2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] MultiplicadoresCnpj1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] MultiplicadoresCnpj2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Numero { get; }
    public TipoDocumento Tipo { get; }

    private Documento(string numero, TipoDocumento tipo)
    {
        Numero = numero;
        Tipo = tipo;
    }

    public static Documento Criar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("Documento é obrigatório.");

        var digitos = new string(valor.Where(char.IsDigit).ToArray());

        return digitos.Length switch
        {
            11 when EhCpfValido(digitos) => new Documento(digitos, TipoDocumento.Cpf),
            14 when EhCnpjValido(digitos) => new Documento(digitos, TipoDocumento.Cnpj),
            11 or 14 => throw new DomainException("Documento inválido: dígitos verificadores não conferem."),
            _ => throw new DomainException("Documento inválido: deve conter 11 dígitos (CPF) ou 14 dígitos (CNPJ).")
        };
    }

    private static bool EhCpfValido(string cpf)
    {
        if (TodosDigitosIguais(cpf)) return false;

        var digito1 = CalcularDigitoVerificador(cpf[..9], MultiplicadoresCpf1);
        var digito2 = CalcularDigitoVerificador(cpf[..9] + digito1, MultiplicadoresCpf2);

        return cpf[9..] == $"{digito1}{digito2}";
    }

    private static bool EhCnpjValido(string cnpj)
    {
        if (TodosDigitosIguais(cnpj)) return false;

        var digito1 = CalcularDigitoVerificador(cnpj[..12], MultiplicadoresCnpj1);
        var digito2 = CalcularDigitoVerificador(cnpj[..12] + digito1, MultiplicadoresCnpj2);

        return cnpj[12..] == $"{digito1}{digito2}";
    }

    private static bool TodosDigitosIguais(string numero) => numero.Distinct().Count() == 1;

    private static int CalcularDigitoVerificador(string baseNumero, int[] multiplicadores)
    {
        var soma = baseNumero.Select((c, i) => (c - '0') * multiplicadores[i]).Sum();
        var resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }

    public string Formatado => Tipo == TipoDocumento.Cpf
        ? $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..]}"
        : $"{Numero[..2]}.{Numero[2..5]}.{Numero[5..8]}/{Numero[8..12]}-{Numero[12..]}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Numero;
    }

    public override string ToString() => Formatado;
}
