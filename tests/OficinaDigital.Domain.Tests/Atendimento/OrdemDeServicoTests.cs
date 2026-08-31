using OficinaDigital.Domain.Atendimento;
using OficinaDigital.Domain.Atendimento.Events;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Atendimento;

internal static class OsBuilder
{
    public static readonly ClienteId Cliente = ClienteId.Novo();
    public static readonly VeiculoId Veiculo = VeiculoId.Novo();
    public static readonly ServicoId ServicoTrocaDeOleo = ServicoId.Novo();
    public static readonly PecaId PecaOleo = PecaId.Novo();

    public static OrdemDeServico Aberta() =>
        OrdemDeServico.Abrir(Cliente, Veiculo, "Barulho no motor");

    public static OrdemDeServico EmDiagnostico()
    {
        var os = Aberta();
        os.IniciarDiagnostico();
        return os;
    }

    public static OrdemDeServico DiagnosticoCompleto(bool comPeca = true)
    {
        var os = EmDiagnostico();

        os.RegistrarServicos([(ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]);

        if (comPeca)
        {
            os.RegistrarPecas([(PecaOleo, "Óleo 5W30", 4m, Dinheiro.De(58m))]);
            os.ConfirmarReservaDaPeca(PecaOleo);
        }

        return os;
    }

    public static OrdemDeServico AguardandoAprovacao()
    {
        var os = DiagnosticoCompleto();
        os.FinalizarDiagnostico();
        os.VincularOrcamento(OrcamentoId.Novo());
        os.MarcarOrcamentoEnviado();
        return os;
    }

    public static OrdemDeServico EmExecucao()
    {
        var os = AguardandoAprovacao();
        os.RegistrarAprovacaoDoOrcamento();
        os.IniciarServico();
        os.ConfirmarSubtracaoDasPecas();
        return os;
    }

    public static OrdemDeServico Finalizada()
    {
        var os = EmExecucao();
        os.FinalizarServico();
        return os;
    }

}

public class OrdemDeServicoAberturaTests
{
    [Fact]
    public void Abre_com_status_recebida_e_total_zerado()
    {
        var os = OsBuilder.Aberta();

        os.Status.ShouldBe(StatusOS.RECEBIDA);
        os.Total.Valor.ShouldBe(0m);
        os.EstaAtiva.ShouldBeTrue();
        os.ClienteId.ShouldBe(OsBuilder.Cliente);
        os.VeiculoId.ShouldBe(OsBuilder.Veiculo);
    }

    [Fact]
    public void Publica_evento_pivotal_de_os_aberta() =>
        OsBuilder.Aberta().EventosDeDominio.OfType<OsAberta>().ShouldHaveSingleItem();

    [Fact]
    public void Nao_abre_sem_cliente_identificado() =>
        Should.Throw<DomainException>(() =>
                OrdemDeServico.Abrir(new ClienteId(Guid.Empty), OsBuilder.Veiculo))
            .Message.ShouldContain("cliente");

    [Fact]
    public void Nao_abre_sem_veiculo_identificado() =>
        Should.Throw<DomainException>(() =>
                OrdemDeServico.Abrir(OsBuilder.Cliente, new VeiculoId(Guid.Empty)))
            .Message.ShouldContain("veículo");
}

public class OrdemDeServicoDiagnosticoTests
{
    [Fact]
    public void Iniciar_diagnostico_muda_o_status_e_publica_a_transicao()
    {
        var os = OsBuilder.Aberta();
        os.LimparEventos();

        os.IniciarDiagnostico();

        os.Status.ShouldBe(StatusOS.EM_DIAGNOSTICO);
        os.EventosDeDominio.OfType<DiagnosticoIniciado>().ShouldHaveSingleItem();

        var transicao = os.EventosDeDominio.OfType<StatusDaOsAlterado>().ShouldHaveSingleItem();
        transicao.StatusAnterior.ShouldBe(StatusOS.RECEBIDA);
        transicao.NovoStatus.ShouldBe(StatusOS.EM_DIAGNOSTICO);
    }

    [Fact]
    public void Registrar_servicos_congela_o_preco_e_soma_no_total()
    {
        var os = OsBuilder.EmDiagnostico();

        os.RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]);

