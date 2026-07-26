namespace OficinaDigital.Domain.Veiculos;

public interface IVeiculoRepository
{
    Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Veiculo>> ListarPorClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken = default);

    void Remover(Veiculo veiculo);
}
