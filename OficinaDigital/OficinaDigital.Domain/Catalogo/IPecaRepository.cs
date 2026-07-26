namespace OficinaDigital.Domain.Catalogo;

public interface IPecaRepository
{
    Task<Peca?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Peca>> ListarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(Peca peca, CancellationToken cancellationToken = default);

    void Remover(Peca peca);
}
