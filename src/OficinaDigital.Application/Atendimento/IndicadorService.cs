using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Atendimento.Repositories;

namespace OficinaDigital.Application.Atendimento;

public sealed record TempoMedioPorServicoResponse(
    Guid ServicoId,
    string Servico,
    int QuantidadeDeOs,
    double TempoMedioRealEmMinutos,
    int TempoPadraoEmMinutos,
    double DesvioEmMinutos,
    double DesvioPercentual);

public sealed record PainelDeTempoMedioResponse(
    DateTime? PeriodoInicio,
    DateTime? PeriodoFim,
    int TotalDeOsConsideradas,
    double TempoMedioGeralEmMinutos,
    IReadOnlyList<TempoMedioPorServicoResponse> PorServico);

public class IndicadorService(IOrdemDeServicoRepository ordens)
{
    public async Task<PainelDeTempoMedioResponse> ObterTempoMedioDeExecucaoAsync(
        DateTime? inicio, DateTime? fim, CancellationToken ct = default)
    {
        if (inicio is not null && fim is not null && inicio > fim)
            throw new ConflitoDeNegocioException("A data inicial do período não pode ser maior que a final.");

        var linhas = await ordens.ObterTempoMedioPorServicoAsync(inicio, fim, ct);

        var porServico = linhas
            .OrderByDescending(l => l.DesvioEmMinutos)
            .Select(l => new TempoMedioPorServicoResponse(
                l.ServicoId.Valor,
                l.NomeDoServico,
                l.QuantidadeDeOs,
                Math.Round(l.MediaMinutosReal, 2),
                l.TempoPadraoMinutos,
                Math.Round(l.DesvioEmMinutos, 2),
                l.DesvioPercentual))
            .ToList();

        var totalOs = porServico.Sum(p => p.QuantidadeDeOs);

        var mediaGeral = totalOs == 0
            ? 0
            : Math.Round(
                porServico.Sum(p => p.TempoMedioRealEmMinutos * p.QuantidadeDeOs) / totalOs, 2);

        return new PainelDeTempoMedioResponse(inicio, fim, totalOs, mediaGeral, porServico);
    }
}
