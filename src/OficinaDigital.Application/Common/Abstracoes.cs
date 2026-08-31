using OficinaDigital.Domain.Common;

namespace OficinaDigital.Application.Common;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}

public interface IPoliticaDeDominio<in TEvento> where TEvento : IDomainEvent
{
    Task ExecutarAsync(TEvento evento, CancellationToken ct = default);
}

public interface IDomainEventDispatcher
{
    Task DespacharAsync(IReadOnlyCollection<IDomainEvent> eventos, CancellationToken ct = default);
}

public interface IUsuarioAtual
{
    UsuarioId? Id { get; }
    string? Email { get; }
    string? Perfil { get; }
    ClienteId? ClienteId { get; }
    bool EstaAutenticado { get; }
}
