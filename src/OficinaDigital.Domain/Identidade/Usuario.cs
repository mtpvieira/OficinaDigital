using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Identidade;

public enum PerfilUsuario
{
    ATENDENTE = 1,
    MECANICO = 2,
    ADMINISTRADOR = 3
}

public class Usuario : AggregateRoot<UsuarioId>
{
    public string Nome { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public string SenhaSalt { get; private set; } = null!;
    public PerfilUsuario Perfil { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? UltimoAcessoEm { get; private set; }

    protected Usuario() { }

    private Usuario(UsuarioId id, string nome, string email, string senhaHash, string senhaSalt,
        PerfilUsuario perfil)
    {
        Id = id;
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        SenhaSalt = senhaSalt;
        Perfil = perfil;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public static Usuario Criar(string? nome, string? email, string senhaHash, string senhaSalt,
        PerfilUsuario perfil)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(nome), "Nome do usuário é obrigatório.");
        DomainException.Se(string.IsNullOrWhiteSpace(email), "E-mail do usuário é obrigatório.");
        DomainException.Se(string.IsNullOrWhiteSpace(senhaHash), "Hash da senha é obrigatório.");

        return new Usuario(UsuarioId.Novo(), nome!.Trim(), email!.Trim().ToLowerInvariant(),
            senhaHash, senhaSalt, perfil);
    }

    public void RegistrarAcesso() => UltimoAcessoEm = DateTime.UtcNow;

    public void AlterarSenha(string novoHash, string novoSalt)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(novoHash), "Hash da senha é obrigatório.");

        SenhaHash = novoHash;
        SenhaSalt = novoSalt;
    }

    public void Inativar() => Ativo = false;
}
