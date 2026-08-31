using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Estoque.Events;

public sealed record EntradaDePecasRegistrada(ItemEstoqueId ItemEstoqueId, PecaId PecaId, decimal Quantidade)
    : DomainEvent;

public sealed record PecaReservada(
    ItemEstoqueId ItemEstoqueId,
    PecaId PecaId,
    OrdemDeServicoId OrdemDeServicoId,
    decimal Quantidade) : DomainEvent;

public sealed record ReservaDePecaLiberada(
    ItemEstoqueId ItemEstoqueId,
    PecaId PecaId,
    OrdemDeServicoId OrdemDeServicoId,
    decimal Quantidade) : DomainEvent;

public sealed record PecaSubtraidaDoEstoque(
    ItemEstoqueId ItemEstoqueId,
    PecaId PecaId,
    OrdemDeServicoId OrdemDeServicoId,
    decimal Quantidade) : DomainEvent;

public sealed record EstoqueMinimoDefinido(ItemEstoqueId ItemEstoqueId, PecaId PecaId, decimal EstoqueMinimo)
    : DomainEvent;

public sealed record AlertaDeEstoqueMinimoEmitido(
    ItemEstoqueId ItemEstoqueId,
    PecaId PecaId,
    decimal Disponivel,
    decimal EstoqueMinimo) : DomainEvent;

public sealed record FaltaDePecaIdentificada(
    PecaId PecaId,
    OrdemDeServicoId OrdemDeServicoId,
    decimal QuantidadeSolicitada,
    decimal QuantidadeDisponivel) : DomainEvent;

public sealed record CompraDePecaSinalizada(PecaId PecaId, decimal QuantidadeFaltante) : DomainEvent;
