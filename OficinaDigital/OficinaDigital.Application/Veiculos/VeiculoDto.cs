namespace OficinaDigital.Application.Veiculos;

public sealed record VeiculoDto(Guid Id, Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
