using System.Text.RegularExpressions;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Veiculos;

public sealed partial class Veiculo : AggregateRoot
{
    private const int AnoMinimo = 1900;

    public Guid ClienteId { get; private set; }
    public string Placa { get; private set; } = null!;
    public string Marca { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public int Ano { get; private set; }

    private Veiculo()
    {
    }

    private Veiculo(Guid clienteId, string placa, string marca, string modelo, int ano)
    {
        Id = Guid.NewGuid();
        ClienteId = clienteId;
        Placa = placa;
        Marca = marca;
        Modelo = modelo;
        Ano = ano;
    }

    public static Veiculo Criar(Guid clienteId, string placa, string marca, string modelo, int ano)
    {
        if (clienteId == Guid.Empty)
            throw new DomainException("Veículo deve estar vinculado a um cliente.");

        if (string.IsNullOrWhiteSpace(placa))
            throw new DomainException("Placa é obrigatória.");

        placa = placa.Trim().ToUpperInvariant().Replace("-", string.Empty);

        ValidarMarcaModelo(marca, modelo);
        ValidarAno(ano);
        ValidarPlaca(placa);

        return new Veiculo(clienteId, placa, marca.Trim(), modelo.Trim(), ano);
    }

    public void AtualizarDados(string marca, string modelo, int ano)
    {
        ValidarMarcaModelo(marca, modelo);
        ValidarAno(ano);

        Marca = marca.Trim();
        Modelo = modelo.Trim();
        Ano = ano;
    }

    private static void ValidarMarcaModelo(string marca, string modelo)
    {
        if (string.IsNullOrWhiteSpace(marca))
            throw new DomainException("Marca do veículo é obrigatória.");
        if (string.IsNullOrWhiteSpace(modelo))
            throw new DomainException("Modelo do veículo é obrigatório.");
    }

    private static void ValidarAno(int ano)
    {
        var anoMaximo = DateTime.UtcNow.Year + 1;
        if (ano < AnoMinimo || ano > anoMaximo)
            throw new DomainException($"Ano do veículo deve estar entre {AnoMinimo} e {anoMaximo}.");
    }

    private static void ValidarPlaca(string placa)
    {
        if (!FormatoAntigoRegex().IsMatch(placa) && !FormatoMercosulRegex().IsMatch(placa))
            throw new DomainException("Placa inválida: formatos aceitos são AAA-9999 (antigo) ou AAA9A99 (Mercosul).");
    }

    [GeneratedRegex("^[A-Z]{3}[0-9]{4}$")]
    private static partial Regex FormatoAntigoRegex();

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex FormatoMercosulRegex();
}