using OficinaDigital.Application.Atendimento;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Orcamentos;
using OficinaDigital.Application.Tests.Infra;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.ValueObjects;
using Shouldly;

namespace OficinaDigital.Application.Tests;

public class OrcamentoComplementarEIndicadoresTests : IDisposable
{
    private readonly AmbienteDeTeste _ambiente = new();
    private readonly Cenario _cenario;

    public OrcamentoComplementarEIndicadoresTests() => _cenario = new Cenario(_ambiente);

    public void Dispose() => _ambiente.Dispose();

    private async Task<(OrdemDeServicoResponse Os, OrcamentoResponse Complementar)> ComComplementarAsync()
    {
        var os = await _cenario.EmExecucaoAsync();
        var servicoExtra = await _cenario.CadastrarServicoAsync("Troca de correia", 890m, 240);

        await _cenario.Ordens.RegistrarProblemaAdicionalAsync(os.Id,
            new RegistrarProblemaAdicionalRequest("Correia dentada com folga"));

        await _cenario.Ordens.RegistrarItensAdicionaisAsync(os.Id,
            new RegistrarItensAdicionaisRequest([new ItemDeServicoRequest(servicoExtra.Id)], null));

        var complementar = (await _cenario.Orcamentos.ListarPorOsAsync(os.Id))
            .Single(o => o.Tipo == TipoOrcamento.COMPLEMENTAR);

        return (await _cenario.Ordens.ObterPorIdAsync(os.Id), complementar);
    }

    [Fact]
    public async Task Itens_adicionais_geram_complementar_e_suspendem_a_execucao()
    {
        var (os, complementar) = await ComComplementarAsync();

        os.Status.ShouldBe(StatusOS.AGUARDANDO_APROVACAO);
        os.PossuiItemAdicionalPendenteNaResposta();
        complementar.TotalVigente.ShouldBe(890m);
        complementar.Versoes.Single().EnviadoEm.ShouldNotBeNull();
    }

    [Fact]
    public async Task Complementar_aprovado_retoma_a_execucao_e_soma_ao_total()
    {
        var (os, complementar) = await ComComplementarAsync();
        var totalAntes = os.Total;

        await _cenario.Orcamentos.ResponderAsync(complementar.Id,
            new ResponderOrcamentoRequest(DecisaoCliente.APROVADO), null);

        var atualizada = await _cenario.Ordens.ObterPorIdAsync(os.Id);
        atualizada.Status.ShouldBe(StatusOS.EM_EXECUCAO);
        atualizada.Total.ShouldBe(totalAntes + 890m);
        atualizada.ItensDeServico.ShouldAllBe(i => !i.AguardandoAprovacaoDoCliente);
    }

