namespace OficinaDigital.Application.Veiculos;

public interface IVeiculoService
{
    Task<VeiculoDto> CriarAsync(CriarVeiculoRequest request, CancellationToken cancellationToken = default);

    Task<VeiculoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VeiculoDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<VeiculoDto> AtualizarAsync(Guid id, AtualizarVeiculoRequest request, CancellationToken cancellationToken = default);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
