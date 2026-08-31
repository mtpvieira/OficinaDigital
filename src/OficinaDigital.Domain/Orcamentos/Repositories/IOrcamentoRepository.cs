using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Domain.Orcamentos.Repositories;

public interface IOrcamentoRepository
{
    Task<Orcamento?> ObterPorIdAsync(OrcamentoId id, CancellationToken ct = default);

    Task<IReadOnlyList<Orcamento>> ListarPorOsAsync(OrdemDeServicoId ordemDeServicoId,
        CancellationToken ct = default);

    Task<Orcamento?> ObterPrincipalDaOsAsync(OrdemDeServicoId ordemDeServicoId, CancellationToken ct = default);

    Task AdicionarAsync(Orcamento orcamento, CancellationToken ct = default);
}