        os.ItensDeServico.ShouldHaveSingleItem().PrecoCongelado.Valor.ShouldBe(180m);
        os.Total.Valor.ShouldBe(180m);
        os.EventosDeDominio.OfType<ServicosDaOsRegistrados>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Nao_registra_o_mesmo_servico_duas_vezes()
    {
        var os = OsBuilder.EmDiagnostico();
        os.RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]);

        Should.Throw<DomainException>(() =>
                os.RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]))
            .Message.ShouldContain("já está registrado");
    }

    [Fact]
    public void Nao_registra_servicos_fora_do_diagnostico() =>
        Should.Throw<DomainException>(() =>
                OsBuilder.Aberta().RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "X", Dinheiro.De(1m), 10)]))
            .Message.ShouldContain("durante o diagnóstico");

    [Fact]
    public void Registrar_pecas_deixa_o_item_aguardando_reserva()
    {
        var os = OsBuilder.EmDiagnostico();

        os.RegistrarPecas([(OsBuilder.PecaOleo, "Óleo 5W30", 4m, Dinheiro.De(58m))]);

        var item = os.ItensDePeca.ShouldHaveSingleItem();
        item.Status.ShouldBe(StatusItemPeca.AGUARDANDO_RESERVA);
        item.Subtotal.Valor.ShouldBe(232m);
        os.Total.Valor.ShouldBe(232m);
    }

    [Fact]
    public void Peca_marcada_como_pendente_bloqueia_o_avanco_ate_a_entrada()
    {
        var os = OsBuilder.EmDiagnostico();
        os.RegistrarPecas([(OsBuilder.PecaOleo, "Óleo 5W30", 4m, Dinheiro.De(58m))]);

        os.MarcarPecaComoPendenteDeCompra(OsBuilder.PecaOleo);

        os.PossuiPecaPendenteDeCompra.ShouldBeTrue();
    }

    [Fact]
    public void Remover_item_recalcula_o_total()
    {
        var os = OsBuilder.EmDiagnostico();
        os.RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]);
        os.RegistrarPecas([(OsBuilder.PecaOleo, "Óleo 5W30", 4m, Dinheiro.De(58m))]);

        os.RemoverPeca(os.ItensDePeca.First().Id);

        os.Total.Valor.ShouldBe(180m);
    }

    [Fact]
    public void Remover_item_inexistente_e_erro() =>
        Should.Throw<DomainException>(() => OsBuilder.EmDiagnostico().RemoverServico(Guid.NewGuid()))
            .Message.ShouldContain("não encontrado");

    [Fact]
    public void Diagnostico_nao_fecha_sem_item_de_servico()
    {
        var os = OsBuilder.EmDiagnostico();

        Should.Throw<DomainException>(os.FinalizarDiagnostico)
            .Message.ShouldContain("ao menos um item de serviço");
    }

    [Fact]
    public void Diagnostico_nao_fecha_com_peca_aguardando_reserva()
    {
        var os = OsBuilder.EmDiagnostico();
        os.RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]);
        os.RegistrarPecas([(OsBuilder.PecaOleo, "Óleo 5W30", 4m, Dinheiro.De(58m))]);

        Should.Throw<DomainException>(os.FinalizarDiagnostico)
            .Message.ShouldContain("aguardando reserva");
    }

    [Fact]
    public void Diagnostico_fecha_com_peca_pendente_de_compra_e_avisa_no_evento()
    {
        var os = OsBuilder.EmDiagnostico();
        os.RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]);
        os.RegistrarPecas([(OsBuilder.PecaOleo, "Óleo 5W30", 4m, Dinheiro.De(58m))]);
        os.MarcarPecaComoPendenteDeCompra(OsBuilder.PecaOleo);
        os.LimparEventos();

        os.FinalizarDiagnostico();

        os.EventosDeDominio.OfType<DiagnosticoFinalizado>()
            .ShouldHaveSingleItem()
            .PossuiPecaPendenteDeCompra.ShouldBeTrue();
    }

    [Fact]
    public void Finalizar_diagnostico_nao_muda_o_status_sozinho()
    {
        var os = OsBuilder.DiagnosticoCompleto();

        os.FinalizarDiagnostico();

        os.Status.ShouldBe(StatusOS.EM_DIAGNOSTICO);
    }
}

