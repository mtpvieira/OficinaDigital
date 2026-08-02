namespace OficinaDigital.Application.Catalogo;

public interface IServicoService
{
    Task<ServicoDto> CriarAsync(CriarServicoRequest request, CancellationToken cancellationToken = default);

    Task<ServicoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServicoDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<ServicoDto> AtualizarAsync(Guid id, AtualizarServicoRequest request, CancellationToken cancellationToken = default);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
