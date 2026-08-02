namespace OficinaDigital.Application.Catalogo;

public interface IPecaService
{
    Task<PecaDto> CriarAsync(CriarPecaRequest request, CancellationToken cancellationToken = default);

    Task<PecaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PecaDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<PecaDto> AtualizarAsync(Guid id, AtualizarPecaRequest request, CancellationToken cancellationToken = default);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PecaDto> ReporEstoqueAsync(Guid id, ReporEstoqueRequest request, CancellationToken cancellationToken = default);
}
