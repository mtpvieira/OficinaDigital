using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Catalogo.Events;
using OficinaDigital.Domain.Catalogo.ValueObjects;
using OficinaDigital.Domain.Common;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Catalogo;

public class SkuTests
{
    [Theory]
    [InlineData("oleo-5w30", "OLEO-5W30")]
    [InlineData(" filtro.01 ", "FILTRO.01")]
    public void Normaliza_para_maiusculo_e_sem_espacos(string entrada, string esperado) =>
        Sku.Criar(entrada).Codigo.ShouldBe(esperado);

    [Theory]
    [InlineData("AB")]                                  // curto demais
    [InlineData("SKU COM ESPACO")]                      // espaço no meio
    [InlineData("SKU#01")]                              // caractere não permitido
    public void Rejeita_sku_fora_do_formato(string entrada) =>
        Should.Throw<DomainException>(() => Sku.Criar(entrada));

    [Fact]
    public void Rejeita_sku_vazio() =>
        Should.Throw<DomainException>(() => Sku.Criar(null)).Message.ShouldContain("obrigatório");
}

public class ServicoTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private static Servico ServicoValido() =>
        Servico.Cadastrar("Troca de óleo", "Motor e filtro", Dinheiro.De(180m), 45, Hoje);

    [Fact]
    public void Cadastra_servico_ativo_com_preco_e_tempo_padrao()
    {
        var servico = ServicoValido();

        servico.Nome.ShouldBe("Troca de óleo");
        servico.Ativo.ShouldBeTrue();
        servico.TempoPadraoExecucao.Minutos.ShouldBe(45);
        servico.PrecoVigenteEm(Hoje).Valor.ShouldBe(180m);
        servico.EventosDeDominio.OfType<ServicoCadastradoNoCatalogo>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Nao_cadastra_sem_nome() =>
        Should.Throw<DomainException>(() => Servico.Cadastrar(null, null, Dinheiro.De(10m), 30));

    [Fact]
    public void Nao_cadastra_com_tempo_padrao_zerado() =>
        Should.Throw<DomainException>(() => Servico.Cadastrar("X", null, Dinheiro.De(10m), 0));

    [Fact]
    public void Alterar_preco_cria_nova_vigencia_e_preserva_o_historico()
    {
        var servico = ServicoValido();
        var amanha = Hoje.AddDays(1);

        servico.AlterarPreco(Dinheiro.De(200m), amanha);

        servico.Precos.Count.ShouldBe(2);
        servico.PrecoVigenteEm(Hoje).Valor.ShouldBe(180m);
        servico.PrecoVigenteEm(amanha).Valor.ShouldBe(200m);
        servico.EventosDeDominio.OfType<PrecoDoServicoAlterado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Alterar_preco_na_mesma_data_substitui_a_vigencia()
    {
        var servico = ServicoValido();

        servico.AlterarPreco(Dinheiro.De(210m), Hoje);

        servico.Precos.Count.ShouldBe(1);
        servico.PrecoVigenteEm(Hoje).Valor.ShouldBe(210m);
    }

    [Fact]
    public void Nao_ha_preco_vigente_antes_da_primeira_vigencia() =>
        Should.Throw<DomainException>(() => ServicoValido().PrecoVigenteEm(Hoje.AddDays(-1)))
            .Message.ShouldContain("preço vigente");

    [Fact]
    public void Inativa_e_publica_evento()
    {
        var servico = ServicoValido();
        servico.LimparEventos();

        servico.Inativar();

        servico.Ativo.ShouldBeFalse();
        servico.EventosDeDominio.OfType<ServicoInativado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Servico_inativo_nao_aceita_alteracao_nem_preco_novo()
    {
        var servico = ServicoValido();
        servico.Inativar();

        Should.Throw<DomainException>(() => servico.AlterarDados("Outro", null, 60));
        Should.Throw<DomainException>(() => servico.AlterarPreco(Dinheiro.De(1m), Hoje));
    }
}

public class PecaTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private static Peca PecaValida() =>
        Peca.Cadastrar("OLEO-5W30", "Óleo sintético 5W30", UnidadeDeMedida.L, Dinheiro.De(58m), 20m, Hoje);

    [Fact]
    public void Cadastra_peca_ativa_com_sku_unidade_e_preco()
    {
        var peca = PecaValida();

        peca.Sku.Codigo.ShouldBe("OLEO-5W30");
        peca.Unidade.ShouldBe(UnidadeDeMedida.L);
        peca.Ativa.ShouldBeTrue();
        peca.PrecoVigenteEm(Hoje).Valor.ShouldBe(58m);
    }

    [Fact]
    public void Publica_evento_com_o_estoque_minimo_para_a_politica_abrir_a_posicao()
    {
        var peca = PecaValida();

        var evento = peca.EventosDeDominio.OfType<PecaCadastrada>().ShouldHaveSingleItem();
        evento.EstoqueMinimo.ShouldBe(20m);
        evento.Unidade.ShouldBe(UnidadeDeMedida.L);
    }

    [Fact]
    public void Nao_cadastra_sem_descricao() =>
        Should.Throw<DomainException>(() =>
            Peca.Cadastrar("SKU-01", null, UnidadeDeMedida.UN, Dinheiro.De(10m), 1m));

    [Fact]
    public void Nao_cadastra_com_estoque_minimo_negativo() =>
        Should.Throw<DomainException>(() =>
            Peca.Cadastrar("SKU-01", "X", UnidadeDeMedida.UN, Dinheiro.De(10m), -1m));

    [Fact]
    public void Inativa_e_bloqueia_alteracoes()
    {
        var peca = PecaValida();
        peca.Inativar();

        peca.Ativa.ShouldBeFalse();
        peca.EventosDeDominio.OfType<PecaInativada>().ShouldHaveSingleItem();
        Should.Throw<DomainException>(() => peca.AlterarDados("Outra descrição"));
    }
}
