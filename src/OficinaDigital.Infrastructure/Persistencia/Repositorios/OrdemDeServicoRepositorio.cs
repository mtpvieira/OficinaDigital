using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OficinaDigital.Domain.Atendimento;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Infrastructure.Persistencia.Repositorios;

public class OrdemDeServicoRepository(OficinaDbContext ctx) : IOrdemDeServicoRepository
{
    private IQueryable<OrdemDeServico> Completa => ctx.OrdensDeServico
        .Include(o => o.ItensDeServico)
        .Include(o => o.ItensDePeca);

    public Task<OrdemDeServico?> ObterPorIdAsync(OrdemDeServicoId id, CancellationToken ct = default) =>
        Completa.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<OrdemDeServico?> ObterPorNumeroAsync(long numero, CancellationToken ct = default) =>
        Completa.FirstOrDefaultAsync(o => o.Numero == numero, ct);

    public Task<OrdemDeServico?> ObterAtivaPorVeiculoAsync(VeiculoId veiculoId, CancellationToken ct = default) =>
        Completa.FirstOrDefaultAsync(
            o => o.VeiculoId == veiculoId && o.Status != StatusOS.ENTREGUE && o.Status != StatusOS.CANCELADA, ct);

    public Task<bool> ClientePossuiOsAtivaAsync(ClienteId clienteId, CancellationToken ct = default) =>
        ctx.OrdensDeServico.AnyAsync(
            o => o.ClienteId == clienteId && o.Status != StatusOS.ENTREGUE && o.Status != StatusOS.CANCELADA, ct);

    public async Task<IReadOnlyList<OrdemDeServico>> ListarAsync(StatusOS? status, ClienteId? clienteId,
        VeiculoId? veiculoId, int pagina, int tamanhoPagina, CancellationToken ct = default) =>
        await Filtrar(status, clienteId, veiculoId)
            .OrderByDescending(o => o.Numero)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

    public Task<int> ContarAsync(StatusOS? status, ClienteId? clienteId, VeiculoId? veiculoId,
        CancellationToken ct = default) =>
        Filtrar(status, clienteId, veiculoId).CountAsync(ct);

    public async Task<IReadOnlyDictionary<StatusOS, int>> ContarPorStatusAsync(CancellationToken ct = default)
    {
        var contagem = await ctx.OrdensDeServico
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count() })
            .ToListAsync(ct);

        return contagem.ToDictionary(c => c.Status, c => c.Quantidade);
    }

    public async Task<IReadOnlyList<OrdemDeServico>> ListarComPecaPendenteAsync(PecaId pecaId,
        CancellationToken ct = default) =>
        await Completa
            .Where(o => o.Status != StatusOS.CANCELADA && o.Status != StatusOS.ENTREGUE)
            .Where(o => o.ItensDePeca.Any(i => i.PecaId == pecaId && i.Status == StatusItemPeca.PENDENTE_COMPRA))
            .ToListAsync(ct);

    public async Task AdicionarAsync(OrdemDeServico ordem, CancellationToken ct = default)
    {
        var entrada = await ctx.OrdensDeServico.AddAsync(ordem, ct);

        // Em SQL Server o número vem de IDENTITY. Nos demais provedores, atribuímos aqui.
        if (entrada.Property(o => o.Numero).Metadata.ValueGenerated == ValueGenerated.Never)
        {
            var ultimo = await ctx.OrdensDeServico.MaxAsync(o => (long?)o.Numero, ct) ?? 999;
            entrada.Property(o => o.Numero).CurrentValue = ultimo + 1;
        }
    }

    public async Task<IReadOnlyList<TempoMedioPorServico>> ObterTempoMedioPorServicoAsync(
        DateTime? inicio, DateTime? fim, CancellationToken ct = default)
    {
        var consulta = ctx.OrdensDeServico
            .Where(o => o.Status == StatusOS.FINALIZADA || o.Status == StatusOS.ENTREGUE)
            .Where(o => o.Execucao.Inicio != null && o.Execucao.Fim != null);

        if (inicio is not null) consulta = consulta.Where(o => o.Execucao.Fim >= inicio);
        if (fim is not null) consulta = consulta.Where(o => o.Execucao.Fim <= fim);

        var concluidas = await consulta
            .Select(o => new
            {
                o.Execucao.Inicio,
                o.Execucao.Fim,
                o.Execucao.TempoAguardandoAprovacao,
                Servicos = o.ItensDeServico.Select(i => i.ServicoId).ToList()
            })
            .ToListAsync(ct);

        if (concluidas.Count == 0) return [];

        var duracoesPorServico = new Dictionary<ServicoId, List<double>>();

        foreach (var os in concluidas)
        {
            var duracao = (os.Fim!.Value - os.Inicio!.Value - os.TempoAguardandoAprovacao).TotalMinutes;
            if (duracao < 0) duracao = 0;

            foreach (var servicoId in os.Servicos)
            {
                if (!duracoesPorServico.TryGetValue(servicoId, out var lista))
                    duracoesPorServico[servicoId] = lista = [];

                lista.Add(duracao);
            }
        }

        var ids = duracoesPorServico.Keys.ToList();

        var catalogo = await ctx.Servicos
            .Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, s.Nome, s.TempoPadraoExecucao })
            .ToListAsync(ct);

        return catalogo
            .Select(s => new TempoMedioPorServico(
                s.Id,
                s.Nome,
                duracoesPorServico[s.Id].Count,
                duracoesPorServico[s.Id].Average(),
                s.TempoPadraoExecucao.Minutos))
            .ToList();
    }

    private IQueryable<OrdemDeServico> Filtrar(StatusOS? status, ClienteId? clienteId, VeiculoId? veiculoId)
    {
        var consulta = Completa;

        if (status is not null) consulta = consulta.Where(o => o.Status == status);
        if (clienteId is not null) consulta = consulta.Where(o => o.ClienteId == clienteId);
        if (veiculoId is not null) consulta = consulta.Where(o => o.VeiculoId == veiculoId);

        return consulta;
    }
}
