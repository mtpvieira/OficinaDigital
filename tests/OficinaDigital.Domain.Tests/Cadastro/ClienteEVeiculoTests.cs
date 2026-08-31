using OficinaDigital.Domain.Cadastro;
using OficinaDigital.Domain.Cadastro.Events;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Tests.Common;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Cadastro;

public class ClienteTests
{
    private static Contato Email(string valor = "cliente@oficina.com.br") =>
        Contato.Criar(CanalContato.EMAIL, valor);

    private static Contato Telefone(string valor = "11987654321") =>
        Contato.Criar(CanalContato.SMS, valor);

    private static Cliente ClienteValido() =>
        Cliente.Cadastrar("Maria Silva", DadosValidos.CpfA, [Email()]);

    [Fact]
    public void Cadastra_cliente_ativo_com_documento_e_contato()
    {
        var cliente = ClienteValido();

        cliente.Nome.ShouldBe("Maria Silva");
        cliente.Documento.Numero.ShouldBe(DadosValidos.CpfA);
        cliente.Ativo.ShouldBeTrue();
        cliente.Contatos.Count.ShouldBe(1);
    }

    [Fact]
    public void Publica_evento_de_cliente_cadastrado()
    {
        var cliente = ClienteValido();

        var evento = cliente.EventosDeDominio.OfType<ClienteCadastrado>().ShouldHaveSingleItem();
        evento.ClienteId.ShouldBe(cliente.Id);
        evento.Documento.ShouldBe(DadosValidos.CpfA);
    }

    [Fact]
    public void Nao_cadastra_sem_nenhum_contato() =>
        Should.Throw<DomainException>(() => Cliente.Cadastrar("Maria", DadosValidos.CpfA, []))
            .Message.ShouldContain("Ao menos um contato");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Nao_cadastra_sem_nome(string? nome) =>
        Should.Throw<DomainException>(() => Cliente.Cadastrar(nome, DadosValidos.CpfA, [Email()]))
            .Message.ShouldContain("Nome");

    [Fact]
    public void Nao_cadastra_com_documento_invalido() =>
        Should.Throw<DomainException>(() => Cliente.Cadastrar("Maria", "52998224724", [Email()]));

    [Fact]
    public void Descarta_contatos_repetidos()
    {
        var cliente = Cliente.Cadastrar("Maria", DadosValidos.CpfA, [Email(), Email()]);

        cliente.Contatos.Count.ShouldBe(1);
    }

    [Fact]
    public void Altera_dados_e_publica_evento()
    {
        var cliente = ClienteValido();
        cliente.LimparEventos();

        cliente.AlterarDados("Maria Silva Souza", [Telefone()],
            Endereco.Criar("Rua A", "1", null, "São Paulo", "SP", "01310100"));

        cliente.Nome.ShouldBe("Maria Silva Souza");
        cliente.Contatos.ShouldHaveSingleItem().Canal.ShouldBe(CanalContato.SMS);
        cliente.Endereco.ShouldNotBeNull();
        cliente.EventosDeDominio.OfType<DadosDoClienteAlterados>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Nao_altera_cliente_inativo()
    {
        var cliente = ClienteValido();
        cliente.Inativar();

        Should.Throw<DomainException>(() => cliente.AlterarDados("Outro Nome", [Email()], null))
            .Message.ShouldContain("inativo");
    }

    [Fact]
    public void Nao_altera_removendo_todos_os_contatos()
    {
        var cliente = ClienteValido();

        Should.Throw<DomainException>(() => cliente.AlterarDados("Maria", [], null))
            .Message.ShouldContain("Ao menos um contato");
    }

    [Fact]
    public void Inativa_e_publica_evento()
    {
        var cliente = ClienteValido();
        cliente.LimparEventos();

        cliente.Inativar();

        cliente.Ativo.ShouldBeFalse();
        cliente.EventosDeDominio.OfType<ClienteInativado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Inativar_duas_vezes_nao_publica_evento_repetido()
    {
        var cliente = ClienteValido();
        cliente.Inativar();
        cliente.LimparEventos();

        cliente.Inativar();

        cliente.EventosDeDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Contato_preferencial_prioriza_email()
    {
        var cliente = Cliente.Cadastrar("Maria", DadosValidos.CpfA, [Telefone(), Email()]);

        cliente.ContatoPreferencial().Canal.ShouldBe(CanalContato.EMAIL);
    }

    [Fact]
    public void Contato_preferencial_usa_sms_quando_nao_ha_email()
    {
        var cliente = Cliente.Cadastrar("Maria", DadosValidos.CpfA, [Telefone()]);

        cliente.ContatoPreferencial().Canal.ShouldBe(CanalContato.SMS);
    }
}

public class VeiculoTests
{
    private static readonly ClienteId Dono = ClienteId.Novo();

    private static Veiculo VeiculoValido() =>
        Veiculo.Cadastrar(Dono, DadosValidos.PlacaAntiga, "Fiat", "Uno", 2015, "Prata");

    [Fact]
    public void Cadastra_veiculo_vinculado_a_um_cliente()
    {
        var veiculo = VeiculoValido();

        veiculo.ClienteId.ShouldBe(Dono);
        veiculo.Placa.Valor.ShouldBe(DadosValidos.PlacaAntiga);
        veiculo.Marca.ShouldBe("Fiat");
        veiculo.AnoFabricacao.ShouldBe(2015);
    }

    [Fact]
    public void Publica_evento_de_veiculo_cadastrado()
    {
        var veiculo = VeiculoValido();

        veiculo.EventosDeDominio.OfType<VeiculoCadastrado>()
            .ShouldHaveSingleItem()
            .Placa.ShouldBe(DadosValidos.PlacaAntiga);
    }

    [Fact]
    public void Nao_cadastra_sem_cliente() =>
        Should.Throw<DomainException>(() =>
                Veiculo.Cadastrar(new ClienteId(Guid.Empty), DadosValidos.PlacaAntiga, "Fiat", "Uno", 2015))
            .Message.ShouldContain("cliente");

    [Theory]
    [InlineData(null, "Uno", "Marca")]
    [InlineData("Fiat", null, "Modelo")]
    public void Nao_cadastra_sem_marca_ou_modelo(string? marca, string? modelo, string trecho) =>
        Should.Throw<DomainException>(() =>
                Veiculo.Cadastrar(Dono, DadosValidos.PlacaAntiga, marca, modelo, 2015))
            .Message.ShouldContain(trecho);

    [Fact]
    public void Nao_aceita_ano_de_fabricacao_no_futuro() =>
        Should.Throw<DomainException>(() =>
                Veiculo.Cadastrar(Dono, DadosValidos.PlacaAntiga, "Fiat", "Uno", DateTime.UtcNow.Year + 1))
            .Message.ShouldContain("Ano de fabricação");

    [Fact]
    public void Nao_aceita_ano_de_fabricacao_anterior_a_1900() =>
        Should.Throw<DomainException>(() =>
            Veiculo.Cadastrar(Dono, DadosValidos.PlacaAntiga, "Fiat", "Uno", 1899));

    [Fact]
    public void Altera_dados_sem_mexer_na_placa()
    {
        var veiculo = VeiculoValido();
        var placaOriginal = veiculo.Placa;
        veiculo.LimparEventos();

        veiculo.AlterarDados("Fiat", "Uno Way", 2016, "Preto");

        veiculo.Modelo.ShouldBe("Uno Way");
        veiculo.Placa.ShouldBe(placaOriginal);
        veiculo.EventosDeDominio.OfType<DadosDoVeiculoAlterados>().ShouldHaveSingleItem();
    }
}
