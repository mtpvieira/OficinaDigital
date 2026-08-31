using OficinaDigital.Application.Atendimento;
using OficinaDigital.Application.Cadastro;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Orcamentos;
using OficinaDigital.Application.Tests.Infra;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.ValueObjects;
using Shouldly;

namespace OficinaDigital.Application.Tests;

public class FluxoDaOrdemDeServicoTests : IDisposable
{
    private readonly AmbienteDeTeste _ambiente = new();
    private readonly Cenario _cenario;

    public FluxoDaOrdemDeServicoTests() => _cenario = new Cenario(_ambiente);

    public void Dispose() => _ambiente.Dispose();

    [Fact]
    public async Task Abre_os_com_numero_sequencial_e_status_recebida()
    {
        var (_, _, os) = await _cenario.AbrirOsAsync();

        os.Status.ShouldBe(StatusOS.RECEBIDA);
        os.StatusDescricao.ShouldBe("Recebida");
        os.Numero.ShouldBeGreaterThan(0);
        os.Total.ShouldBe(0m);
    }

    [Fact]
    public async Task Nao_abre_segunda_os_para_o_mesmo_veiculo()
    {
        var (cliente, veiculo, _) = await _cenario.AbrirOsAsync();

        var erro = await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _cenario.Ordens.AbrirAsync(new AbrirOsRequest(cliente.Id, veiculo.Id, null)));

