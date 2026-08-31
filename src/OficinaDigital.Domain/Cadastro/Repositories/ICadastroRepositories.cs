using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(ClienteId id, CancellationToken ct = default);
    Task<Cliente?> ObterPorDocumentoAsync(CpfCnpj documento, CancellationToken ct = default);
    Task<bool> ExisteComDocumentoAsync(CpfCnpj documento, CancellationToken ct = default);
    Task<IReadOnlyList<Cliente>> ListarAsync(string? filtroNome, bool? ativo, int pagina, int tamanhoPagina,
        CancellationToken ct = default);
    Task<int> ContarAsync(string? filtroNome, bool? ativo, CancellationToken ct = default);
    Task AdicionarAsync(Cliente cliente, CancellationToken ct = default);
    void Remover(Cliente cliente);
}

public interface IVeiculoRepository
{
    Task<Veiculo?> ObterPorIdAsync(VeiculoId id, CancellationToken ct = default);
    Task<Veiculo?> ObterPorPlacaAsync(Placa placa, CancellationToken ct = default);
    Task<bool> ExisteComPlacaAsync(Placa placa, CancellationToken ct = default);
    Task<IReadOnlyList<Veiculo>> ListarPorClienteAsync(ClienteId clienteId, CancellationToken ct = default);
    Task<IReadOnlyList<Veiculo>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(CancellationToken ct = default);
    Task AdicionarAsync(Veiculo veiculo, CancellationToken ct = default);
    void Remover(Veiculo veiculo);
}
