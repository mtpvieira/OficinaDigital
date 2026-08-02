namespace OficinaDigital.Application.Catalogo;

public sealed record ServicoDto(Guid Id, string Nome, string? Descricao, decimal PrecoBase, int? TempoEstimadoMinutos);
