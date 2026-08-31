using System.Net;
using System.Net.Http.Json;
using OficinaDigital.Api.IntegrationTests.Infra;
using Shouldly;

namespace OficinaDigital.Api.IntegrationTests;

public class SegurancaDaApiTests(ApiDeTeste api) : IClassFixture<ApiDeTeste>
{
    [Theory]
    [InlineData("/api/clientes")]
    [InlineData("/api/veiculos")]
    [InlineData("/api/servicos")]
    [InlineData("/api/pecas")]
    [InlineData("/api/estoque")]
    [InlineData("/api/ordens-servico")]
    [InlineData("/api/indicadores/tempo-medio-execucao")]
    [InlineData("/api/minhas-ordens")]
    public async Task Endpoint_administrativo_sem_token_devolve_401(string rota)
    {
        var resposta = await api.Anonimo().GetAsync(rota);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_check_e_publico()
    {
        var resposta = await api.Anonimo().GetAsync("/health");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_com_credenciais_validas_devolve_token()
    {
        var resposta = await api.Anonimo().PostAsJsonAsync("/api/auth/login",
            new { email = ApiDeTeste.EmailAtendente, senha = ApiDeTeste.SenhaPadrao }, ApiDeTeste.Json);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var corpo = await resposta.Content.ReadAsStringAsync();
        corpo.ShouldContain("accessToken");
        corpo.ShouldContain("ATENDENTE");
    }

    [Fact]
    public async Task Login_com_senha_errada_devolve_401_sem_revelar_o_motivo()
    {
        var resposta = await api.Anonimo().PostAsJsonAsync("/api/auth/login",
            new { email = ApiDeTeste.EmailAtendente, senha = "SenhaErrada@1" }, ApiDeTeste.Json);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("Credenciais inválidas");
    }

    [Fact]
    public async Task Token_invalido_e_recusado()
    {
        var cliente = api.Anonimo();
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", "token.invalido.aqui");

        var resposta = await cliente.GetAsync("/api/clientes");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Mecanico_nao_acessa_endpoint_exclusivo_do_atendente()
    {
        var mecanico = await api.ComoMecanicoAsync();

        var resposta = await mecanico.PostAsJsonAsync("/api/servicos",
            new { nome = "Serviço do mecânico", preco = 10m, tempoPadraoMinutos = 30 }, ApiDeTeste.Json);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Atendente_nao_gerencia_usuarios()
    {
        var atendente = await api.ComoAtendenteAsync();

        var resposta = await atendente.GetAsync("/api/auth/usuarios");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrador_gerencia_usuarios()
    {
        var admin = await api.ComoAdminAsync();

        var resposta = await admin.GetAsync("/api/auth/usuarios");

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Documento_invalido_devolve_400_com_problem_details()
    {
        var atendente = await api.ComoAtendenteAsync();

        var resposta = await atendente.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Cliente Teste",
            documento = "52998224724",
            contatos = new[] { new { canal = "EMAIL", valor = "teste@exemplo.com" } }
        }, ApiDeTeste.Json);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("dígito verificador");
    }

    [Fact]
    public async Task Placa_invalida_devolve_400()
    {
        var atendente = await api.ComoAtendenteAsync();

        var cliente = await CriarClienteAsync(atendente, "11144477735", "Cliente da placa");

        var resposta = await atendente.PostAsJsonAsync("/api/veiculos", new
        {
            clienteId = cliente,
            placa = "1234ABC",
            marca = "Fiat",
            modelo = "Uno",
            anoFabricacao = 2015
        }, ApiDeTeste.Json);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Payload_invalido_devolve_400_pela_validacao_de_modelo()
    {
        var atendente = await api.ComoAtendenteAsync();

        var resposta = await atendente.PostAsJsonAsync("/api/clientes",
            new { nome = "A", documento = "", contatos = Array.Empty<object>() }, ApiDeTeste.Json);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Recurso_inexistente_devolve_404()
    {
        var atendente = await api.ComoAtendenteAsync();

        var resposta = await atendente.GetAsync($"/api/clientes/{Guid.NewGuid()}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> CriarClienteAsync(HttpClient cliente, string documento, string nome)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome,
            documento,
            contatos = new[] { new { canal = "EMAIL", valor = "cliente@exemplo.com" } }
        }, ApiDeTeste.Json);

        if (resposta.StatusCode == HttpStatusCode.Conflict)
        {
            var existente = await cliente.GetFromJsonAsync<Dictionary<string, object>>(
                $"/api/clientes/por-documento/{documento}", ApiDeTeste.Json);

            return Guid.Parse(existente!["id"].ToString()!);
        }

        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<Dictionary<string, object>>(ApiDeTeste.Json);
        return Guid.Parse(criado!["id"].ToString()!);
    }
}
