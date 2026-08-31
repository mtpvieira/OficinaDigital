using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Identidade;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Identidade;

public class UsuarioTests
{
    [Fact]
    public void Cria_usuario_ativo_com_email_normalizado()
    {
        var usuario = Usuario.Criar("Ana", "ANA@Oficina.com", "hash", "salt", PerfilUsuario.ATENDENTE);

        usuario.Email.ShouldBe("ana@oficina.com");
        usuario.Ativo.ShouldBeTrue();
        usuario.Perfil.ShouldBe(PerfilUsuario.ATENDENTE);
        usuario.UltimoAcessoEm.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, "a@b.com", "hash")]
    [InlineData("Ana", null, "hash")]
    [InlineData("Ana", "a@b.com", "")]
    public void Exige_nome_email_e_hash(string? nome, string? email, string hash) =>
        Should.Throw<DomainException>(() => Usuario.Criar(nome, email, hash, "salt", PerfilUsuario.ATENDENTE));

    [Fact]
    public void Registrar_acesso_marca_a_data()
    {
        var usuario = Usuario.Criar("Ana", "a@b.com", "hash", "salt", PerfilUsuario.MECANICO);

        usuario.RegistrarAcesso();

        usuario.UltimoAcessoEm.ShouldNotBeNull();
    }

    [Fact]
    public void Altera_senha_e_inativa()
    {
        var usuario = Usuario.Criar("Ana", "a@b.com", "hash", "salt", PerfilUsuario.MECANICO);

        usuario.AlterarSenha("novoHash", "novoSalt");
        usuario.SenhaHash.ShouldBe("novoHash");

        usuario.Inativar();
        usuario.Ativo.ShouldBeFalse();
    }
}