public class OrdemDeServicoExecucaoTests
{
    [Fact]
    public void Enviar_orcamento_leva_a_os_para_aguardando_aprovacao()
    {
        var os = OsBuilder.DiagnosticoCompleto();
        os.FinalizarDiagnostico();
        os.VincularOrcamento(OrcamentoId.Novo());

        os.MarcarOrcamentoEnviado();

        os.Status.ShouldBe(StatusOS.AGUARDANDO_APROVACAO);
    }

    [Fact]
    public void Nao_marca_orcamento_enviado_sem_orcamento_vinculado()
    {
        var os = OsBuilder.DiagnosticoCompleto();
        os.FinalizarDiagnostico();

        Should.Throw<DomainException>(os.MarcarOrcamentoEnviado)
            .Message.ShouldContain("não há orçamento", Case.Insensitive);
    }

    [Fact]
    public void Servico_so_inicia_com_orcamento_aprovado()
    {
        var os = OsBuilder.AguardandoAprovacao();

        Should.Throw<DomainException>(os.IniciarServico)
            .Message.ShouldContain("aprovado pelo cliente");
    }

    [Fact]
    public void Servico_nao_inicia_com_peca_pendente_de_compra()
    {
        var os = OsBuilder.AguardandoAprovacao();
        os.RegistrarAprovacaoDoOrcamento();
        os.MarcarPecaComoPendenteDeCompra(OsBuilder.PecaOleo);

        Should.Throw<DomainException>(os.IniciarServico)
            .Message.ShouldContain("pendentes de compra");
    }

