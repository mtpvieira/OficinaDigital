namespace OficinaDigital.Application.Catalogo;

public sealed record CriarServicoRequest(string Nome, decimal PrecoBase, string? Descricao, int? TempoEstimadoMinutos);
