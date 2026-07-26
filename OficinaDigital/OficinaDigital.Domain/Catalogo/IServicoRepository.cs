namespace OficinaDigital.Domain.Catalogo;

public interface IServicoRepository
{
    Task<Servico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Servico>> ListarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(Servico servico, CancellationToken cancellationToken = default);

    void Remover(Servico servico);
}