    [Fact]
    public void Iniciar_servico_liga_o_relogio_da_duracao_real()
    {
        var os = OsBuilder.AguardandoAprovacao();
        os.RegistrarAprovacaoDoOrcamento();

        os.IniciarServico();

        os.Status.ShouldBe(StatusOS.EM_EXECUCAO);
        os.Execucao.Inicio.ShouldNotBeNull();
        os.EventosDeDominio.OfType<ServicoIniciado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Confirmar_subtracao_marca_as_pecas_reservadas_como_consumidas()
    {
        var os = OsBuilder.EmExecucao();

        os.ItensDePeca.ShouldHaveSingleItem().Status.ShouldBe(StatusItemPeca.SUBTRAIDA);
    }

    [Fact]
    public void Problema_adicional_exige_descricao() =>
        Should.Throw<DomainException>(() => OsBuilder.EmExecucao().RegistrarProblemaAdicional("  "))
            .Message.ShouldContain("Descreva");

    [Fact]
    public void Itens_adicionais_nascem_aguardando_aprovacao_e_fora_do_total()
    {
        var os = OsBuilder.EmExecucao();
        var totalAntes = os.Total.Valor;

        os.RegistrarItensAdicionais(
            [(ServicoId.Novo(), "Troca de correia", Dinheiro.De(890m), 240)],
            []);

        os.PossuiItemAdicionalPendente.ShouldBeTrue();
        os.Total.Valor.ShouldBe(totalAntes);
        os.EventosDeDominio.OfType<ServicosEPecasAdicionaisRegistrados>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Registrar_itens_adicionais_exige_ao_menos_um_item() =>
        Should.Throw<DomainException>(() => OsBuilder.EmExecucao().RegistrarItensAdicionais([], []))
            .Message.ShouldContain("ao menos um");

    [Fact]
    public void Aprovar_complementar_soma_os_adicionais_ao_total_e_retoma_a_execucao()
    {
        var os = OsBuilder.EmExecucao();
        var totalAntes = os.Total.Valor;
        os.RegistrarItensAdicionais([(ServicoId.Novo(), "Troca de correia", Dinheiro.De(890m), 240)], []);
        os.SuspenderParaAprovacaoComplementar();

        os.AprovarItensAdicionais();

        os.Status.ShouldBe(StatusOS.EM_EXECUCAO);
        os.Total.Valor.ShouldBe(totalAntes + 890m);
        os.PossuiItemAdicionalPendente.ShouldBeFalse();
    }

    [Fact]
    public void Reprovar_complementar_remove_os_adicionais_e_mantem_o_total()
    {
        var os = OsBuilder.EmExecucao();
        var totalAntes = os.Total.Valor;
        var itensAntes = os.ItensDeServico.Count;
        os.RegistrarItensAdicionais([(ServicoId.Novo(), "Troca de correia", Dinheiro.De(890m), 240)], []);
        os.SuspenderParaAprovacaoComplementar();

        os.RemoverItensAdicionais();

        os.Status.ShouldBe(StatusOS.EM_EXECUCAO);
        os.Total.Valor.ShouldBe(totalAntes);
        os.ItensDeServico.Count.ShouldBe(itensAntes);
        os.EventosDeDominio.OfType<ItensAdicionaisRemovidosDaOs>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Os_nao_conclui_com_item_adicional_pendente_de_resposta()
    {
        var os = OsBuilder.EmExecucao();
        os.RegistrarItensAdicionais([(ServicoId.Novo(), "Troca de correia", Dinheiro.De(890m), 240)], []);

        Should.Throw<DomainException>(os.FinalizarServico)
            .Message.ShouldContain("item adicional pendente");
    }

    [Fact]
    public void Finalizar_servico_registra_a_duracao_real()
    {
        var os = OsBuilder.EmExecucao();
        os.LimparEventos();

        os.FinalizarServico();

        os.Status.ShouldBe(StatusOS.FINALIZADA);
        os.Execucao.Fim.ShouldNotBeNull();
        os.Execucao.DuracaoRealEmMinutos.ShouldNotBeNull();
        os.EventosDeDominio.OfType<ServicoConcluido>().ShouldHaveSingleItem();
        os.EventosDeDominio.OfType<DuracaoRealDaExecucaoRegistrada>().ShouldHaveSingleItem();
    }
}

public class OrdemDeServicoEntregaTests
{
    [Fact]
    public void Entrega_fecha_o_ciclo_da_os()
    {
        var os = OsBuilder.Finalizada();
        os.LimparEventos();

        os.RegistrarEntrega();

        os.Status.ShouldBe(StatusOS.ENTREGUE);
        os.EstaAtiva.ShouldBeFalse();
        os.EventosDeDominio.OfType<VeiculoEntregue>().ShouldHaveSingleItem();
    }
}

public class OrdemDeServicoCancelamentoTests
{
    [Theory]
    [InlineData(MotivoCancelamento.REPROVADO_PELO_CLIENTE)]
    [InlineData(MotivoCancelamento.CANCELADO_PELA_OFICINA)]
    public void Cancela_registrando_o_motivo(MotivoCancelamento motivo)
    {
        var os = OsBuilder.AguardandoAprovacao();

        os.Cancelar(motivo, "observação do atendente");

        os.Status.ShouldBe(StatusOS.CANCELADA);
        os.MotivoCancelamento.ShouldBe(motivo);
        os.ObservacaoCancelamento.ShouldBe("observação do atendente");
        os.EstaAtiva.ShouldBeFalse();
        os.EventosDeDominio.OfType<OsCancelada>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Cancela_a_partir_de_qualquer_status_antes_da_conclusao()
    {
        OsBuilder.Aberta().Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA);
        OsBuilder.EmDiagnostico().Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA);
        OsBuilder.AguardandoAprovacao().Cancelar(MotivoCancelamento.REPROVADO_PELO_CLIENTE);
        OsBuilder.EmExecucao().Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA);
    }

    [Fact]
    public void Nao_cancela_depois_do_servico_concluido() =>
        Should.Throw<DomainException>(() =>
                OsBuilder.Finalizada().Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA))
            .Message.ShouldContain("depois do serviço concluído");

    [Fact]
    public void Nao_cancela_os_ja_entregue()
    {
        var os = OsBuilder.Finalizada();
        os.RegistrarEntrega();

        Should.Throw<DomainException>(() => os.Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA));
    }

    [Fact]
    public void Nao_cancela_duas_vezes()
    {
        var os = OsBuilder.Aberta();
        os.Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA);

        Should.Throw<DomainException>(() => os.Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA))
            .Message.ShouldContain("já está cancelada");
    }
}

