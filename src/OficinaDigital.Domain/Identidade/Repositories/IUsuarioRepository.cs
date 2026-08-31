using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Identidade.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExisteComEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default);
    Task AdicionarAsync(Usuario usuario, CancellationToken ct = default);
}
