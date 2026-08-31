using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Catalogo.Events;

public sealed record ServicoCadastradoNoCatalogo(ServicoId ServicoId, string Nome) : DomainEvent;

public sealed record PrecoDoServicoAlterado(ServicoId ServicoId, decimal NovoValor, DateOnly VigenteDe) : DomainEvent;

public sealed record ServicoInativado(ServicoId ServicoId) : DomainEvent;

public sealed record PecaCadastrada(PecaId PecaId, string Sku, UnidadeDeMedida Unidade, decimal EstoqueMinimo)
    : DomainEvent;

public sealed record PrecoDaPecaAlterado(PecaId PecaId, decimal NovoValor, DateOnly VigenteDe) : DomainEvent;

public sealed record PecaInativada(PecaId PecaId) : DomainEvent;