    [Fact]
    public async Task Complementar_reprovado_remove_os_itens_e_retoma_a_execucao()
    {
        var (os, complementar) = await ComComplementarAsync();
        var totalAntes = os.Total;

        await _cenario.Orcamentos.ResponderAsync(complementar.Id,
            new ResponderOrcamentoRequest(DecisaoCliente.REPROVADO), null);

        var atualizada = await _cenario.Ordens.ObterPorIdAsync(os.Id);
        atualizada.Status.ShouldBe(StatusOS.EM_EXECUCAO);
        atualizada.Total.ShouldBe(totalAntes);
        atualizada.ItensDeServico.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Os_com_complementar_pendente_nao_conclui()
    {
        var (os, _) = await ComComplementarAsync();

        await Should.ThrowAsync<DomainException>(() => _cenario.Ordens.FinalizarServicoAsync(os.Id));
    }

    [Fact]
    public async Task Tempo_parado_aguardando_o_complementar_sai_da_duracao_real()
    {
        var (os, complementar) = await ComComplementarAsync();

        await _cenario.Orcamentos.ResponderAsync(complementar.Id,
            new ResponderOrcamentoRequest(DecisaoCliente.APROVADO), null);

        var finalizada = await _cenario.Ordens.FinalizarServicoAsync(os.Id);

        finalizada.Status.ShouldBe(StatusOS.FINALIZADA);
        finalizada.Execucao.DuracaoRealEmMinutos.ShouldNotBeNull();
        finalizada.Execucao.DuracaoRealEmMinutos.Value.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Alterar_orcamento_cria_nova_versao_e_preserva_a_anterior()
    {
        var (_, orcamento) = await _cenario.ComOrcamentoGeradoAsync();
        await _cenario.Orcamentos.EnviarAsync(orcamento.Id);

        var alterado = await _cenario.Orcamentos.AlterarAsync(orcamento.Id);

        alterado.VersaoVigente.ShouldBe(2);
        alterado.Versoes.Count.ShouldBe(2);
        alterado.Versoes.First(v => v.Numero == 1).Vigente.ShouldBeFalse();
    }

    [Fact]
    public async Task Nao_altera_orcamento_ja_respondido()
    {
        var (_, orcamento) = await _cenario.ComOrcamentoGeradoAsync();
        await _cenario.Orcamentos.EnviarAsync(orcamento.Id);
        await _cenario.Orcamentos.ResponderAsync(orcamento.Id,
            new ResponderOrcamentoRequest(DecisaoCliente.APROVADO), null);

        await Should.ThrowAsync<DomainException>(() => _cenario.Orcamentos.AlterarAsync(orcamento.Id));
    }

    [Fact]
    public async Task Cliente_nao_responde_orcamento_de_outro_cliente()
    {
        var (_, orcamento) = await _cenario.ComOrcamentoGeradoAsync();
        await _cenario.Orcamentos.EnviarAsync(orcamento.Id);

        var outroCliente = await _cenario.CadastrarClienteAsync(Cenario.CpfOutroCliente, "João");

        await Should.ThrowAsync<AcessoNegadoException>(() => _cenario.Orcamentos.ResponderAsync(
            orcamento.Id, new ResponderOrcamentoRequest(DecisaoCliente.APROVADO),
            new ClienteId(outroCliente.Id)));
    }

    [Fact]
    public async Task Indicador_de_tempo_medio_ignora_os_que_nunca_executaram()
    {
        var os = await _cenario.EmExecucaoAsync();
        await _cenario.Ordens.FinalizarServicoAsync(os.Id);

        var cliente = await _cenario.CadastrarClienteAsync(Cenario.CpfOutroCliente, "João");
        var veiculo = await _cenario.CadastrarVeiculoAsync(cliente.Id, "XYZ9K88");
        var cancelada = await _cenario.Ordens.AbrirAsync(new AbrirOsRequest(cliente.Id, veiculo.Id, null));
        await _cenario.Ordens.CancelarAsync(cancelada.Id,
            new CancelarOsRequest(MotivoCancelamento.CANCELADO_PELA_OFICINA, "desistência"));

        var indicadores = _ambiente.Servico<IndicadorService>();
        var painel = await indicadores.ObterTempoMedioDeExecucaoAsync(null, null);

        painel.TotalDeOsConsideradas.ShouldBe(1);
        painel.PorServico.ShouldHaveSingleItem().Servico.ShouldBe("Troca de óleo");
        painel.PorServico[0].TempoPadraoEmMinutos.ShouldBe(45);
    }

    [Fact]
    public async Task Indicador_vazio_quando_nenhuma_os_foi_concluida()
    {
        await _cenario.EmExecucaoAsync();

        var painel = await _ambiente.Servico<IndicadorService>().ObterTempoMedioDeExecucaoAsync(null, null);

        painel.TotalDeOsConsideradas.ShouldBe(0);
        painel.PorServico.ShouldBeEmpty();
    }

    [Fact]
    public async Task Indicador_rejeita_periodo_invertido() =>
        await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _ambiente.Servico<IndicadorService>()
                .ObterTempoMedioDeExecucaoAsync(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));
}

internal static class AsercoesDeOs
{
    public static void PossuiItemAdicionalPendenteNaResposta(this OrdemDeServicoResponse os) =>
        os.ItensDeServico.ShouldContain(i => i.AguardandoAprovacaoDoCliente);
}
