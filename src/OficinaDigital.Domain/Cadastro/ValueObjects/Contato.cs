using System.Text.RegularExpressions;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro.ValueObjects;

public enum CanalContato
{
    EMAIL = 1,
    SMS = 2
}

public sealed partial class Contato : ValueObject
{
    public CanalContato Canal { get; }
    public string Valor { get; }

    private Contato(CanalContato canal, string valor)
    {
        Canal = canal;
        Valor = valor;
    }

    public static Contato Criar(CanalContato canal, string? valor)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(valor), "Valor do contato é obrigatório.");

        var normalizado = valor!.Trim();

        switch (canal)
        {
            case CanalContato.EMAIL:
                DomainException.Se(!FormatoEmail().IsMatch(normalizado), $"E-mail {valor} é inválido.");
                normalizado = normalizado.ToLowerInvariant();
                break;

            case CanalContato.SMS:
                normalizado = NaoDigito().Replace(normalizado, string.Empty);
                DomainException.Se(normalizado.Length is < 10 or > 11,
                    $"Telefone {valor} deve ter DDD + número (10 ou 11 dígitos).");
                break;

            default:
                throw new DomainException($"Canal de contato {canal} não suportado.");
        }

        return new Contato(canal, normalizado);
    }

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Canal;
        yield return Valor;
    }

    public override string ToString() => $"{Canal}: {Valor}";

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$")]
    private static partial Regex FormatoEmail();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex NaoDigito();
}
