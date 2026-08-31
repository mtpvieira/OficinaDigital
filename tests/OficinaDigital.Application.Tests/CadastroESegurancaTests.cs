using OficinaDigital.Application.Cadastro;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Seguranca;
using OficinaDigital.Application.Tests.Infra;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Identidade;
using OficinaDigital.Infrastructure.Seguranca;
using Shouldly;

namespace OficinaDigital.Application.Tests;

public class CadastroTests : IDisposable
{
    private readonly AmbienteDeTeste _ambiente = new();
    private readonly Cenario _cenario;

    public CadastroTests() => _cenario = new Cenario(_ambiente);

    public void Dispose() => _ambiente.Dispose();

    [Fact]
    public async Task Cadastra_e_identifica_cliente_por_documento()
    {
        var cadastrado = await _cenario.CadastrarClienteAsync();

        var encontrado = await _cenario.Clientes.IdentificarPorDocumentoAsync("529.982.247-25");

        encontrado.ShouldNotBeNull();
        encontrado.Id.ShouldBe(cadastrado.Id);
        encontrado.DocumentoFormatado.ShouldBe("529.982.247-25");
    }

    [Fact]
    public async Task Cliente_nao_encontrado_devolve_nulo_e_leva_ao_cadastro()
    {
        await _cenario.CadastrarClienteAsync();

        (await _cenario.Clientes.IdentificarPorDocumentoAsync(Cenario.CpfOutroCliente)).ShouldBeNull();
    }

    [Fact]
    public async Task Documento_duplicado_e_rejeitado()
    {
        await _cenario.CadastrarClienteAsync();

        var erro = await Should.ThrowAsync<ConflitoDeNegocioException>(() => _cenario.CadastrarClienteAsync());
        erro.Message.ShouldContain("Já existe cliente");
    }

    [Fact]
    public async Task Documento_invalido_e_rejeitado_na_borda_do_dominio() =>
        await Should.ThrowAsync<DomainException>(() =>
            _cenario.CadastrarClienteAsync("52998224724"));

    [Fact]
    public async Task Placa_duplicada_e_rejeitada()
    {
        var cliente = await _cenario.CadastrarClienteAsync();
        await _cenario.CadastrarVeiculoAsync(cliente.Id);

        var erro = await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _cenario.CadastrarVeiculoAsync(cliente.Id, "abc-1234"));

        erro.Message.ShouldContain("Já existe veículo");
    }

    [Fact]
    public async Task Aceita_placa_mercosul()
    {
        var cliente = await _cenario.CadastrarClienteAsync();

        var veiculo = await _cenario.CadastrarVeiculoAsync(cliente.Id, "BRA2E19");

        veiculo.PadraoPlaca.ShouldBe(PadraoPlaca.MERCOSUL);
    }

    [Fact]
    public async Task Cliente_com_os_ativa_nao_pode_ser_inativado()
    {
        var (cliente, _, _) = await _cenario.AbrirOsAsync();

        var erro = await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _cenario.Clientes.InativarAsync(cliente.Id));

        erro.Message.ShouldContain("ordem de serviço ativa");
    }

    [Fact]
    public async Task Cliente_sem_os_ativa_e_inativado_sem_perder_historico()
    {
        var cliente = await _cenario.CadastrarClienteAsync();

        await _cenario.Clientes.InativarAsync(cliente.Id);

        (await _cenario.Clientes.ObterPorIdAsync(cliente.Id)).Ativo.ShouldBeFalse();
    }

    [Fact]
    public async Task Nao_abre_os_para_cliente_inativo()
    {
        var cliente = await _cenario.CadastrarClienteAsync();
        var veiculo = await _cenario.CadastrarVeiculoAsync(cliente.Id);
        await _cenario.Clientes.InativarAsync(cliente.Id);

        await Should.ThrowAsync<ConflitoDeNegocioException>(() =>
            _cenario.Ordens.AbrirAsync(new Application.Atendimento.AbrirOsRequest(cliente.Id, veiculo.Id, null)));
    }

    [Fact]
    public async Task Servico_com_nome_duplicado_e_rejeitado()
    {
        await _cenario.CadastrarServicoAsync();

        await Should.ThrowAsync<ConflitoDeNegocioException>(() => _cenario.CadastrarServicoAsync());
    }

    [Fact]
    public async Task Listagem_de_clientes_pagina_e_filtra()
    {
        await _cenario.CadastrarClienteAsync(Cenario.CpfCliente, "Maria Silva");
        await _cenario.CadastrarClienteAsync(Cenario.CpfOutroCliente, "João Souza");

        var todos = await _cenario.Clientes.ListarAsync(null, null, new Paginacao());
        var filtrados = await _cenario.Clientes.ListarAsync("Maria", null, new Paginacao());

        todos.TotalDeItens.ShouldBe(2);
        filtrados.TotalDeItens.ShouldBe(1);
    }

    [Fact]
    public async Task Paginacao_normaliza_valores_absurdos()
    {
        await _cenario.CadastrarClienteAsync();

        var resultado = await _cenario.Clientes.ListarAsync(null, null,
            new Paginacao { Pagina = 0, TamanhoPagina = 5000 });

        resultado.Pagina.ShouldBe(1);
        resultado.TamanhoPagina.ShouldBe(100);
    }
}

