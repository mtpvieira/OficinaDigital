using OficinaDigital.Application.Atendimento;
using OficinaDigital.Application.Cadastro;
using OficinaDigital.Application.Catalogo;
using OficinaDigital.Application.Estoque;
using OficinaDigital.Application.Orcamentos;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Application.Tests.Infra;

public sealed class Cenario(AmbienteDeTeste ambiente)
{
    public const string CpfCliente = "52998224725";
    public const string CpfOutroCliente = "11144477735";
    public const string PlacaVeiculo = "ABC1234";

    public ClienteService Clientes => ambiente.Servico<ClienteService>();
    public VeiculoService Veiculos => ambiente.Servico<VeiculoService>();
    public ServicoService Servicos => ambiente.Servico<ServicoService>();
    public PecaService Pecas => ambiente.Servico<PecaService>();
    public EstoqueService Estoque => ambiente.Servico<EstoqueService>();
    public OrdemDeServicoService Ordens => ambiente.Servico<OrdemDeServicoService>();
    public OrcamentoService Orcamentos => ambiente.Servico<OrcamentoService>();

    public Task<ClienteResponse> CadastrarClienteAsync(string documento = CpfCliente, string nome = "Maria Silva") =>
        Clientes.CadastrarAsync(new CadastrarClienteRequest(
            nome, documento, [new ContatoDto(CanalContato.EMAIL, "maria@exemplo.com")], null));

    public Task<VeiculoResponse> CadastrarVeiculoAsync(Guid clienteId, string placa = PlacaVeiculo) =>
        Veiculos.CadastrarAsync(new CadastrarVeiculoRequest(clienteId, placa, "Fiat", "Uno", 2015, "Prata"));

    public Task<ServicoResponse> CadastrarServicoAsync(string nome = "Troca de óleo", decimal preco = 180m,
        int minutos = 45) =>
        Servicos.CadastrarAsync(new CadastrarServicoRequest(nome, null, preco, minutos));

    public async Task<PecaResponse> CadastrarPecaComEstoqueAsync(string sku = "OLEO-5W30", decimal saldo = 20m,
        decimal preco = 58m, decimal estoqueMinimo = 5m)
    {
        var peca = await Pecas.CadastrarAsync(new CadastrarPecaRequest(
            sku, $"Peça {sku}", UnidadeDeMedida.L, preco, estoqueMinimo));

        if (saldo > 0)
            await Estoque.RegistrarEntradaAsync(peca.Id, new RegistrarEntradaRequest(saldo, preco * 0.6m));

        return peca;
    }

    public async Task<(ClienteResponse Cliente, VeiculoResponse Veiculo, OrdemDeServicoResponse Os)>
        AbrirOsAsync()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync(cliente.Id);
        var os = await Ordens.AbrirAsync(new AbrirOsRequest(cliente.Id, veiculo.Id, "Barulho no motor"));

        return (cliente, veiculo, os);
    }

    public async Task<(OrdemDeServicoResponse Os, OrcamentoResponse Orcamento)> ComOrcamentoGeradoAsync(
        decimal quantidadePeca = 4m, decimal saldoDaPeca = 20m)
    {
        var (_, _, os) = await AbrirOsAsync();
        var servico = await CadastrarServicoAsync();
        var peca = await CadastrarPecaComEstoqueAsync(saldo: saldoDaPeca);

        await Ordens.IniciarDiagnosticoAsync(os.Id);
        await Ordens.RegistrarServicosAsync(os.Id, new RegistrarServicosRequest([new ItemDeServicoRequest(servico.Id)]));
        await Ordens.RegistrarPecasAsync(os.Id,
            new RegistrarPecasRequest([new ItemDePecaRequest(peca.Id, quantidadePeca)]));
        await Ordens.FinalizarDiagnosticoAsync(os.Id);

        var orcamentos = await Orcamentos.ListarPorOsAsync(os.Id);

        return (await Ordens.ObterPorIdAsync(os.Id), orcamentos.Single());
    }

    public async Task<OrdemDeServicoResponse> ComOrcamentoAprovadoAsync()
    {
        var (os, orcamento) = await ComOrcamentoGeradoAsync();

        await Orcamentos.EnviarAsync(orcamento.Id);
        await Orcamentos.ResponderAsync(orcamento.Id,
            new ResponderOrcamentoRequest(Domain.Orcamentos.ValueObjects.DecisaoCliente.APROVADO), null);

        return await Ordens.ObterPorIdAsync(os.Id);
    }

    public async Task<OrdemDeServicoResponse> EmExecucaoAsync()
    {
        var os = await ComOrcamentoAprovadoAsync();
        return await Ordens.IniciarServicoAsync(os.Id);
    }
}
