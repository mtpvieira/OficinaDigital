namespace OficinaDigital.Application.Clientes;

public sealed record CriarClienteRequest(string Nome, string Documento, string? Email, string? Telefone);
