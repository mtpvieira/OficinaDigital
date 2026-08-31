using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Estoque.Repositories;

public interface IItemEstoqueRepository
{
    Task<ItemEstoque?> ObterPorIdAsync(ItemEstoqueId id, CancellationToken ct = default);
    Task<ItemEstoque?> ObterPorPecaAsync(PecaId pecaId, CancellationToken ct = default);
    Task<IReadOnlyList<ItemEstoque>> ObterPorPecasAsync(IEnumerable<PecaId> pecaIds, CancellationToken ct = default);

    Task<IReadOnlyList<ItemEstoque>> ObterComReservaDaOsAsync(OrdemDeServicoId ordemDeServicoId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ItemEstoque>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ItemEstoque>> ListarAbaixoDoMinimoAsync(CancellationToken ct = default);

    Task AdicionarAsync(ItemEstoque item, CancellationToken ct = default);
}
