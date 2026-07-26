namespace OficinaDigital.Domain.OrdensServico;

public interface IOrdemServicoRepository
{
    Task<OrdemDeServico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrdemDeServico?> ObterPorNumeroAsync(string numero, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrdemDeServico>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrdemDeServico>> ListarFinalizadasAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(OrdemDeServico ordemDeServico, CancellationToken cancellationToken = default);
}
