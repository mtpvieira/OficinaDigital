using System.ComponentModel.DataAnnotations;
using OficinaDigital.Domain.Cadastro;
using OficinaDigital.Domain.Cadastro.ValueObjects;

namespace OficinaDigital.Application.Cadastro;

public sealed record CadastrarVeiculoRequest(
    [Required] Guid ClienteId,
    [Required, StringLength(8, MinimumLength = 7)] string Placa,
    [Required, StringLength(40)] string Marca,
    [Required, StringLength(60)] string Modelo,
    [Range(1900, 2100)] int AnoFabricacao,
    [StringLength(30)] string? Cor);

public sealed record AlterarVeiculoRequest(
    [Required, StringLength(40)] string Marca,
    [Required, StringLength(60)] string Modelo,
    [Range(1900, 2100)] int AnoFabricacao,
    [StringLength(30)] string? Cor);

public sealed record VeiculoResponse(
    Guid Id,
    Guid ClienteId,
    string Placa,
    PadraoPlaca PadraoPlaca,
    string Marca,
    string Modelo,
    int AnoFabricacao,
    string? Cor,
    DateTime CriadoEm)
{
    public static VeiculoResponse De(Veiculo v) => new(
        v.Id.Valor, v.ClienteId.Valor, v.Placa.Valor, v.Placa.Padrao,
        v.Marca, v.Modelo, v.AnoFabricacao, v.Cor, v.CriadoEm);
}
