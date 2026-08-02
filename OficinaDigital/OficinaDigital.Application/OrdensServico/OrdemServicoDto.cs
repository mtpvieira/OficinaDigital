namespace OficinaDigital.Application.OrdensServico;

public sealed record ItemServicoDto(Guid ServicoId, string Descricao, decimal Preco);

public sealed record ItemPecaDto(Guid PecaId, string Descricao, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

public sealed record OrdemServicoDto(
    Guid Id,
    string Numero,
    Guid ClienteId,
    Guid VeiculoId,
    string Status,
    decimal Orcamento,
    IReadOnlyList<ItemServicoDto> ItensServico,
    IReadOnlyList<ItemPecaDto> ItensPeca,
    DateTime CriadaEm,
    DateTime? EnviadaAprovacaoEm,
    DateTime? AprovadaEm,
    DateTime? ExecucaoIniciadaEm,
    DateTime? FinalizadaEm,
    DateTime? EntregueEm);