public class MaquinaDeEstadosTests
{
    [Fact]
    public void Nao_pula_do_recebida_direto_para_execucao()
    {
        var os = OsBuilder.Aberta();

        Should.Throw<DomainException>(os.IniciarServico);
    }

    [Fact]
    public void Nao_inicia_diagnostico_duas_vezes()
    {
        var os = OsBuilder.EmDiagnostico();

        Should.Throw<DomainException>(os.IniciarDiagnostico)
            .Message.ShouldContain("Transição de status inválida");
    }

    [Fact]
    public void Nao_finaliza_servico_que_nao_esta_em_execucao() =>
        Should.Throw<DomainException>(OsBuilder.EmDiagnostico().FinalizarServico);

    [Fact]
    public void Entregue_e_terminal()
    {
        var os = OsBuilder.Finalizada();
        os.RegistrarEntrega();

        Should.Throw<DomainException>(os.RegistrarEntrega);
    }

    [Fact]
    public void Cancelada_e_terminal()
    {
        var os = OsBuilder.Aberta();
        os.Cancelar(MotivoCancelamento.CANCELADO_PELA_OFICINA);

        Should.Throw<DomainException>(os.IniciarDiagnostico);
    }

    [Fact]
    public void Percorre_o_caminho_feliz_inteiro()
    {
        var os = OsBuilder.Aberta();
        os.Status.ShouldBe(StatusOS.RECEBIDA);

        os.IniciarDiagnostico();
        os.Status.ShouldBe(StatusOS.EM_DIAGNOSTICO);

        os.RegistrarServicos([(OsBuilder.ServicoTrocaDeOleo, "Troca de óleo", Dinheiro.De(180m), 45)]);
        os.FinalizarDiagnostico();

        os.VincularOrcamento(OrcamentoId.Novo());
        os.MarcarOrcamentoEnviado();
        os.Status.ShouldBe(StatusOS.AGUARDANDO_APROVACAO);

        os.RegistrarAprovacaoDoOrcamento();
        os.IniciarServico();
        os.Status.ShouldBe(StatusOS.EM_EXECUCAO);

        os.FinalizarServico();
        os.Status.ShouldBe(StatusOS.FINALIZADA);

        os.RegistrarEntrega();
        os.Status.ShouldBe(StatusOS.ENTREGUE);
    }
}

public class JanelaDeExecucaoTests
{
    [Fact]
    public void Janela_nao_iniciada_nao_tem_duracao()
    {
        var janela = JanelaDeExecucao.NaoIniciada();

        janela.Inicio.ShouldBeNull();
        janela.DuracaoReal.ShouldBeNull();
        janela.DuracaoRealEmMinutos.ShouldBeNull();
    }

    [Fact]
    public void Nao_inicia_duas_vezes() =>
        Should.Throw<DomainException>(() => JanelaDeExecucao.NaoIniciada().Iniciar().Iniciar());

    [Fact]
    public void Nao_suspende_execucao_que_nao_comecou() =>
        Should.Throw<DomainException>(() => JanelaDeExecucao.NaoIniciada().Suspender());

    [Fact]
    public void Nao_retoma_execucao_que_nao_esta_suspensa() =>
        Should.Throw<DomainException>(() => JanelaDeExecucao.NaoIniciada().Iniciar().Retomar());

    [Fact]
    public void Suspender_e_retomar_acumula_o_tempo_de_espera()
    {
        var janela = JanelaDeExecucao.NaoIniciada().Iniciar().Suspender();

        var retomada = janela.Retomar();

        retomada.SuspensaEm.ShouldBeNull();
        retomada.TempoAguardandoAprovacao.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Concluir_enquanto_suspensa_fecha_a_suspensao_antes()
    {
        var janela = JanelaDeExecucao.NaoIniciada().Iniciar().Suspender().Concluir();

        janela.Fim.ShouldNotBeNull();
        janela.SuspensaEm.ShouldBeNull();
        janela.DuracaoReal.ShouldNotBeNull();
    }

    [Fact]
    public void Nao_conclui_duas_vezes() =>
        Should.Throw<DomainException>(() => JanelaDeExecucao.NaoIniciada().Iniciar().Concluir().Concluir());
}
