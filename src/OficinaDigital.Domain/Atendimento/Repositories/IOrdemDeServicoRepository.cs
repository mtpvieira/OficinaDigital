using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Atendimento.Repositories;

public sealed record TempoMedioPorServico(
    ServicoId ServicoId,
    string NomeDoServico,
    int QuantidadeDeOs,
    double MediaMinutosReal,
    int TempoPadraoMinutos)
{
    public double DesvioEmMinutos => MediaMinutosReal - TempoPadraoMinutos;

    public double DesvioPercentual =>
        TempoPadraoMinutos == 0 ? 0 : Math.Round((MediaMinutosReal - TempoPadraoMinutos) / TempoPadraoMinutos * 100, 2);
}

public interface IOrdemDeServicoRepository
{
    Task<OrdemDeServico?> ObterPorIdAsync(OrdemDeServicoId id, CancellationToken ct = default);
    Task<OrdemDeServico?> ObterPorNumeroAsync(long numero, CancellationToken ct = default);

    Task<OrdemDeServico?> ObterAtivaPorVeiculoAsync(VeiculoId veiculoId, CancellationToken ct = default);

    Task<bool> ClientePossuiOsAtivaAsync(ClienteId clienteId, CancellationToken ct = default);

    Task<IReadOnlyList<OrdemDeServico>> ListarAsync(StatusOS? status, ClienteId? clienteId, VeiculoId? veiculoId,
        int pagina, int tamanhoPagina, CancellationToken ct = default);

    Task<int> ContarAsync(StatusOS? status, ClienteId? clienteId, VeiculoId? veiculoId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<StatusOS, int>> ContarPorStatusAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TempoMedioPorServico>> ObterTempoMedioPorServicoAsync(
        DateTime? inicio, DateTime? fim, CancellationToken ct = default);

    Task<IReadOnlyList<OrdemDeServico>> ListarComPecaPendenteAsync(PecaId pecaId, CancellationToken ct = default);

    Task AdicionarAsync(OrdemDeServico ordem, CancellationToken ct = default);
}
