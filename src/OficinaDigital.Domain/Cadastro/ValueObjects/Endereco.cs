using System.Text.RegularExpressions;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro.ValueObjects;

public sealed partial class Endereco : ValueObject
{
    public string Logradouro { get; }
    public string Numero { get; }
    public string? Complemento { get; }
    public string Cidade { get; }
    public string Uf { get; }
    public string Cep { get; }

    private Endereco(string logradouro, string numero, string? complemento, string cidade, string uf, string cep)
    {
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Cidade = cidade;
        Uf = uf;
        Cep = cep;
    }

    public static Endereco Criar(string? logradouro, string? numero, string? complemento,
        string? cidade, string? uf, string? cep)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(logradouro), "Logradouro é obrigatório no endereço.");
        DomainException.Se(string.IsNullOrWhiteSpace(numero), "Número é obrigatório no endereço.");
        DomainException.Se(string.IsNullOrWhiteSpace(cidade), "Cidade é obrigatória no endereço.");
        DomainException.Se(string.IsNullOrWhiteSpace(uf) || uf!.Trim().Length != 2, "UF deve ter 2 letras.");

        var cepDigitos = NaoDigito().Replace(cep ?? string.Empty, string.Empty);
        DomainException.Se(cepDigitos.Length != 8, "CEP deve ter 8 dígitos.");

        return new Endereco(logradouro!.Trim(), numero!.Trim(), complemento?.Trim(),
            cidade!.Trim(), uf!.Trim().ToUpperInvariant(), cepDigitos);
    }

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Logradouro;
        yield return Numero;
        yield return Complemento;
        yield return Cidade;
        yield return Uf;
        yield return Cep;
    }

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex NaoDigito();
}
