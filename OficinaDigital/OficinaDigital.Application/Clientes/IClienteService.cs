namespace OficinaDigital.Application.Clientes;

public interface IClienteService
{
    Task<ClienteDto> CriarAsync(CriarClienteRequest request, CancellationToken cancellationToken = default);

    Task<ClienteDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<ClienteDto> AtualizarAsync(Guid id, AtualizarClienteRequest request, CancellationToken cancellationToken = default);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
