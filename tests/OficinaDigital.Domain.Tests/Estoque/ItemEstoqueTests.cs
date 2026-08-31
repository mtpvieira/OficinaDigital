using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Estoque;
using OficinaDigital.Domain.Estoque.Events;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Estoque;

public class ItemEstoqueTests
{
    private static readonly PecaId Peca = PecaId.Novo();

    private static ItemEstoque ComSaldo(decimal saldo, decimal minimo = 0m)
    {
        var item = ItemEstoque.Abrir(Peca, UnidadeDeMedida.UN, minimo);
        if (saldo > 0) item.RegistrarEntrada(saldo, Dinheiro.De(10m));
        item.LimparEventos();
        return item;
    }

    [Fact]
    public void Abre_posicao_com_saldo_e_reservado_zerados()
    {
        var item = ItemEstoque.Abrir(Peca, UnidadeDeMedida.UN, 5m);

        item.Saldo.Valor.ShouldBe(0m);
        item.Reservado.Valor.ShouldBe(0m);
        item.Disponivel.Valor.ShouldBe(0m);
        item.EstoqueMinimo.Valor.ShouldBe(5m);
    }

    [Fact]
    public void Entrada_soma_ao_saldo_e_publica_evento()
    {
        var item = ItemEstoque.Abrir(Peca, UnidadeDeMedida.UN, 0m);

        item.RegistrarEntrada(10m, Dinheiro.De(25m));

        item.Saldo.Valor.ShouldBe(10m);
        item.Disponivel.Valor.ShouldBe(10m);
        item.Movimentos.ShouldHaveSingleItem().Tipo.ShouldBe(TipoMovimentoEstoque.ENTRADA);
        item.EventosDeDominio.OfType<EntradaDePecasRegistrada>().ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Entrada_exige_quantidade_positiva(decimal quantidade) =>
        Should.Throw<DomainException>(() =>
            ItemEstoque.Abrir(Peca, UnidadeDeMedida.UN, 0m).RegistrarEntrada(quantidade, Dinheiro.De(1m)));

    [Fact]
    public void Entrada_exige_custo_unitario() =>
        Should.Throw<DomainException>(() =>
                ItemEstoque.Abrir(Peca, UnidadeDeMedida.UN, 0m).RegistrarEntrada(10m, Dinheiro.Zero))
            .Message.ShouldContain("custo unitário");

    [Fact]
    public void Reserva_reduz_o_disponivel_sem_mexer_no_saldo_fisico()
    {
        var item = ComSaldo(10m);
        var os = OrdemDeServicoId.Novo();

        item.Reservar(os, 4m);

        item.Saldo.Valor.ShouldBe(10m);
        item.Reservado.Valor.ShouldBe(4m);
        item.Disponivel.Valor.ShouldBe(6m);
        item.EventosDeDominio.OfType<PecaReservada>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Nao_reserva_acima_do_disponivel()
    {
        var item = ComSaldo(10m);
        item.Reservar(OrdemDeServicoId.Novo(), 8m);

        Should.Throw<DomainException>(() => item.Reservar(OrdemDeServicoId.Novo(), 3m))
            .Message.ShouldContain("indisponível");

        item.Reservado.Valor.ShouldBe(8m);
    }

    [Fact]
    public void Reserva_no_limite_exato_do_disponivel_e_aceita()
    {
        var item = ComSaldo(10m);

        item.Reservar(OrdemDeServicoId.Novo(), 10m);

        item.Disponivel.Valor.ShouldBe(0m);
    }

    [Fact]
    public void Reservar_de_novo_para_a_mesma_os_acumula_em_uma_reserva_so()
    {
        var item = ComSaldo(10m);
        var os = OrdemDeServicoId.Novo();

        item.Reservar(os, 3m);
        item.Reservar(os, 2m);

        item.Reservas.ShouldHaveSingleItem().Quantidade.ShouldBe(5m);
        item.Reservado.Valor.ShouldBe(5m);
    }

    [Fact]
    public void PodeReservar_responde_sem_alterar_o_estado()
    {
        var item = ComSaldo(5m);

        item.PodeReservar(5m).ShouldBeTrue();
        item.PodeReservar(6m).ShouldBeFalse();
        item.PodeReservar(0m).ShouldBeFalse();
        item.Reservado.Valor.ShouldBe(0m);
    }

    [Fact]
    public void Liberar_reserva_devolve_ao_disponivel()
    {
        var item = ComSaldo(10m);
        var os = OrdemDeServicoId.Novo();
        item.Reservar(os, 4m);
        item.LimparEventos();

        item.LiberarReserva(os);

        item.Reservado.Valor.ShouldBe(0m);
        item.Disponivel.Valor.ShouldBe(10m);
        item.Reservas.ShouldBeEmpty();
        item.EventosDeDominio.OfType<ReservaDePecaLiberada>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Liberar_reserva_de_os_sem_reserva_nao_faz_nada()
    {
        var item = ComSaldo(10m);

        item.LiberarReserva(OrdemDeServicoId.Novo());

        item.EventosDeDominio.OfType<ReservaDePecaLiberada>().ShouldBeEmpty();
    }

    [Fact]
    public void Liberar_a_reserva_de_uma_os_nao_toca_na_reserva_da_outra()
    {
        var item = ComSaldo(10m);
        var osA = OrdemDeServicoId.Novo();
        var osB = OrdemDeServicoId.Novo();
        item.Reservar(osA, 3m);
        item.Reservar(osB, 2m);

        item.LiberarReserva(osA);

        item.Reservado.Valor.ShouldBe(2m);
        item.QuantidadeReservadaPara(osB).ShouldBe(2m);
    }

    [Fact]
    public void Subtracao_consome_a_reserva_e_derruba_saldo_e_reservado_juntos()
    {
        var item = ComSaldo(10m);
        var os = OrdemDeServicoId.Novo();
        item.Reservar(os, 4m);
        item.LimparEventos();

        item.Subtrair(os);

        item.Saldo.Valor.ShouldBe(6m);
        item.Reservado.Valor.ShouldBe(0m);
        item.Disponivel.Valor.ShouldBe(6m);
        item.EventosDeDominio.OfType<PecaSubtraidaDoEstoque>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Nao_subtrai_sem_reserva_previa()
    {
        var item = ComSaldo(10m);

        Should.Throw<DomainException>(() => item.Subtrair(OrdemDeServicoId.Novo()))
            .Message.ShouldContain("reserva");
    }

    [Fact]
    public void Saldo_nunca_fica_negativo_apos_subtracao()
    {
        var item = ComSaldo(5m);
        var os = OrdemDeServicoId.Novo();
        item.Reservar(os, 5m);

        item.Subtrair(os);

        item.Saldo.Valor.ShouldBe(0m);
    }

    [Fact]
    public void Emite_alerta_quando_o_disponivel_cai_abaixo_do_minimo()
    {
        var item = ComSaldo(10m, minimo: 8m);
        var os = OrdemDeServicoId.Novo();

        item.Reservar(os, 5m);

        item.AbaixoDoEstoqueMinimo.ShouldBeTrue();
        item.EventosDeDominio.OfType<AlertaDeEstoqueMinimoEmitido>()
            .ShouldHaveSingleItem()
            .Disponivel.ShouldBe(5m);
    }

    [Fact]
    public void Nao_emite_alerta_quando_o_disponivel_esta_acima_do_minimo()
    {
        var item = ComSaldo(10m, minimo: 3m);

        item.Reservar(OrdemDeServicoId.Novo(), 2m);

        item.AbaixoDoEstoqueMinimo.ShouldBeFalse();
        item.EventosDeDominio.OfType<AlertaDeEstoqueMinimoEmitido>().ShouldBeEmpty();
    }

    [Fact]
    public void Definir_estoque_minimo_publica_evento_e_reavalia_a_reposicao()
    {
        var item = ComSaldo(5m);

        item.DefinirEstoqueMinimo(10m);

        item.EstoqueMinimo.Valor.ShouldBe(10m);
        item.EventosDeDominio.OfType<EstoqueMinimoDefinido>().ShouldHaveSingleItem();
        item.EventosDeDominio.OfType<AlertaDeEstoqueMinimoEmitido>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Sinalizar_falta_publica_a_identificacao_e_a_compra()
    {
        var item = ComSaldo(2m);
        var os = OrdemDeServicoId.Novo();

        item.SinalizarFalta(os, 5m);

        var falta = item.EventosDeDominio.OfType<FaltaDePecaIdentificada>().ShouldHaveSingleItem();
        falta.QuantidadeSolicitada.ShouldBe(5m);
        falta.QuantidadeDisponivel.ShouldBe(2m);

        item.EventosDeDominio.OfType<CompraDePecaSinalizada>()
            .ShouldHaveSingleItem()
            .QuantidadeFaltante.ShouldBe(3m);
    }
}
