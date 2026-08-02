using Microsoft.Extensions.Options;
using Moq;
using OficinaDigital.Infrastructure.Auth;

namespace OficinaDigital.Infrastructure.Tests.Auth;

public class AdminAuthenticatorTests
{
    private static AdminAuthenticator CriarSut(AdminCredentialsOptions options)
    {
        var optionsMock = new Mock<IOptions<AdminCredentialsOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        return new AdminAuthenticator(optionsMock.Object);
    }

    [Fact]
    public void ValidarCredenciais_ComUsuarioESenhaCorretos_DeveRetornarTrue()
    {
        var sut = CriarSut(new AdminCredentialsOptions { Usuario = "admin", Senha = "senha-forte" });

        var resultado = sut.ValidarCredenciais("admin", "senha-forte");

        Assert.True(resultado);
    }

    [Theory]
    [InlineData("admin", "senha-errada")]
    [InlineData("usuario-errado", "senha-forte")]
    [InlineData("Admin", "senha-forte")]
    public void ValidarCredenciais_ComDadosIncorretos_DeveRetornarFalse(string usuario, string senha)
    {
        var sut = CriarSut(new AdminCredentialsOptions { Usuario = "admin", Senha = "senha-forte" });

        var resultado = sut.ValidarCredenciais(usuario, senha);

        Assert.False(resultado);
    }
}
