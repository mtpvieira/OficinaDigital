using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Tests.Common;
using Shouldly;

namespace OficinaDigital.Domain.Tests.Cadastro;

public class CpfCnpjTests
{
    [Theory]
    [InlineData(DadosValidos.CpfA)]
    [InlineData(DadosValidos.CpfB)]
    [InlineData(DadosValidos.CpfC)]
    public void Aceita_cpf_com_digito_verificador_valido(string entrada)
    {
        var documento = CpfCnpj.Criar(entrada);

        documento.Numero.ShouldBe(entrada);
        documento.Tipo.ShouldBe(TipoPessoa.PF);
    }

    [Theory]
    [InlineData(DadosValidos.CnpjA)]
    [InlineData(DadosValidos.CnpjB)]
    public void Aceita_cnpj_com_digito_verificador_valido(string entrada)
    {
        var documento = CpfCnpj.Criar(entrada);

        documento.Numero.ShouldBe(entrada);
        documento.Tipo.ShouldBe(TipoPessoa.PJ);
    }

    [Theory]
    [InlineData("529.982.247-25", DadosValidos.CpfA)]
    [InlineData("11.222.333/0001-81", DadosValidos.CnpjA)]
    [InlineData("529 982 247 25", DadosValidos.CpfA)]
    public void Ignora_pontuacao_e_espacos(string entrada, string esperado) =>
        CpfCnpj.Criar(entrada).Numero.ShouldBe(esperado);

    [Theory]
    [InlineData("52998224724")]      // último dígito trocado
    [InlineData("11222333000180")]   // último dígito trocado
    public void Rejeita_digito_verificador_invalido(string entrada) =>
        Should.Throw<DomainException>(() => CpfCnpj.Criar(entrada))
            .Message.ShouldContain("dígito verificador");

    [Theory]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("11111111111111")]
    public void Rejeita_documento_com_todos_os_digitos_iguais(string entrada) =>
        Should.Throw<DomainException>(() => CpfCnpj.Criar(entrada));

    [Theory]
    [InlineData("123")]
    [InlineData("5299822472")]        // 10 dígitos
    [InlineData("112223330001811")]   // 15 dígitos
    public void Rejeita_quantidade_de_digitos_fora_do_padrao(string entrada) =>
        Should.Throw<DomainException>(() => CpfCnpj.Criar(entrada))
            .Message.ShouldContain("11 (CPF) ou 14 (CNPJ)");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejeita_documento_vazio(string? entrada) =>
        Should.Throw<DomainException>(() => CpfCnpj.Criar(entrada))
            .Message.ShouldContain("obrigatório");

    [Fact]
    public void Formata_cpf_para_exibicao() =>
        CpfCnpj.Criar(DadosValidos.CpfA).Formatado().ShouldBe(DadosValidos.CpfFormatado);

    [Fact]
    public void Formata_cnpj_para_exibicao() =>
        CpfCnpj.Criar(DadosValidos.CnpjA).Formatado().ShouldBe(DadosValidos.CnpjFormatado);

    [Fact]
    public void Compara_por_conteudo_e_nao_por_referencia()
    {
        var a = CpfCnpj.Criar(DadosValidos.CpfA);
        var b = CpfCnpj.Criar(DadosValidos.CpfFormatado);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Documentos_diferentes_nao_sao_iguais() =>
        CpfCnpj.Criar(DadosValidos.CpfA).ShouldNotBe(CpfCnpj.Criar(DadosValidos.CpfB));

    [Theory]
    [InlineData(DadosValidos.CpfA, true)]
    [InlineData("52998224724", false)]
    [InlineData(null, false)]
    public void EhValido_nao_lanca_e_responde_true_ou_false(string? entrada, bool esperado) =>
        CpfCnpj.EhValido(entrada).ShouldBe(esperado);
}
