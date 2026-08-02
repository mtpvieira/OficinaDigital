namespace OficinaDigital.Application.Veiculos;

public sealed record CriarVeiculoRequest(Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
