using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Moq;
using OficinaDigital.Infrastructure.Auth;

namespace OficinaDigital.Infrastructure.Tests.Auth;

public class JwtTokenGeneratorTests
{
    private static Mock<IOptions<JwtOptions>> CriarOptionsMock(JwtOptions options)
    {
        var mock = new Mock<IOptions<JwtOptions>>();
        mock.Setup(o => o.Value).Returns(options);
        return mock;
    }

    [Fact]
    public void GerarToken_DeveRetornarTokenComClaimsEExpiracaoCorretas()
    {
        var options = new JwtOptions
        {
            ChaveSecreta = "chave-secreta-de-teste-com-tamanho-suficiente-1234",
            Emissor = "OficinaDigital.Testes",
            Audiencia = "OficinaDigital.Api.Testes",
            ExpiracaoMinutos = 30
        };
        var optionsMock = CriarOptionsMock(options);
        var generator = new JwtTokenGenerator(optionsMock.Object);

        var resultado = generator.GerarToken("usuario.teste", ["Admin"]);

        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));

        var tokenLido = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Token);
        Assert.Equal(options.Emissor, tokenLido.Issuer);
        Assert.Contains(options.Audiencia, tokenLido.Audiences);
        Assert.Equal("usuario.teste", tokenLido.Subject);
        Assert.Contains(tokenLido.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        var diferenca = resultado.ExpiraEm - DateTime.UtcNow;
        Assert.True(diferenca.TotalMinutes is > 29 and <= 30);

        optionsMock.Verify(o => o.Value, Times.AtLeastOnce);
    }

    [Fact]
    public void GerarToken_SemRoles_NaoDeveIncluirClaimsDeRole()
    {
        var options = new JwtOptions { ChaveSecreta = "outra-chave-secreta-de-teste-com-tamanho-ok-9876" };
        var optionsMock = CriarOptionsMock(options);
        var generator = new JwtTokenGenerator(optionsMock.Object);

        var resultado = generator.GerarToken("outro.usuario", []);

        var tokenLido = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Token);
        Assert.DoesNotContain(tokenLido.Claims, c => c.Type == ClaimTypes.Role);
    }
}
