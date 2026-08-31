using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OficinaDigital.Api.IntegrationTests.Infra;
using Shouldly;
using Xunit.Abstractions;

namespace OficinaDigital.Api.IntegrationTests;

/// <summary>
/// Replica Docs/demo.http requisição por requisição, com os mesmos payloads, e confere
/// cada status code que o roteiro do vídeo promete.
/// </summary>
public class ValidacaoDoRoteiroDemo(ApiDeTeste api, ITestOutputHelper saida) : IClassFixture<ApiDeTeste>
{
    private const string Documento = "12345678909";
    private const string Placa = "BRA2E19";

    [Fact]
    public async Task Roteiro_demo_executa_do_inicio_ao_fim()
    {
        var anon = api.Anonimo();

        // Bloco 1 — autenticação
        await Conferir("GET /api/clientes sem token", HttpStatusCode.Unauthorized,
            () => anon.GetAsync("/api/clientes"));

        await Conferir("login com senha errada", HttpStatusCode.Unauthorized,
            () => anon.PostAsJsonAsync("/api/auth/login",
                new { email = ApiDeTeste.EmailAdmin, senha = "SenhaErrada@123" }, ApiDeTeste.Json));

        var admin = await api.ComoAdminAsync();

        // Bloco 2 — validação
        await Conferir("cliente com CPF de DV inválido", HttpStatusCode.BadRequest,
            () => admin.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Cliente Invalido",
                documento = "52998224724",
                contatos = new[] { new { canal = "EMAIL", valor = "invalido@exemplo.com" } }
            }, ApiDeTeste.Json));

        await Conferir("cliente ainda não cadastrado", HttpStatusCode.NotFound,
            () => admin.GetAsync($"/api/clientes/por-documento/{Documento}"));

        // Bloco 3 — cadastro
        var cliente = await Conferir("cadastrar cliente", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Carlos Andrade",
                documento = Documento,
                contatos = new[]
                {
                    new { canal = "EMAIL", valor = "carlos.andrade@exemplo.com" },
                    new { canal = "SMS", valor = "11987654321" }
                },
                endereco = new
                {
                    logradouro = "Rua das Oficinas",
                    numero = "1200",
                    cidade = "São Paulo",
                    uf = "SP",
                    cep = "01310-100"
                }
            }, ApiDeTeste.Json));

        var clienteId = cliente.GetProperty("id").GetGuid();

        await Conferir("veículo com placa fora dos padrões", HttpStatusCode.BadRequest,
            () => admin.PostAsJsonAsync("/api/veiculos", new
            {
                clienteId,
                placa = "1234ABC",
                marca = "Volkswagen",
                modelo = "Gol",
                anoFabricacao = 2019
            }, ApiDeTeste.Json));

        var veiculo = await Conferir("cadastrar veículo Mercosul", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/veiculos", new
            {
                clienteId,
                placa = Placa,
                marca = "Volkswagen",
                modelo = "Gol 1.6",
                anoFabricacao = 2019,
                cor = "Branco"
            }, ApiDeTeste.Json));

        var veiculoId = veiculo.GetProperty("id").GetGuid();

        // Bloco 4 — catálogo e estoque
        var servico = await Conferir("cadastrar serviço", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/servicos", new
            {
                nome = "Troca de pastilhas de freio",
                descricao = "Substituição das pastilhas dianteiras",
                preco = 320.00m,
                tempoPadraoMinutos = 120
            }, ApiDeTeste.Json));

        var servicoId = servico.GetProperty("id").GetGuid();

        var peca = await Conferir("cadastrar peça", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/pecas", new
            {
                sku = "PAST-FRE-DIANT",
                descricao = "Pastilha de freio dianteira (jogo)",
                unidade = "PC",
                precoVenda = 215.00m,
                estoqueMinimo = 4
            }, ApiDeTeste.Json));

        var pecaId = peca.GetProperty("id").GetGuid();

        var estoqueZerado = await Conferir("estoque nasce zerado", HttpStatusCode.OK,
            () => admin.GetAsync($"/api/estoque/pecas/{pecaId}"));

        estoqueZerado.GetProperty("resumo").GetProperty("saldo").GetDecimal().ShouldBe(0m);
        estoqueZerado.GetProperty("resumo").GetProperty("abaixoDoEstoqueMinimo").GetBoolean().ShouldBeTrue();

        await Conferir("entrada de 10 unidades", HttpStatusCode.OK,
            () => admin.PostAsJsonAsync($"/api/estoque/pecas/{pecaId}/entradas",
                new { quantidade = 10, custoUnitario = 130.00m }, ApiDeTeste.Json));

        // Bloco 5 — abertura e diagnóstico
        var os = await Conferir("abrir OS", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId,
                veiculoId,
                descricaoDoProblema = "Barulho ao frear e pedal baixo"
            }, ApiDeTeste.Json));

        var osId = os.GetProperty("id").GetGuid();
        os.GetProperty("status").GetString().ShouldBe("RECEBIDA");
        os.GetProperty("numero").GetInt64().ShouldBeGreaterThan(0);

        await Conferir("segunda OS no mesmo veículo", HttpStatusCode.Conflict,
            () => admin.PostAsJsonAsync("/api/ordens-servico",
                new { clienteId, veiculoId, descricaoDoProblema = "Tentativa duplicada" }, ApiDeTeste.Json));

        var emDiagnostico = await Conferir("iniciar diagnóstico", HttpStatusCode.OK,
            () => admin.PostAsync($"/api/ordens-servico/{osId}/diagnostico/iniciar", null));
        emDiagnostico.GetProperty("status").GetString().ShouldBe("EM_DIAGNOSTICO");

        await Conferir("registrar serviço", HttpStatusCode.OK,
            () => admin.PostAsJsonAsync($"/api/ordens-servico/{osId}/servicos",
                new { servicos = new[] { new { servicoId } } }, ApiDeTeste.Json));

        var comPeca = await Conferir("registrar peça", HttpStatusCode.OK,
            () => admin.PostAsJsonAsync($"/api/ordens-servico/{osId}/pecas",
                new { pecas = new[] { new { pecaId, quantidade = 2 } } }, ApiDeTeste.Json));

        comPeca.GetProperty("itensDePeca")[0].GetProperty("status").GetString().ShouldBe("RESERVADA");

        var reservado = await Conferir("estoque após a reserva", HttpStatusCode.OK,
            () => admin.GetAsync($"/api/estoque/pecas/{pecaId}"));

        var resumo = reservado.GetProperty("resumo");
        resumo.GetProperty("saldo").GetDecimal().ShouldBe(10m);
        resumo.GetProperty("reservado").GetDecimal().ShouldBe(2m);
        resumo.GetProperty("disponivel").GetDecimal().ShouldBe(8m);

        var diagnosticoFechado = await Conferir("finalizar diagnóstico", HttpStatusCode.OK,
            () => admin.PostAsync($"/api/ordens-servico/{osId}/diagnostico/finalizar", null));

        // O demo.http captura o id do orçamento daqui. Se este campo sumir, a demo quebra.
        diagnosticoFechado.TryGetProperty("orcamentoVigenteId", out var campoOrcamento).ShouldBeTrue(
            "A resposta de finalizar diagnóstico precisa trazer orcamentoVigenteId: " +
            "é dele que o demo.http tira o @orcamentoId.");

        var orcamentoId = campoOrcamento.GetGuid();

        // Bloco 6 — orçamento
        var orcamentos = await Conferir("listar orçamentos da OS", HttpStatusCode.OK,
            () => admin.GetAsync($"/api/orcamentos/da-os/{osId}"));

        orcamentos.GetArrayLength().ShouldBe(1);
        orcamentos[0].GetProperty("id").GetGuid().ShouldBe(orcamentoId);
        orcamentos[0].GetProperty("totalVigente").GetDecimal().ShouldBe(750m);

        await Conferir("enviar orçamento", HttpStatusCode.OK,
            () => admin.PostAsync($"/api/orcamentos/{orcamentoId}/envio", null));

        var aguardando = await Conferir("OS após o envio", HttpStatusCode.OK,
            () => admin.GetAsync($"/api/ordens-servico/{osId}"));
        aguardando.GetProperty("status").GetString().ShouldBe("AGUARDANDO_APROVACAO");

        // Bloco 7 — app do cliente
        var app = await api.ComoClienteAsync(Documento, Placa);

        await Conferir("cliente vê a própria OS", HttpStatusCode.OK,
            () => app.GetAsync($"/api/minhas-ordens/{osId}"));

        await Conferir("token do cliente na API administrativa", HttpStatusCode.Forbidden,
            () => app.GetAsync("/api/clientes"));

        await Conferir("aprovar orçamento", HttpStatusCode.OK,
            () => app.PostAsJsonAsync($"/api/orcamentos/{orcamentoId}/resposta",
                new { decisao = "APROVADO" }, ApiDeTeste.Json));

        await Conferir("responder de novo", HttpStatusCode.BadRequest,
            () => app.PostAsJsonAsync($"/api/orcamentos/{orcamentoId}/resposta",
                new { decisao = "REPROVADO" }, ApiDeTeste.Json));

        // Bloco 8 — execução e entrega
        var emExecucao = await Conferir("iniciar serviço", HttpStatusCode.OK,
            () => admin.PostAsync($"/api/ordens-servico/{osId}/execucao/iniciar", null));
        emExecucao.GetProperty("status").GetString().ShouldBe("EM_EXECUCAO");

        var subtraido = await Conferir("estoque após a subtração", HttpStatusCode.OK,
            () => admin.GetAsync($"/api/estoque/pecas/{pecaId}"));

        var resumoFinal = subtraido.GetProperty("resumo");
        resumoFinal.GetProperty("saldo").GetDecimal().ShouldBe(8m);
        resumoFinal.GetProperty("reservado").GetDecimal().ShouldBe(0m);

        var finalizada = await Conferir("finalizar serviço", HttpStatusCode.OK,
            () => admin.PostAsync($"/api/ordens-servico/{osId}/execucao/finalizar", null));
        finalizada.GetProperty("status").GetString().ShouldBe("FINALIZADA");
        finalizada.GetProperty("total").GetDecimal().ShouldBe(750m);

        var entregue = await Conferir("registrar entrega", HttpStatusCode.OK,
            () => admin.PostAsync($"/api/ordens-servico/{osId}/entrega", null));
        entregue.GetProperty("status").GetString().ShouldBe("ENTREGUE");

        // Bloco 9 — gestão e indicadores
        await Conferir("painel por status", HttpStatusCode.OK,
            () => admin.GetAsync("/api/ordens-servico/painel"));

        await Conferir("listagem de OS", HttpStatusCode.OK,
            () => admin.GetAsync("/api/ordens-servico?pagina=1&tamanhoPagina=10"));

        var indicador = await Conferir("tempo médio de execução", HttpStatusCode.OK,
            () => admin.GetAsync("/api/indicadores/tempo-medio-execucao"));
        indicador.GetProperty("totalDeOsConsideradas").GetInt32().ShouldBeGreaterThanOrEqualTo(1);

        await Conferir("alertas de estoque mínimo", HttpStatusCode.OK,
            () => admin.GetAsync("/api/estoque/alertas"));

        // Bloco 10 — falta de peça
        var pecaSemEstoque = await Conferir("peça sem estoque", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/pecas", new
            {
                sku = "CORREIA-DENT-99",
                descricao = "Correia dentada",
                unidade = "UN",
                precoVenda = 310.00m,
                estoqueMinimo = 2
            }, ApiDeTeste.Json));

        var pecaFaltandoId = pecaSemEstoque.GetProperty("id").GetGuid();

        var veiculo2 = await Conferir("segundo veículo", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/veiculos", new
            {
                clienteId,
                placa = "QWE1R23",
                marca = "Fiat",
                modelo = "Argo",
                anoFabricacao = 2021
            }, ApiDeTeste.Json));

        var os2 = await Conferir("segunda OS", HttpStatusCode.Created,
            () => admin.PostAsJsonAsync("/api/ordens-servico", new
            {
                clienteId,
                veiculoId = veiculo2.GetProperty("id").GetGuid(),
                descricaoDoProblema = "Troca de correia dentada"
            }, ApiDeTeste.Json));

        var os2Id = os2.GetProperty("id").GetGuid();

        await Conferir("iniciar diagnóstico da segunda OS", HttpStatusCode.OK,
            () => admin.PostAsync($"/api/ordens-servico/{os2Id}/diagnostico/iniciar", null));

        var pendente = await Conferir("registrar peça sem saldo", HttpStatusCode.OK,
            () => admin.PostAsJsonAsync($"/api/ordens-servico/{os2Id}/pecas",
                new { pecas = new[] { new { pecaId = pecaFaltandoId, quantidade = 1 } } }, ApiDeTeste.Json));

        pendente.GetProperty("itensDePeca")[0].GetProperty("status").GetString()
            .ShouldBe("PENDENTE_COMPRA");

        await Conferir("entrada da peça faltante", HttpStatusCode.OK,
            () => admin.PostAsJsonAsync($"/api/estoque/pecas/{pecaFaltandoId}/entradas",
                new { quantidade = 5, custoUnitario = 190.00m }, ApiDeTeste.Json));

        var reservadaSozinha = await Conferir("OS após a entrada", HttpStatusCode.OK,
            () => admin.GetAsync($"/api/ordens-servico/{os2Id}"));

        reservadaSozinha.GetProperty("itensDePeca")[0].GetProperty("status").GetString()
            .ShouldBe("RESERVADA");

        saida.WriteLine("");
        saida.WriteLine("Roteiro validado do inicio ao fim.");
    }

    private async Task<JsonElement> Conferir(string passo, HttpStatusCode esperado,
        Func<Task<HttpResponseMessage>> chamada)
    {
        var resposta = await chamada();
        var corpo = await resposta.Content.ReadAsStringAsync();

        saida.WriteLine($"[{(int)resposta.StatusCode}] {passo}");

        if (resposta.StatusCode != esperado)
            throw new Xunit.Sdk.XunitException(
                $"Passo '{passo}': esperado {(int)esperado} {esperado}, veio {(int)resposta.StatusCode}. Corpo: {corpo}");

        return string.IsNullOrWhiteSpace(corpo)
            ? default
            : JsonDocument.Parse(corpo).RootElement.Clone();
    }
}
