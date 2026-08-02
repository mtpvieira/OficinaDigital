namespace OficinaDigital.Application.OrdensServico;

public sealed record ItemServicoRequest(Guid ServicoId);

public sealed record ItemPecaRequest(Guid PecaId, int Quantidade);

public sealed record CriarOrdemServicoRequest(
    Guid ClienteId,
    Guid VeiculoId,
    IReadOnlyList<ItemServicoRequest> Servicos,
    IReadOnlyList<ItemPecaRequest> Pecas);