public class SegurancaTests : IDisposable
{
    private readonly AmbienteDeTeste _ambiente = new();

    public void Dispose() => _ambiente.Dispose();

    private AuthService Auth => _ambiente.Servico<AuthService>();

    [Fact]
    public void Hash_de_senha_gera_salt_diferente_a_cada_usuario()
    {
        var hasher = _ambiente.Servico<IPasswordHasher>();

        var (hashA, saltA) = hasher.Gerar("SenhaForte@123");
        var (hashB, saltB) = hasher.Gerar("SenhaForte@123");

        saltA.ShouldNotBe(saltB);
        hashA.ShouldNotBe(hashB);
        hasher.Verificar("SenhaForte@123", hashA, saltA).ShouldBeTrue();
        hasher.Verificar("SenhaForte@123", hashB, saltB).ShouldBeTrue();
    }

    [Fact]
    public void Senha_errada_nao_passa_na_verificacao()
    {
        var hasher = _ambiente.Servico<IPasswordHasher>();
        var (hash, salt) = hasher.Gerar("SenhaForte@123");

        hasher.Verificar("SenhaErrada", hash, salt).ShouldBeFalse();
        hasher.Verificar("", hash, salt).ShouldBeFalse();
        hasher.Verificar("SenhaForte@123", "nao-e-base64!", salt).ShouldBeFalse();
    }

    [Fact]
    public async Task Login_com_credenciais_validas_emite_token_com_o_perfil()
    {
        await Auth.CriarUsuarioAsync(new CriarUsuarioRequest(
            "Ana", "ana@oficina.com", "SenhaForte@123", PerfilUsuario.MECANICO));

        var token = await Auth.LoginAsync(new LoginRequest("ana@oficina.com", "SenhaForte@123"));

        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
        token.TokenType.ShouldBe("Bearer");
        token.Perfil.ShouldBe("MECANICO");
    }

    [Fact]
    public async Task Login_com_senha_errada_e_negado()
    {
        await Auth.CriarUsuarioAsync(new CriarUsuarioRequest(
            "Ana", "ana@oficina.com", "SenhaForte@123", PerfilUsuario.ATENDENTE));

        await Should.ThrowAsync<CredencialInvalidaException>(() =>
            Auth.LoginAsync(new LoginRequest("ana@oficina.com", "OutraSenha@1")));
    }

    [Fact]
    public async Task Usuario_inexistente_devolve_a_mesma_mensagem_de_credencial_invalida()
    {
        var erro = await Should.ThrowAsync<CredencialInvalidaException>(() =>
            Auth.LoginAsync(new LoginRequest("ninguem@oficina.com", "SenhaForte@123")));

        erro.Message.ShouldBe("Credenciais inválidas.");
    }

    [Fact]
    public async Task Email_duplicado_e_rejeitado()
    {
        await Auth.CriarUsuarioAsync(new CriarUsuarioRequest(
            "Ana", "ana@oficina.com", "SenhaForte@123", PerfilUsuario.ATENDENTE));

        await Should.ThrowAsync<ConflitoDeNegocioException>(() => Auth.CriarUsuarioAsync(
            new CriarUsuarioRequest("Outra Ana", "ANA@oficina.com", "SenhaForte@456",
                PerfilUsuario.MECANICO)));
    }

    [Fact]
    public async Task Cliente_autentica_com_documento_e_placa_do_proprio_veiculo()
    {
        var cenario = new Cenario(_ambiente);
        var cliente = await cenario.CadastrarClienteAsync();
        await cenario.CadastrarVeiculoAsync(cliente.Id);

        var token = await Auth.LoginClienteAsync(
            new LoginClienteRequest(Cenario.CpfCliente, Cenario.PlacaVeiculo));

        token.Perfil.ShouldBe(PerfilAcesso.Cliente);
    }

    [Fact]
    public async Task Cliente_nao_autentica_com_placa_de_veiculo_de_outro()
    {
        var cenario = new Cenario(_ambiente);
        var clienteA = await cenario.CadastrarClienteAsync();
        await cenario.CadastrarVeiculoAsync(clienteA.Id);
        await cenario.CadastrarClienteAsync(Cenario.CpfOutroCliente, "João");

        await Should.ThrowAsync<CredencialInvalidaException>(() => Auth.LoginClienteAsync(
            new LoginClienteRequest(Cenario.CpfOutroCliente, Cenario.PlacaVeiculo)));
    }
}
