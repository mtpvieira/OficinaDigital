using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OficinaDigital.Api.IntegrationTests.Infra;
using Shouldly;

namespace OficinaDigital.Api.IntegrationTests;

public class FluxoCompletoViaApiTests : IClassFixture<ApiDeTeste>
{
    private readonly ApiDeTeste _api;

    private const string Documento = "12345678909";
    private const string Placa = "BRA2E19";

    public FluxoCompletoViaApiTests(ApiDeTeste api) => _api = api;

    [Fact]
    public async Task Caminho_feliz_da_os_do_recebimento_a_entrega()
    {
        var atendente = await _api.ComoAtendenteAsync();
        var mecanico = await _api.ComoMecanicoAsync();

        (await atendente.GetAsync($"/api/clientes/por-documento/{Documento}"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var clienteId = await PostAsync(atendente, "/api/clientes", new
        {
            nome = "Carlos Andrade",
            documento = Documento,
            contatos = new[] { new { canal = "EMAIL", valor = "carlos@exemplo.com" } }
        });

        var veiculoId = await PostAsync(atendente, "/api/veiculos", new
        {
            clienteId,
            placa = Placa,
            marca = "Volkswagen",
            modelo = "Gol",
            anoFabricacao = 2019,
            cor = "Branco"
        });

        var servicoId = await PostAsync(atendente, "/api/servicos", new
        {
            nome = "Revisão de freios",
            descricao = "Pastilhas e discos",
            preco = 320m,
            tempoPadraoMinutos = 120
        });

        var pecaId = await PostAsync(atendente, "/api/pecas", new
        {
            sku = "PAST-FREIO-D",
            descricao = "Pastilha de freio dianteira",
            unidade = "PC",
            precoVenda = 215m,
            estoqueMinimo = 4m
        });

        (await atendente.PostAsJsonAsync($"/api/estoque/pecas/{pecaId}/entradas",
            new { quantidade = 10m, custoUnitario = 130m }, ApiDeTeste.Json))
            .EnsureSuccessStatusCode();

        var osId = await PostAsync(atendente, "/api/ordens-servico", new
        {
            clienteId,
            veiculoId,
            descricaoDoProblema = "Barulho ao frear"
        });

        (await LerOsAsync(atendente, osId)).GetProperty("status").GetString().ShouldBe("RECEBIDA");

        await ChamarAsync(mecanico, $"/api/ordens-servico/{osId}/diagnostico/iniciar");
        (await LerOsAsync(mecanico, osId)).GetProperty("status").GetString().ShouldBe("EM_DIAGNOSTICO");

        await ChamarAsync(mecanico, $"/api/ordens-servico/{osId}/servicos",
            new { servicos = new[] { new { servicoId } } });

        await ChamarAsync(mecanico, $"/api/ordens-servico/{osId}/pecas",
            new { pecas = new[] { new { pecaId, quantidade = 2m } } });

        await ChamarAsync(mecanico, $"/api/ordens-servico/{osId}/diagnostico/finalizar");

        var orcamentos = await LerAsync(atendente, $"/api/orcamentos/da-os/{osId}");
        orcamentos.GetArrayLength().ShouldBe(1);

        var orcamentoId = orcamentos[0].GetProperty("id").GetGuid();
        orcamentos[0].GetProperty("totalVigente").GetDecimal().ShouldBe(320m + 2m * 215m);

        await ChamarAsync(atendente, $"/api/orcamentos/{orcamentoId}/envio");
        (await LerOsAsync(atendente, osId)).GetProperty("status").GetString().ShouldBe("AGUARDANDO_APROVACAO");

        var appDoCliente = await _api.ComoClienteAsync(Documento, Placa);

        var acompanhamento = await LerAsync(appDoCliente, $"/api/minhas-ordens/{osId}");
        acompanhamento.GetProperty("possuiOrcamentoAguardandoResposta").GetBoolean().ShouldBeTrue();
        acompanhamento.GetProperty("placa").GetString().ShouldBe(Placa);

        await ChamarAsync(appDoCliente, $"/api/orcamentos/{orcamentoId}/resposta",
            new { decisao = "APROVADO" });

        await ChamarAsync(mecanico, $"/api/ordens-servico/{osId}/execucao/iniciar");

        var estoque = await LerAsync(atendente, $"/api/estoque/pecas/{pecaId}");
        estoque.GetProperty("resumo").GetProperty("saldo").GetDecimal().ShouldBe(8m);
        estoque.GetProperty("resumo").GetProperty("reservado").GetDecimal().ShouldBe(0m);

        await ChamarAsync(mecanico, $"/api/ordens-servico/{osId}/execucao/finalizar");

        var finalizada = await LerOsAsync(atendente, osId);
        finalizada.GetProperty("status").GetString().ShouldBe("FINALIZADA");

        await ChamarAsync(atendente, $"/api/ordens-servico/{osId}/entrega");

        var entregue = await LerOsAsync(atendente, osId);
        entregue.GetProperty("status").GetString().ShouldBe("ENTREGUE");

        var painel = await LerAsync(atendente, "/api/indicadores/tempo-medio-execucao");
        painel.GetProperty("totalDeOsConsideradas").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Cliente_nao_enxerga_os_de_outro_cliente()
    {
        var atendente = await _api.ComoAtendenteAsync();

        var donoId = await PostAsync(atendente, "/api/clientes", new
        {
            nome = "Dono do carro",
            documento = "98765432100",
            contatos = new[] { new { canal = "EMAIL", valor = "dono@exemplo.com" } }
        });

        var veiculoId = await PostAsync(atendente, "/api/veiculos", new
        {
            clienteId = donoId,
            placa = "QWE1R23",
            marca = "Fiat",
            modelo = "Argo",
            anoFabricacao = 2020
        });

        var osId = await PostAsync(atendente, "/api/ordens-servico",
            new { clienteId = donoId, veiculoId, descricaoDoProblema = "Revisão" });

        var intrusoId = await PostAsync(atendente, "/api/clientes", new
        {
            nome = "Outro cliente",
            documento = "11144477735",
            contatos = new[] { new { canal = "EMAIL", valor = "outro@exemplo.com" } }
        });

        await PostAsync(atendente, "/api/veiculos", new
        {
            clienteId = intrusoId,
            placa = "ZXC4V56",
            marca = "Ford",
            modelo = "Ka",
            anoFabricacao = 2018
        });

        var intruso = await _api.ComoClienteAsync("11144477735", "ZXC4V56");

        var resposta = await intruso.GetAsync($"/api/minhas-ordens/{osId}");

        resposta.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Nao_inicia_servico_sem_orcamento_aprovado()
    {
        var atendente = await _api.ComoAtendenteAsync();
        var mecanico = await _api.ComoMecanicoAsync();

        var clienteId = await PostAsync(atendente, "/api/clientes", new
        {
            nome = "Cliente sem aprovação",
            documento = "11222333000181",
            contatos = new[] { new { canal = "SMS", valor = "11987654321" } }
        });

        var veiculoId = await PostAsync(atendente, "/api/veiculos", new
        {
            clienteId,
            placa = "POI9U87",
            marca = "Honda",
            modelo = "Fit",
            anoFabricacao = 2017
        });

        var osId = await PostAsync(atendente, "/api/ordens-servico",
            new { clienteId, veiculoId, descricaoDoProblema = "Revisão" });

        var resposta = await mecanico.PostAsync($"/api/ordens-servico/{osId}/execucao/iniciar", null);

        resposta.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    private static async Task<Guid> PostAsync(HttpClient cliente, string rota, object corpo)
    {
        var resposta = await cliente.PostAsJsonAsync(rota, corpo, ApiDeTeste.Json);

        if (!resposta.IsSuccessStatusCode)
            throw new Xunit.Sdk.XunitException(
                $"POST {rota} devolveu {(int)resposta.StatusCode}: {await resposta.Content.ReadAsStringAsync()}");

        var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task ChamarAsync(HttpClient cliente, string rota, object? corpo = null,
        HttpMethod? metodo = null)
    {
        var requisicao = new HttpRequestMessage(metodo ?? HttpMethod.Post, rota);

        if (corpo is not null)
            requisicao.Content = JsonContent.Create(corpo, options: ApiDeTeste.Json);

        var resposta = await cliente.SendAsync(requisicao);

        if (!resposta.IsSuccessStatusCode)
            throw new Xunit.Sdk.XunitException(
                $"{requisicao.Method} {rota} devolveu {(int)resposta.StatusCode}: " +
                await resposta.Content.ReadAsStringAsync());
    }

    private static async Task<JsonElement> LerAsync(HttpClient cliente, string rota)
    {
        var resposta = await cliente.GetAsync(rota);

        if (!resposta.IsSuccessStatusCode)
            throw new Xunit.Sdk.XunitException(
                $"GET {rota} devolveu {(int)resposta.StatusCode}: {await resposta.Content.ReadAsStringAsync()}");

        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static Task<JsonElement> LerOsAsync(HttpClient cliente, Guid osId) =>
        LerAsync(cliente, $"/api/ordens-servico/{osId}");
}
