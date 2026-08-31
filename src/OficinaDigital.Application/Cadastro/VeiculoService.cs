using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Cadastro;
using OficinaDigital.Domain.Cadastro.Repositories;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Application.Cadastro;

public class VeiculoService(
    IVeiculoRepository veiculos,
    IClienteRepository clientes,
    IUnitOfWork uow)
{
    public async Task<VeiculoResponse?> IdentificarPorPlacaAsync(string placa, CancellationToken ct = default)
    {
        var veiculo = await veiculos.ObterPorPlacaAsync(Placa.Criar(placa), ct);
        return veiculo is null ? null : VeiculoResponse.De(veiculo);
    }

    public async Task<VeiculoResponse> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(id), ct)
                      ?? throw new RecursoNaoEncontradoException("Veículo", id);

        return VeiculoResponse.De(veiculo);
    }

    public async Task<IReadOnlyList<VeiculoResponse>> ListarPorClienteAsync(Guid clienteId,
        CancellationToken ct = default)
    {
        var lista = await veiculos.ListarPorClienteAsync(new ClienteId(clienteId), ct);
        return lista.Select(VeiculoResponse.De).ToList();
    }

    public async Task<ResultadoPaginado<VeiculoResponse>> ListarAsync(Paginacao paginacao,
        CancellationToken ct = default)
    {
        var p = paginacao.Normalizar();

        var itens = await veiculos.ListarAsync(p.Pagina, p.TamanhoPagina, ct);
        var total = await veiculos.ContarAsync(ct);

        return new ResultadoPaginado<VeiculoResponse>(
            itens.Select(VeiculoResponse.De).ToList(), p.Pagina, p.TamanhoPagina, total);
    }

    public async Task<VeiculoResponse> CadastrarAsync(CadastrarVeiculoRequest request, CancellationToken ct = default)
    {
        var clienteId = new ClienteId(request.ClienteId);

        var cliente = await clientes.ObterPorIdAsync(clienteId, ct)
                      ?? throw new RecursoNaoEncontradoException("Cliente", request.ClienteId);

        if (!cliente.Ativo)
            throw new ConflitoDeNegocioException("Não é possível cadastrar veículo para um cliente inativo.");

        var placa = Placa.Criar(request.Placa);

        if (await veiculos.ExisteComPlacaAsync(placa, ct))
            throw new ConflitoDeNegocioException($"Já existe veículo cadastrado com a placa {placa.Valor}.");

        var veiculo = Veiculo.Cadastrar(clienteId, request.Placa, request.Marca, request.Modelo,
            request.AnoFabricacao, request.Cor);

        await veiculos.AdicionarAsync(veiculo, ct);
        await uow.CommitAsync(ct);

        return VeiculoResponse.De(veiculo);
    }

    public async Task<VeiculoResponse> AlterarAsync(Guid id, AlterarVeiculoRequest request,
        CancellationToken ct = default)
    {
        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(id), ct)
                      ?? throw new RecursoNaoEncontradoException("Veículo", id);

        veiculo.AlterarDados(request.Marca, request.Modelo, request.AnoFabricacao, request.Cor);

        await uow.CommitAsync(ct);
        return VeiculoResponse.De(veiculo);
    }
}
