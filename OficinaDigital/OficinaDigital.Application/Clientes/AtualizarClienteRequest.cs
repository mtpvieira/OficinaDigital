namespace OficinaDigital.Application.Clientes;

public sealed record AtualizarClienteRequest(string Nome, string? Email, string? Telefone);
