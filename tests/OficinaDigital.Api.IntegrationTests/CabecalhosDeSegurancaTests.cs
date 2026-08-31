using System.Net;
using OficinaDigital.Api.IntegrationTests.Infra;
using Shouldly;

namespace OficinaDigital.Api.IntegrationTests;

public class CabecalhosDeSegurancaTests(ApiDeTeste api) : IClassFixture<ApiDeTeste>
{
    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "no-referrer")]
    public async Task Toda_resposta_traz_os_cabecalhos_de_seguranca(string cabecalho, string esperado)
    {
        var resposta = await api.Anonimo().GetAsync("/health");

        resposta.Headers.TryGetValues(cabecalho, out var valores).ShouldBeTrue(
            $"O cabeçalho {cabecalho} deveria estar presente.");

        valores!.ShouldContain(esperado);
    }

    [Fact]
    public async Task Cabecalhos_tambem_aparecem_em_resposta_de_erro()
    {
        var resposta = await api.Anonimo().GetAsync("/api/clientes");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        resposta.Headers.TryGetValues("X-Content-Type-Options", out _).ShouldBeTrue();
    }
}
