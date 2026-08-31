using OficinaDigital.Domain.Catalogo.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Catalogo.Repositories;

public interface IServicoRepository
{
    Task<Servico?> ObterPorIdAsync(ServicoId id, CancellationToken ct = default);
    Task<IReadOnlyList<Servico>> ObterPorIdsAsync(IEnumerable<ServicoId> ids, CancellationToken ct = default);
    Task<bool> ExisteComNomeAsync(string nome, ServicoId? exceto = null, CancellationToken ct = default);
    Task<IReadOnlyList<Servico>> ListarAsync(bool? ativo, int pagina, int tamanhoPagina,
        CancellationToken ct = default);
    Task<int> ContarAsync(bool? ativo, CancellationToken ct = default);
    Task AdicionarAsync(Servico servico, CancellationToken ct = default);
}

public interface IPecaRepository
{
    Task<Peca?> ObterPorIdAsync(PecaId id, CancellationToken ct = default);
    Task<IReadOnlyList<Peca>> ObterPorIdsAsync(IEnumerable<PecaId> ids, CancellationToken ct = default);
    Task<bool> ExisteComSkuAsync(Sku sku, CancellationToken ct = default);
    Task<IReadOnlyList<Peca>> ListarAsync(bool? ativa, int pagina, int tamanhoPagina,
        CancellationToken ct = default);
    Task<int> ContarAsync(bool? ativa, CancellationToken ct = default);
    Task AdicionarAsync(Peca peca, CancellationToken ct = default);
}
