using Microsoft.Extensions.Logging;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Orcamentos;
using OficinaDigital.Domain.Atendimento.Events;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Estoque.Repositories;
using OficinaDigital.Domain.Orcamentos;
using OficinaDigital.Domain.Orcamentos.Events;
using OficinaDigital.Domain.Orcamentos.Repositories;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Application.Politicas;

public class GerarOrcamentoAoFinalizarDiagnostico(
    IOrdemDeServicoRepository ordens,
    IOrcamentoRepository orcamentos,
    OrcamentoService servicoDeOrcamento,
    ILogger<GerarOrcamentoAoFinalizarDiagnostico> logger)
    : IPoliticaDeDominio<DiagnosticoFinalizado>
{
    public async Task ExecutarAsync(DiagnosticoFinalizado evento, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorIdAsync(evento.OrdemDeServicoId, ct);
        if (os is null) return;

        if (os.PossuiPecaPendenteDeCompra)
        {
            logger.LogInformation(
                "OS {OsId} com peça pendente de compra: o orçamento será gerado quando a peça entrar no estoque.",
                evento.OrdemDeServicoId.Valor);
            return;
        }

        if (await orcamentos.ObterPrincipalDaOsAsync(os.Id, ct) is not null) return;

        var itens = await servicoDeOrcamento.MontarItensAsync(os, TipoOrcamento.PRINCIPAL, ct);
        if (itens.Count == 0) return;

        var orcamento = Orcamento.Gerar(os.Id, TipoOrcamento.PRINCIPAL, itens);

        await orcamentos.AdicionarAsync(orcamento, ct);
        os.VincularOrcamento(orcamento.Id);
    }
}

public class GerarComplementarAoRegistrarItensAdicionais(
    IOrdemDeServicoRepository ordens,
    IOrcamentoRepository orcamentos,
    IItemEstoqueRepository estoque,
    OrcamentoService servicoDeOrcamento)
    : IPoliticaDeDominio<ServicosEPecasAdicionaisRegistrados>
{
    public async Task ExecutarAsync(ServicosEPecasAdicionaisRegistrados evento, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorIdAsync(evento.OrdemDeServicoId, ct);
        if (os is null) return;

        foreach (var itemDaOs in os.ItensDePeca
                     .Where(i => i.Origem == OrigemItem.ADICIONAL && i.Status == StatusItemPeca.AGUARDANDO_RESERVA))
        {
            var itemEstoque = await estoque.ObterPorPecaAsync(itemDaOs.PecaId, ct);

            if (itemEstoque is null || !itemEstoque.PodeReservar(itemDaOs.Quantidade))
            {
                os.MarcarPecaComoPendenteDeCompra(itemDaOs.PecaId);
                itemEstoque?.SinalizarFalta(os.Id, itemDaOs.Quantidade);
                continue;
            }

            itemEstoque.Reservar(os.Id, itemDaOs.Quantidade);
            os.ConfirmarReservaDaPeca(itemDaOs.PecaId);
        }

        var itens = await servicoDeOrcamento.MontarItensAsync(os, TipoOrcamento.COMPLEMENTAR, ct);
        if (itens.Count == 0) return;

        var complementar = Orcamento.Gerar(os.Id, TipoOrcamento.COMPLEMENTAR, itens);
        await orcamentos.AdicionarAsync(complementar, ct);

        complementar.Enviar();
    }
}

public class AtualizarOsAoEnviarOrcamento(IOrdemDeServicoRepository ordens)
    : IPoliticaDeDominio<OrcamentoEnviadoAoCliente>
{
    public async Task ExecutarAsync(OrcamentoEnviadoAoCliente evento, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorIdAsync(evento.OrdemDeServicoId, ct);
        if (os is null) return;

        if (evento.Tipo == TipoOrcamento.PRINCIPAL)
        {
            os.VincularOrcamento(evento.OrcamentoId);
            os.MarcarOrcamentoEnviado();
            return;
        }

        if (os.Status == StatusOS.EM_EXECUCAO)
            os.SuspenderParaAprovacaoComplementar();
    }
}

public class AoAprovarOrcamento(IOrdemDeServicoRepository ordens) : IPoliticaDeDominio<OrcamentoAprovado>
{
    public async Task ExecutarAsync(OrcamentoAprovado evento, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorIdAsync(evento.OrdemDeServicoId, ct);
        if (os is null) return;

        if (evento.Tipo == TipoOrcamento.PRINCIPAL)
            os.RegistrarAprovacaoDoOrcamento();
        else
            os.AprovarItensAdicionais();
    }
}

public class AoReprovarOrcamento(
    IOrdemDeServicoRepository ordens,
    IItemEstoqueRepository estoque)
    : IPoliticaDeDominio<OrcamentoReprovado>
{
    public async Task ExecutarAsync(OrcamentoReprovado evento, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorIdAsync(evento.OrdemDeServicoId, ct);
        if (os is null) return;

        if (evento.Tipo == TipoOrcamento.PRINCIPAL)
        {
            os.Cancelar(MotivoCancelamento.REPROVADO_PELO_CLIENTE,
                "Orçamento reprovado pelo cliente.");
            return;
        }

        os.RemoverItensAdicionais();

        foreach (var item in await estoque.ObterComReservaDaOsAsync(os.Id, ct))
            item.LiberarReserva(os.Id);
    }
}
