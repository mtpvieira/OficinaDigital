namespace OficinaDigital.Application.Clientes;

public sealed record ClienteDto(Guid Id, string Nome, string Documento, string? Email, string? Telefone);
