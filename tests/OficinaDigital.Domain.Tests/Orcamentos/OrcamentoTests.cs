using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos;
using OficinaDigital.Domain.Orcamentos.Events;
using OficinaDigital.Domain.Orcamentos.ValueObjects;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Orcamentos;

public class OrcamentoTests
{
    private static readonly OrdemDeServicoId Os = OrdemDeServicoId.Novo();

    private static List<(TipoItemOrcado, Guid, string, decimal, Dinheiro)> Itens(decimal precoServico = 180m) =>
    [
        (TipoItemOrcado.SERVICO, Guid.NewGuid(), "Troca de óleo", 1m, Dinheiro.De(precoServico)),
        (TipoItemOrcado.PECA, Guid.NewGuid(), "Óleo 5W30", 4m, Dinheiro.De(58m))
    ];

    private static Orcamento Gerado(TipoOrcamento tipo = TipoOrcamento.PRINCIPAL) =>
        Orcamento.Gerar(Os, tipo, Itens());

    private static Orcamento Enviado()
    {
        var orcamento = Gerado();
        orcamento.Enviar();
        return orcamento;
    }

    [Fact]
    public void Gera_a_versao_1_como_vigente_e_soma_os_itens()
    {
        var orcamento = Gerado();

        orcamento.VersaoVigente.ShouldBe(1);
        orcamento.Versoes.ShouldHaveSingleItem();
        orcamento.ObterVersaoVigente().Total.Valor.ShouldBe(180m + 4m * 58m);
    }

    [Fact]
    public void Publica_evento_de_orcamento_gerado_com_o_total()
    {
        var orcamento = Gerado();

        orcamento.EventosDeDominio.OfType<OrcamentoGerado>()
            .ShouldHaveSingleItem()
            .Total.ShouldBe(412m);
    }

    [Fact]
    public void Nao_gera_orcamento_sem_itens() =>
        Should.Throw<DomainException>(() => Orcamento.Gerar(Os, TipoOrcamento.PRINCIPAL, []))
            .Message.ShouldContain("ao menos um item");

    [Fact]
    public void Item_orcado_exige_quantidade_positiva() =>
        Should.Throw<DomainException>(() => Orcamento.Gerar(Os, TipoOrcamento.PRINCIPAL,
            [(TipoItemOrcado.PECA, Guid.NewGuid(), "Óleo", 0m, Dinheiro.De(58m))]));

    [Fact]
    public void Enviar_marca_a_versao_e_publica_o_evento()
    {
        var orcamento = Gerado();
        orcamento.LimparEventos();

        orcamento.Enviar();

        orcamento.ObterVersaoVigente().EnviadoEm.ShouldNotBeNull();

        var evento = orcamento.EventosDeDominio.OfType<OrcamentoEnviadoAoCliente>().ShouldHaveSingleItem();
        evento.Versao.ShouldBe(1);
    }

    [Fact]
    public void Nao_envia_versao_ja_respondida()
    {
        var orcamento = Enviado();
        orcamento.Responder(DecisaoCliente.APROVADO);

        Should.Throw<DomainException>(orcamento.Enviar)
            .Message.ShouldContain("já foi respondida");
    }

    [Fact]
    public void Alterar_cria_nova_versao_vigente_e_preserva_a_anterior()
    {
        var orcamento = Gerado();

        orcamento.Alterar(Itens(precoServico: 250m));

        orcamento.VersaoVigente.ShouldBe(2);
        orcamento.Versoes.Count.ShouldBe(2);
        orcamento.Versoes.First(v => v.Numero == 1).Total.Valor.ShouldBe(412m);
        orcamento.ObterVersaoVigente().Total.Valor.ShouldBe(482m);
        orcamento.EventosDeDominio.OfType<OrcamentoAlterado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Nao_altera_versao_ja_respondida_pelo_cliente()
    {
        var orcamento = Enviado();
        orcamento.Responder(DecisaoCliente.REPROVADO);

        Should.Throw<DomainException>(() => orcamento.Alterar(Itens()))
            .Message.ShouldContain("já foi respondida");
    }

    [Fact]
    public void Nova_versao_precisa_ser_enviada_de_novo()
    {
        var orcamento = Enviado();
        orcamento.Alterar(Itens(250m));

        orcamento.ObterVersaoVigente().EnviadoEm.ShouldBeNull();
        Should.Throw<DomainException>(() => orcamento.Responder(DecisaoCliente.APROVADO))
            .Message.ShouldContain("ainda não foi enviado");
    }

    [Fact]
    public void Aprovar_publica_o_evento_pivotal()
    {
        var orcamento = Enviado();
        orcamento.LimparEventos();

        orcamento.Responder(DecisaoCliente.APROVADO);

        orcamento.EstaAprovadoEVigente().ShouldBeTrue();
        orcamento.EventosDeDominio.OfType<OrcamentoAprovado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Reprovar_publica_o_evento_de_reprovacao()
    {
        var orcamento = Enviado();
        orcamento.LimparEventos();

        orcamento.Responder(DecisaoCliente.REPROVADO);

        orcamento.EstaAprovadoEVigente().ShouldBeFalse();
        orcamento.EventosDeDominio.OfType<OrcamentoReprovado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Resposta_e_irreversivel()
    {
        var orcamento = Enviado();
        orcamento.Responder(DecisaoCliente.REPROVADO);

        Should.Throw<DomainException>(() => orcamento.Responder(DecisaoCliente.APROVADO))
            .Message.ShouldContain("irreversível");
    }

    [Fact]
    public void Nao_responde_orcamento_que_nao_foi_enviado() =>
        Should.Throw<DomainException>(() => Gerado().Responder(DecisaoCliente.APROVADO))
            .Message.ShouldContain("ainda não foi enviado");

    [Fact]
    public void Complementar_tem_ciclo_proprio()
    {
        var complementar = Gerado(TipoOrcamento.COMPLEMENTAR);
        complementar.Enviar();
        complementar.Responder(DecisaoCliente.APROVADO);

        complementar.Tipo.ShouldBe(TipoOrcamento.COMPLEMENTAR);
        complementar.EventosDeDominio.OfType<OrcamentoAprovado>()
            .ShouldHaveSingleItem()
            .Tipo.ShouldBe(TipoOrcamento.COMPLEMENTAR);
    }
}
