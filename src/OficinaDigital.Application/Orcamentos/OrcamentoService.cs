using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Atendimento;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Catalogo.Repositories;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos;
using OficinaDigital.Domain.Orcamentos.Repositories;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Application.Orcamentos;

public class OrcamentoService(
    IOrcamentoRepository orcamentos,
    IOrdemDeServicoRepository ordens,
    IServicoRepository servicos,
    IPecaRepository pecas,
    IUnitOfWork uow)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<OrcamentoResponse> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        OrcamentoResponse.De(await CarregarAsync(id, ct));

    public async Task<IReadOnlyList<OrcamentoResponse>> ListarPorOsAsync(Guid ordemDeServicoId,
        CancellationToken ct = default)
    {
        var lista = await orcamentos.ListarPorOsAsync(new OrdemDeServicoId(ordemDeServicoId), ct);
        return lista.Select(OrcamentoResponse.De).ToList();
    }

    public async Task<OrcamentoResponse> EnviarAsync(Guid id, CancellationToken ct = default)
    {
        var orcamento = await CarregarAsync(id, ct);

        orcamento.Enviar();
        await uow.CommitAsync(ct);

        return OrcamentoResponse.De(orcamento);
    }

    public async Task<OrcamentoResponse> AlterarAsync(Guid id, CancellationToken ct = default)
    {
        var orcamento = await CarregarAsync(id, ct);

        var os = await ordens.ObterPorIdAsync(orcamento.OrdemDeServicoId, ct)
                 ?? throw new RecursoNaoEncontradoException("Ordem de serviço",
                     orcamento.OrdemDeServicoId.Valor);

        var itens = await MontarItensAsync(os, orcamento.Tipo, ct);

        orcamento.Alterar(itens);
        await uow.CommitAsync(ct);

        return OrcamentoResponse.De(orcamento);
    }

    public async Task<OrcamentoResponse> ResponderAsync(Guid id, ResponderOrcamentoRequest request,
        ClienteId? clienteAutenticado, CancellationToken ct = default)
    {
        var orcamento = await CarregarAsync(id, ct);

        if (clienteAutenticado is not null)
            await GarantirQueOOrcamentoEhDoClienteAsync(orcamento, clienteAutenticado.Value, ct);

        orcamento.Responder(request.Decisao);
        await uow.CommitAsync(ct);

        return OrcamentoResponse.De(orcamento);
    }

    public async Task<List<(TipoItemOrcado, Guid, string, decimal, Dinheiro)>> MontarItensAsync(
        OrdemDeServico os, TipoOrcamento tipo, CancellationToken ct)
    {
        var origem = tipo == TipoOrcamento.PRINCIPAL ? OrigemItem.DIAGNOSTICO : OrigemItem.ADICIONAL;
        var hoje = Hoje;

        var itens = new List<(TipoItemOrcado, Guid, string, decimal, Dinheiro)>();

        var itensDeServico = os.ItensDeServico.Where(i => i.Origem == origem).ToList();
        if (itensDeServico.Count > 0)
        {
            var catalogo = (await servicos.ObterPorIdsAsync(itensDeServico.Select(i => i.ServicoId), ct))
                .ToDictionary(s => s.Id);

            itens.AddRange(itensDeServico.Select(i => (
                TipoItemOrcado.SERVICO,
                i.ServicoId.Valor,
                catalogo.TryGetValue(i.ServicoId, out var s) ? s.Nome : i.Descricao,
                1m,
                PrecoDoDiaOuCongelado(catalogo, i.ServicoId, hoje, i.PrecoCongelado))));
        }

        var itensDePeca = os.ItensDePeca.Where(i => i.Origem == origem).ToList();
        if (itensDePeca.Count > 0)
        {
            var catalogo = (await pecas.ObterPorIdsAsync(itensDePeca.Select(i => i.PecaId), ct))
                .ToDictionary(p => p.Id);

            itens.AddRange(itensDePeca.Select(i => (
                TipoItemOrcado.PECA,
                i.PecaId.Valor,
                catalogo.TryGetValue(i.PecaId, out var p) ? $"{p.Sku.Codigo} - {p.Descricao}" : i.Descricao,
                i.Quantidade,
                i.PrecoCongelado)));
        }

        return itens;
    }

    private static Dinheiro PrecoDoDiaOuCongelado(
        IReadOnlyDictionary<ServicoId, Domain.Catalogo.Servico> catalogo, ServicoId id, DateOnly hoje,
        Dinheiro congelado) =>
        catalogo.TryGetValue(id, out var servico) ? servico.PrecoVigenteEm(hoje) : congelado;

    private async Task<Orcamento> CarregarAsync(Guid id, CancellationToken ct) =>
        await orcamentos.ObterPorIdAsync(new OrcamentoId(id), ct)
        ?? throw new RecursoNaoEncontradoException("Orçamento", id);

    private async Task GarantirQueOOrcamentoEhDoClienteAsync(Orcamento orcamento, ClienteId clienteId,
        CancellationToken ct)
    {
        var os = await ordens.ObterPorIdAsync(orcamento.OrdemDeServicoId, ct)
                 ?? throw new RecursoNaoEncontradoException("Ordem de serviço", orcamento.OrdemDeServicoId.Valor);

        if (os.ClienteId != clienteId)
            throw new AcessoNegadoException("Este orçamento pertence a outro cliente.");
    }
}
