using System.ComponentModel.DataAnnotations;
using OficinaDigital.Domain.Atendimento;
using OficinaDigital.Domain.Atendimento.ValueObjects;

namespace OficinaDigital.Application.Atendimento;

public sealed record AbrirOsRequest(
    [Required] Guid ClienteId,
    [Required] Guid VeiculoId,
    [StringLength(1000)] string? DescricaoDoProblema);

public sealed record ItemDeServicoRequest(
    [Required] Guid ServicoId);

public sealed record ItemDePecaRequest(
    [Required] Guid PecaId,
    [Range(0.001, 100_000)] decimal Quantidade);

public sealed record RegistrarServicosRequest(
    [Required, MinLength(1)] IReadOnlyList<ItemDeServicoRequest> Servicos);

public sealed record RegistrarPecasRequest(
    [Required, MinLength(1)] IReadOnlyList<ItemDePecaRequest> Pecas);

public sealed record RegistrarProblemaAdicionalRequest(
    [Required, StringLength(1000, MinimumLength = 3)] string Descricao);

public sealed record RegistrarItensAdicionaisRequest(
    IReadOnlyList<ItemDeServicoRequest>? Servicos,
    IReadOnlyList<ItemDePecaRequest>? Pecas);

public sealed record CancelarOsRequest(
    [Required] MotivoCancelamento Motivo,
    [StringLength(500)] string? Observacao);

public sealed record ItemDeServicoResponse(
    Guid Id,
    Guid ServicoId,
    string Descricao,
    decimal Preco,
    int TempoPadraoMinutos,
    OrigemItem Origem,
    bool AguardandoAprovacaoDoCliente);

public sealed record ItemDePecaResponse(
    Guid Id,
    Guid PecaId,
    string Descricao,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal,
    OrigemItem Origem,
    StatusItemPeca Status,
    bool AguardandoAprovacaoDoCliente);

public sealed record ExecucaoResponse(
    DateTime? Inicio,
    DateTime? Fim,
    int TempoAguardandoAprovacaoEmMinutos,
    int? DuracaoRealEmMinutos);

public sealed record OrdemDeServicoResponse(
    Guid Id,
    long Numero,
    Guid ClienteId,
    Guid VeiculoId,
    StatusOS Status,
    string StatusDescricao,
    MotivoCancelamento? MotivoCancelamento,
    string? ObservacaoCancelamento,
    string? DescricaoDoProblema,
    ExecucaoResponse Execucao,
    Guid? OrcamentoVigenteId,
    bool OrcamentoAprovado,
    decimal Total,
    IReadOnlyList<ItemDeServicoResponse> ItensDeServico,
    IReadOnlyList<ItemDePecaResponse> ItensDePeca,
    DateTime AbertaEm)
{
    public static OrdemDeServicoResponse De(OrdemDeServico os) => new(
        os.Id.Valor,
        os.Numero,
        os.ClienteId.Valor,
        os.VeiculoId.Valor,
        os.Status,
        DescreverStatus(os.Status),
        os.MotivoCancelamento,
        os.ObservacaoCancelamento,
        os.DescricaoDoProblema,
        new ExecucaoResponse(
            os.Execucao.Inicio,
            os.Execucao.Fim,
            (int)os.Execucao.TempoAguardandoAprovacao.TotalMinutes,
            os.Execucao.DuracaoRealEmMinutos),
        os.OrcamentoVigenteId?.Valor,
        os.OrcamentoAprovado,
        os.Total.Valor,
        os.ItensDeServico
            .Select(i => new ItemDeServicoResponse(i.Id, i.ServicoId.Valor, i.Descricao, i.PrecoCongelado.Valor,
                i.TempoPadraoMinutos, i.Origem, i.AguardandoAprovacaoDoCliente))
            .ToList(),
        os.ItensDePeca
            .Select(i => new ItemDePecaResponse(i.Id, i.PecaId.Valor, i.Descricao, i.Quantidade,
                i.PrecoCongelado.Valor, i.Subtotal.Valor, i.Origem, i.Status, i.AguardandoAprovacaoDoCliente))
            .ToList(),
        os.AbertaEm);

    public static string DescreverStatus(StatusOS status) => status switch
    {
        StatusOS.RECEBIDA => "Recebida",
        StatusOS.EM_DIAGNOSTICO => "Em diagnóstico",
        StatusOS.AGUARDANDO_APROVACAO => "Aguardando aprovação",
        StatusOS.EM_EXECUCAO => "Em execução",
        StatusOS.FINALIZADA => "Finalizada",
        StatusOS.ENTREGUE => "Entregue",
        StatusOS.CANCELADA => "Cancelada",
        _ => status.ToString()
    };
}

public sealed record OrdemDeServicoResumoResponse(
    Guid Id,
    long Numero,
    Guid ClienteId,
    Guid VeiculoId,
    StatusOS Status,
    string StatusDescricao,
    decimal Total,
    DateTime AbertaEm)
{
    public static OrdemDeServicoResumoResponse De(OrdemDeServico os) => new(
        os.Id.Valor, os.Numero, os.ClienteId.Valor, os.VeiculoId.Valor, os.Status,
        OrdemDeServicoResponse.DescreverStatus(os.Status), os.Total.Valor, os.AbertaEm);
}
