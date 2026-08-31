using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Tests.Common;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Cadastro;

public class PlacaTests
{
    [Theory]
    [InlineData("ABC1234")]
    [InlineData("abc1234")]
    [InlineData("ABC-1234")]
    public void Aceita_placa_no_padrao_antigo(string entrada)
    {
        var placa = Placa.Criar(entrada);

        placa.Valor.ShouldBe(DadosValidos.PlacaAntiga);
        placa.Padrao.ShouldBe(PadraoPlaca.ANTIGO);
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("abc1d23")]
    [InlineData("ABC 1D23")]
    public void Aceita_placa_no_padrao_mercosul(string entrada)
    {
        var placa = Placa.Criar(entrada);

        placa.Valor.ShouldBe(DadosValidos.PlacaMercosul);
        placa.Padrao.ShouldBe(PadraoPlaca.MERCOSUL);
    }

    [Theory]
    [InlineData("AB1234")]     // letras de menos
    [InlineData("ABCD123")]    // letra onde deveria haver dígito
    [InlineData("ABC12345")]   // dígitos demais
    [InlineData("1234ABC")]    // ordem invertida
    [InlineData("ABCD1E23")]   // não é nenhum dos dois padrões
    public void Rejeita_placa_fora_dos_dois_padroes(string entrada) =>
        Should.Throw<DomainException>(() => Placa.Criar(entrada))
            .Message.ShouldContain("AAA0000");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rejeita_placa_vazia(string? entrada) =>
        Should.Throw<DomainException>(() => Placa.Criar(entrada))
            .Message.ShouldContain("obrigatória");

    [Fact]
    public void Compara_por_conteudo() =>
        Placa.Criar("abc-1234").ShouldBe(Placa.Criar("ABC1234"));

    [Theory]
    [InlineData(DadosValidos.PlacaAntiga, true)]
    [InlineData("XX", false)]
    public void EhValida_nao_lanca(string entrada, bool esperado) =>
        Placa.EhValida(entrada).ShouldBe(esperado);
}

public class ContatoTests
{
    [Theory]
    [InlineData("cliente@oficina.com.br")]
    [InlineData("CLIENTE@OFICINA.COM.BR")]
    public void Aceita_email_valido_e_normaliza_para_minusculo(string entrada) =>
        Contato.Criar(CanalContato.EMAIL, entrada).Valor.ShouldBe("cliente@oficina.com.br");

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("dois@@arrobas.com")]
    [InlineData("sem@dominio")]
    public void Rejeita_email_invalido(string entrada) =>
        Should.Throw<DomainException>(() => Contato.Criar(CanalContato.EMAIL, entrada))
            .Message.ShouldContain("inválido");

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("11 3456-7890", "1134567890")]
    public void Aceita_telefone_com_ddd_e_guarda_so_digitos(string entrada, string esperado) =>
        Contato.Criar(CanalContato.SMS, entrada).Valor.ShouldBe(esperado);

    [Theory]
    [InlineData("987654321")]        // 9 dígitos, sem DDD
    [InlineData("119876543210")]     // 12 dígitos
    public void Rejeita_telefone_fora_do_tamanho(string entrada) =>
        Should.Throw<DomainException>(() => Contato.Criar(CanalContato.SMS, entrada))
            .Message.ShouldContain("DDD");

    [Fact]
    public void Compara_por_canal_e_valor()
    {
        var a = Contato.Criar(CanalContato.EMAIL, "a@b.com");
        var b = Contato.Criar(CanalContato.EMAIL, "A@B.com");

        a.ShouldBe(b);
    }

    [Fact]
    public void Mesmo_valor_em_canais_diferentes_nao_sao_iguais() =>
        Contato.Criar(CanalContato.EMAIL, "a@b.com")
            .ShouldNotBe(Contato.Criar(CanalContato.SMS, "11987654321"));
}

public class EnderecoTests
{
    [Fact]
    public void Cria_endereco_valido_e_normaliza_uf_e_cep()
    {
        var endereco = Endereco.Criar("Rua das Oficinas", "100", "Sala 2", "São Paulo", "sp", "01310-100");

        endereco.Uf.ShouldBe("SP");
        endereco.Cep.ShouldBe("01310100");
    }

    [Theory]
    [InlineData(null, "100", "São Paulo", "SP", "01310100", "Logradouro")]
    [InlineData("Rua X", null, "São Paulo", "SP", "01310100", "Número")]
    [InlineData("Rua X", "100", null, "SP", "01310100", "Cidade")]
    [InlineData("Rua X", "100", "São Paulo", "SAO", "01310100", "UF")]
    [InlineData("Rua X", "100", "São Paulo", "SP", "123", "CEP")]
    public void Rejeita_endereco_incompleto(string? logradouro, string? numero, string? cidade,
        string? uf, string? cep, string trechoEsperado) =>
        Should.Throw<DomainException>(() => Endereco.Criar(logradouro, numero, null, cidade, uf, cep))
            .Message.ShouldContain(trechoEsperado);
}
