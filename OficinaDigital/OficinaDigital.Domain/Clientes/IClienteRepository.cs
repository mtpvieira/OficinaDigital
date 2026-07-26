namespace OficinaDigital.Domain.Clientes;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Cliente?> ObterPorDocumentoAsync(string documento, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default);

    void Remover(Cliente cliente);
}
