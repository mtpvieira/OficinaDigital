using OficinaDigital.Domain.Common;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Common;

public class DinheiroTests
{
    [Fact]
    public void Zero_vale_zero_em_reais()
    {
        Dinheiro.Zero.Valor.ShouldBe(0m);
        Dinheiro.Zero.Moeda.ShouldBe("BRL");
    }

    [Theory]
    [InlineData(10.004, 10.00)]
    [InlineData(10.005, 10.01)]
    [InlineData(10.006, 10.01)]
    public void Arredonda_para_duas_casas(decimal entrada, decimal esperado) =>
        Dinheiro.De(entrada).Valor.ShouldBe(esperado);

    [Fact]
    public void Nao_aceita_valor_negativo() =>
        Should.Throw<DomainException>(() => Dinheiro.De(-1m))
            .Message.ShouldContain("negativo");

    [Fact]
    public void Soma_valores() =>
        Dinheiro.De(180m).Somar(Dinheiro.De(42.50m)).Valor.ShouldBe(222.50m);

    [Fact]
    public void Subtrai_valores() =>
        Dinheiro.De(180m).Subtrair(Dinheiro.De(80m)).Valor.ShouldBe(100m);

    [Fact]
    public void Nao_subtrai_alem_do_saldo() =>
        Should.Throw<DomainException>(() => Dinheiro.De(50m).Subtrair(Dinheiro.De(80m)));

    [Fact]
    public void Multiplica_por_quantidade() =>
        Dinheiro.De(58m).Multiplicar(2.5m).Valor.ShouldBe(145m);

    [Fact]
    public void Nao_multiplica_por_fator_negativo() =>
        Should.Throw<DomainException>(() => Dinheiro.De(10m).Multiplicar(-1m));

    [Fact]
    public void Nao_opera_moedas_diferentes() =>
        Should.Throw<DomainException>(() => Dinheiro.De(10m).Somar(Dinheiro.De(10m, "USD")))
            .Message.ShouldContain("moedas diferentes");

    [Fact]
    public void Compara_por_conteudo()
    {
        Dinheiro.De(10m).ShouldBe(Dinheiro.De(10m));
        Dinheiro.De(10m).ShouldNotBe(Dinheiro.De(11m));
        Dinheiro.De(11m).MaiorQue(Dinheiro.De(10m)).ShouldBeTrue();
    }
}

public class QuantidadeTests
{
    [Fact]
    public void Cria_quantidade_com_unidade()
    {
        var quantidade = Quantidade.De(3.5m, UnidadeDeMedida.L);

        quantidade.Valor.ShouldBe(3.5m);
        quantidade.Unidade.ShouldBe(UnidadeDeMedida.L);
    }

    [Fact]
    public void Nao_aceita_quantidade_negativa() =>
        Should.Throw<DomainException>(() => Quantidade.De(-1m, UnidadeDeMedida.UN));

    [Fact]
    public void Soma_e_subtrai_na_mesma_unidade()
    {
        var a = Quantidade.De(10m, UnidadeDeMedida.UN);
        var b = Quantidade.De(4m, UnidadeDeMedida.UN);

        a.Somar(b).Valor.ShouldBe(14m);
        a.Subtrair(b).Valor.ShouldBe(6m);
    }

    [Fact]
    public void Nao_deixa_a_subtracao_ficar_negativa() =>
        Should.Throw<DomainException>(() =>
            Quantidade.De(2m, UnidadeDeMedida.UN).Subtrair(Quantidade.De(3m, UnidadeDeMedida.UN)));

    [Fact]
    public void Nao_opera_unidades_diferentes() =>
        Should.Throw<DomainException>(() =>
                Quantidade.De(1m, UnidadeDeMedida.L).Somar(Quantidade.De(1m, UnidadeDeMedida.KG)))
            .Message.ShouldContain("unidades diferentes");

    [Fact]
    public void Zero_reconhece_a_si_mesmo() =>
        Quantidade.Zero(UnidadeDeMedida.UN).EhZero.ShouldBeTrue();
}

public class DuracaoEPrecoTests
{
    [Fact]
    public void Duracao_exige_minutos_positivos()
    {
        Duracao.DeMinutos(45).Minutos.ShouldBe(45);
        Should.Throw<DomainException>(() => Duracao.DeMinutos(0));
        Should.Throw<DomainException>(() => Duracao.DeMinutos(-10));
    }

    [Fact]
    public void Duracao_converte_horas_em_minutos() =>
        Duracao.DeHoras(1.5m).Minutos.ShouldBe(90);

    [Fact]
    public void Preco_vigente_exige_valor_maior_que_zero() =>
        Should.Throw<DomainException>(() =>
            PrecoVigente.De(Dinheiro.Zero, DateOnly.FromDateTime(DateTime.UtcNow)));
}
