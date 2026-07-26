using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Clientes;

public sealed class Cliente : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    public Documento Documento { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }

    private Cliente()
    {
    }

    private Cliente(string nome, Documento documento, string? email, string? telefone)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Documento = documento;
        Email = email;
        Telefone = telefone;
    }

    public static Cliente Criar(string nome, string documento, string? email = null, string? telefone = null)
    {
        ValidarNome(nome);
        return new Cliente(nome.Trim(), Documento.Criar(documento), email, telefone);
    }

    public void AtualizarDados(string nome, string? email, string? telefone)
    {
        ValidarNome(nome);
        Nome = nome.Trim();
        Email = email;
        Telefone = telefone;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente é obrigatório.");
    }
}
