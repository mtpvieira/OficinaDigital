namespace OficinaDigital.Application.Catalogo;

public sealed record AtualizarServicoRequest(string Nome, decimal PrecoBase, string? Descricao, int? TempoEstimadoMinutos);
