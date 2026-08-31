using OficinaDigital.Application.Atendimento;
using OficinaDigital.Application.Catalogo;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Estoque;
using OficinaDigital.Application.Tests.Infra;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;
using Shouldly;

namespace OficinaDigital.Application.Tests;

public class FluxoDeEstoqueTests : IDisposable
{
    private readonly AmbienteDeTeste _ambiente = new();
    private readonly Cenario _cenario;

    public FluxoDeEstoqueTests() => _cenario = new Cenario(_ambiente);

    public void Dispose() => _ambiente.Dispose();

    [Fact]
    public async Task Cadastrar_peca_abre_a_posicao_de_estoque_zerada_por_politica()
    {
        var peca = await _cenario.Pecas.CadastrarAsync(
            new CadastrarPecaRequest("FILTRO-01", "Filtro de óleo", UnidadeDeMedida.UN, 42m, 10m));

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(peca.Id);

        estoque.Resumo.Saldo.ShouldBe(0m);
        estoque.Resumo.EstoqueMinimo.ShouldBe(10m);
        estoque.Resumo.AbaixoDoEstoqueMinimo.ShouldBeTrue();
    }

    [Fact]
    public async Task Entrada_de_pecas_soma_ao_saldo_e_registra_movimento()
    {
        var peca = await _cenario.CadastrarPecaComEstoqueAsync(saldo: 0m);

        await _cenario.Estoque.RegistrarEntradaAsync(peca.Id, new RegistrarEntradaRequest(30m, 35m));

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(peca.Id);
        estoque.Resumo.Saldo.ShouldBe(30m);
        estoque.Movimentos.ShouldHaveSingleItem().Tipo.ShouldBe(Domain.Estoque.TipoMovimentoEstoque.ENTRADA);
    }

    [Fact]
    public async Task Sem_saldo_suficiente_o_item_da_os_fica_pendente_de_compra()
    {
        var (_, _, os) = await _cenario.AbrirOsAsync();
        var peca = await _cenario.CadastrarPecaComEstoqueAsync(saldo: 2m);

        await _cenario.Ordens.IniciarDiagnosticoAsync(os.Id);
        var atualizada = await _cenario.Ordens.RegistrarPecasAsync(os.Id,
            new RegistrarPecasRequest([new ItemDePecaRequest(peca.Id, 10m)]));

        atualizada.ItensDePeca.ShouldHaveSingleItem().Status.ShouldBe(StatusItemPeca.PENDENTE_COMPRA);

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(peca.Id);
        estoque.Resumo.Reservado.ShouldBe(0m);
    }

    [Fact]
    public async Task Com_peca_pendente_o_diagnostico_fecha_mas_o_orcamento_nao_e_gerado()
    {
        var (_, _, os) = await _cenario.AbrirOsAsync();
        var servico = await _cenario.CadastrarServicoAsync();
        var peca = await _cenario.CadastrarPecaComEstoqueAsync(saldo: 0m);

        await _cenario.Ordens.IniciarDiagnosticoAsync(os.Id);
        await _cenario.Ordens.RegistrarServicosAsync(os.Id,
            new RegistrarServicosRequest([new ItemDeServicoRequest(servico.Id)]));
        await _cenario.Ordens.RegistrarPecasAsync(os.Id,
            new RegistrarPecasRequest([new ItemDePecaRequest(peca.Id, 4m)]));

        await _cenario.Ordens.FinalizarDiagnosticoAsync(os.Id);

        (await _cenario.Orcamentos.ListarPorOsAsync(os.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Entrada_da_peca_faltante_reserva_para_a_os_na_fila()
    {
        var (_, _, os) = await _cenario.AbrirOsAsync();
        var peca = await _cenario.CadastrarPecaComEstoqueAsync(saldo: 0m);

        await _cenario.Ordens.IniciarDiagnosticoAsync(os.Id);
        await _cenario.Ordens.RegistrarPecasAsync(os.Id,
            new RegistrarPecasRequest([new ItemDePecaRequest(peca.Id, 4m)]));

        await _cenario.Estoque.RegistrarEntradaAsync(peca.Id, new RegistrarEntradaRequest(10m, 30m));

        var atualizada = await _cenario.Ordens.ObterPorIdAsync(os.Id);
        atualizada.ItensDePeca.ShouldHaveSingleItem().Status.ShouldBe(StatusItemPeca.RESERVADA);

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(peca.Id);
        estoque.Resumo.Reservado.ShouldBe(4m);
        estoque.Resumo.Disponivel.ShouldBe(6m);
    }

    [Fact]
    public async Task Consulta_de_disponibilidade_responde_o_que_o_estoque_atende()
    {
        var comSaldo = await _cenario.CadastrarPecaComEstoqueAsync("PECA-A", saldo: 10m);
        var semSaldo = await _cenario.CadastrarPecaComEstoqueAsync("PECA-B", saldo: 1m);

        var resultado = await _cenario.Estoque.ConsultarDisponibilidadeAsync(
            new Dictionary<Guid, decimal> { [comSaldo.Id] = 5m, [semSaldo.Id] = 5m });

        resultado.First(r => r.PecaId == comSaldo.Id).Atende.ShouldBeTrue();
        resultado.First(r => r.PecaId == semSaldo.Id).Atende.ShouldBeFalse();
    }

    [Fact]
    public async Task Alerta_de_estoque_minimo_lista_o_que_precisa_de_reposicao()
    {
        await _cenario.CadastrarPecaComEstoqueAsync("PECA-OK", saldo: 50m, estoqueMinimo: 5m);
        await _cenario.CadastrarPecaComEstoqueAsync("PECA-BAIXA", saldo: 2m, estoqueMinimo: 20m);

        var alertas = await _cenario.Estoque.ListarAlertasDeEstoqueMinimoAsync();

        alertas.ShouldHaveSingleItem().Sku.ShouldBe("PECA-BAIXA");
    }

    [Fact]
    public async Task Peca_inativa_nao_entra_em_nova_os()
    {
        var (_, _, os) = await _cenario.AbrirOsAsync();
        var peca = await _cenario.CadastrarPecaComEstoqueAsync();
        await _cenario.Pecas.InativarAsync(peca.Id);

        await _cenario.Ordens.IniciarDiagnosticoAsync(os.Id);

        var erro = await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _cenario.Ordens.RegistrarPecasAsync(os.Id,
                new RegistrarPecasRequest([new ItemDePecaRequest(peca.Id, 1m)])));

        erro.Message.ShouldContain("inativa");
    }

    [Fact]
    public async Task Sku_duplicado_e_rejeitado()
    {
        await _cenario.CadastrarPecaComEstoqueAsync("SKU-DUP");

        await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _cenario.Pecas.CadastrarAsync(new CadastrarPecaRequest(
                "sku-dup", "Outra peça", UnidadeDeMedida.UN, 10m, 1m)));
    }
}