        erro.Message.ShouldContain("já possui a OS");
    }

    [Fact]
    public async Task Abre_nova_os_depois_que_a_anterior_e_entregue()
    {
        var os = await _cenario.EmExecucaoAsync();
        await _cenario.Ordens.FinalizarServicoAsync(os.Id);

        var finalizada = await _cenario.Ordens.ObterPorIdAsync(os.Id);
        await _cenario.Ordens.RegistrarEntregaAsync(os.Id);

        var nova = await _cenario.Ordens.AbrirAsync(
            new AbrirOsRequest(finalizada.ClienteId, finalizada.VeiculoId, "Novo problema"));

        nova.Numero.ShouldBeGreaterThan(finalizada.Numero);
    }

    [Fact]
    public async Task Nao_abre_os_com_veiculo_de_outro_cliente()
    {
        var clienteA = await _cenario.CadastrarClienteAsync();
        var clienteB = await _cenario.CadastrarClienteAsync(Cenario.CpfOutroCliente, "João");
        var veiculoDeA = await _cenario.CadastrarVeiculoAsync(clienteA.Id);

        var erro = await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _cenario.Ordens.AbrirAsync(new AbrirOsRequest(clienteB.Id, veiculoDeA.Id, null)));

        erro.Message.ShouldContain("não pertence ao cliente");
    }

    [Fact]
    public async Task Registrar_peca_reserva_no_estoque_por_politica()
    {
        var (_, _, os) = await _cenario.AbrirOsAsync();
        var peca = await _cenario.CadastrarPecaComEstoqueAsync(saldo: 20m);

        await _cenario.Ordens.IniciarDiagnosticoAsync(os.Id);
        var atualizada = await _cenario.Ordens.RegistrarPecasAsync(os.Id,
            new RegistrarPecasRequest([new ItemDePecaRequest(peca.Id, 4m)]));

        atualizada.ItensDePeca.ShouldHaveSingleItem().Status.ShouldBe(StatusItemPeca.RESERVADA);

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(peca.Id);
        estoque.Resumo.Saldo.ShouldBe(20m);
        estoque.Resumo.Reservado.ShouldBe(4m);
        estoque.Resumo.Disponivel.ShouldBe(16m);
    }

    [Fact]
    public async Task Finalizar_diagnostico_gera_o_orcamento_automaticamente()
    {
        var (os, orcamento) = await _cenario.ComOrcamentoGeradoAsync();

        orcamento.Tipo.ShouldBe(TipoOrcamento.PRINCIPAL);
        orcamento.VersaoVigente.ShouldBe(1);
        orcamento.TotalVigente.ShouldBe(180m + 4m * 58m);
        os.OrcamentoVigenteId.ShouldBe(orcamento.Id);
    }

    [Fact]
    public async Task Enviar_orcamento_muda_o_status_para_aguardando_aprovacao()
    {
        var (os, orcamento) = await _cenario.ComOrcamentoGeradoAsync();

        await _cenario.Orcamentos.EnviarAsync(orcamento.Id);

        var atualizada = await _cenario.Ordens.ObterPorIdAsync(os.Id);
        atualizada.Status.ShouldBe(StatusOS.AGUARDANDO_APROVACAO);
    }

    [Fact]
    public async Task Aprovar_orcamento_libera_a_execucao_e_subtrai_as_pecas()
    {
        var os = await _cenario.EmExecucaoAsync();

        os.Status.ShouldBe(StatusOS.EM_EXECUCAO);
        os.Execucao.Inicio.ShouldNotBeNull();
        os.ItensDePeca.ShouldHaveSingleItem().Status.ShouldBe(StatusItemPeca.SUBTRAIDA);

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(os.ItensDePeca[0].PecaId);
        estoque.Resumo.Saldo.ShouldBe(16m);
        estoque.Resumo.Reservado.ShouldBe(0m);
    }

    [Fact]
    public async Task Reprovar_orcamento_cancela_a_os_e_libera_as_reservas()
    {
        var (os, orcamento) = await _cenario.ComOrcamentoGeradoAsync();
        await _cenario.Orcamentos.EnviarAsync(orcamento.Id);

        await _cenario.Orcamentos.ResponderAsync(orcamento.Id,
            new ResponderOrcamentoRequest(DecisaoCliente.REPROVADO), null);

        var cancelada = await _cenario.Ordens.ObterPorIdAsync(os.Id);
        cancelada.Status.ShouldBe(StatusOS.CANCELADA);
        cancelada.MotivoCancelamento.ShouldBe(MotivoCancelamento.REPROVADO_PELO_CLIENTE);

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(os.ItensDePeca[0].PecaId);
        estoque.Resumo.Reservado.ShouldBe(0m);
        estoque.Resumo.Disponivel.ShouldBe(20m);
    }

    [Fact]
    public async Task Cancelar_a_os_libera_as_reservas()
    {
        var (os, _) = await _cenario.ComOrcamentoGeradoAsync();

        await _cenario.Ordens.CancelarAsync(os.Id,
            new CancelarOsRequest(MotivoCancelamento.CANCELADO_PELA_OFICINA, "Cliente desistiu"));

        var estoque = await _cenario.Estoque.ObterPorPecaAsync(os.ItensDePeca[0].PecaId);
        estoque.Resumo.Reservado.ShouldBe(0m);
    }

    [Fact]
    public async Task Fluxo_completo_termina_com_veiculo_entregue()
    {
        var os = await _cenario.EmExecucaoAsync();

        var finalizada = await _cenario.Ordens.FinalizarServicoAsync(os.Id);
        finalizada.Status.ShouldBe(StatusOS.FINALIZADA);
        finalizada.Execucao.DuracaoRealEmMinutos.ShouldNotBeNull();

        var entregue = await _cenario.Ordens.RegistrarEntregaAsync(os.Id);

        entregue.Status.ShouldBe(StatusOS.ENTREGUE);
    }

    [Fact]
    public async Task Painel_conta_as_os_por_status()
    {
        await _cenario.EmExecucaoAsync();

        var painel = await _cenario.Ordens.ContarPorStatusAsync();

        painel["Em execução"].ShouldBe(1);
        painel["Recebida"].ShouldBe(0);
    }

    [Fact]
    public async Task Listagem_filtra_por_status()
    {
        await _cenario.EmExecucaoAsync();

        var emExecucao = await _cenario.Ordens.ListarAsync(StatusOS.EM_EXECUCAO, null, null, new Paginacao());
        var recebidas = await _cenario.Ordens.ListarAsync(StatusOS.RECEBIDA, null, null, new Paginacao());

        emExecucao.TotalDeItens.ShouldBe(1);
        recebidas.TotalDeItens.ShouldBe(0);
    }

    [Fact]
    public async Task Os_inexistente_devolve_recurso_nao_encontrado() =>
        await Should.ThrowAsync<RecursoNaoEncontradoException>(() =>
            _cenario.Ordens.ObterPorIdAsync(Guid.NewGuid()));
}
