using Microsoft.Extensions.Logging;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Catalogo.Events;
using OficinaDigital.Domain.Atendimento.Events;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Estoque;
using OficinaDigital.Domain.Estoque.Events;
using OficinaDigital.Domain.Estoque.Repositories;

namespace OficinaDigital.Application.Politicas;

public class AbrirPosicaoDeEstoqueAoCadastrarPeca(IItemEstoqueRepository estoque)
    : IPoliticaDeDominio<PecaCadastrada>
{
    public async Task ExecutarAsync(PecaCadastrada evento, CancellationToken ct = default)
    {
        if (await estoque.ObterPorPecaAsync(evento.PecaId, ct) is not null) return;

        var item = ItemEstoque.Abrir(evento.PecaId, evento.Unidade, evento.EstoqueMinimo);
        await estoque.AdicionarAsync(item, ct);
    }
}

public class ReservarPecasAoRegistrarNaOs(
    IItemEstoqueRepository estoque,
    IOrdemDeServicoRepository ordens,
    ILogger<ReservarPecasAoRegistrarNaOs> logger)
    : IPoliticaDeDominio<PecasDaOsRegistradas>
{
    public async Task ExecutarAsync(PecasDaOsRegistradas evento, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorIdAsync(evento.OrdemDeServicoId, ct);
        if (os is null) return;

        foreach (var (pecaId, quantidade) in evento.Pecas)
        {
            var item = await estoque.ObterPorPecaAsync(pecaId, ct);

            if (item is null || !item.PodeReservar(quantidade))
            {
                os.MarcarPecaComoPendenteDeCompra(pecaId);
                item?.SinalizarFalta(evento.OrdemDeServicoId, quantidade);

                logger.LogInformation(
                    "Falta de peça {PecaId} para a OS {OsId}: solicitado {Quantidade}.",
                    pecaId.Valor, evento.OrdemDeServicoId.Valor, quantidade);

                continue;
            }

            item.Reservar(evento.OrdemDeServicoId, quantidade);
            os.ConfirmarReservaDaPeca(pecaId);
        }
    }
}

public class ReservarPecasPendentesAoRegistrarEntrada(
    IItemEstoqueRepository estoque,
    IOrdemDeServicoRepository ordens,
    ILogger<ReservarPecasPendentesAoRegistrarEntrada> logger)
    : IPoliticaDeDominio<EntradaDePecasRegistrada>
{
    public async Task ExecutarAsync(EntradaDePecasRegistrada evento, CancellationToken ct = default)
    {
        var pendentes = await ordens.ListarComPecaPendenteAsync(evento.PecaId, ct);
        if (pendentes.Count == 0) return;

        var item = await estoque.ObterPorPecaAsync(evento.PecaId, ct);
        if (item is null) return;

        foreach (var os in pendentes.OrderBy(o => o.AbertaEm))
        {
            var itemDaOs = os.ItensDePeca.FirstOrDefault(i =>
                i.PecaId == evento.PecaId && i.Status == Domain.Atendimento.ValueObjects.StatusItemPeca.PENDENTE_COMPRA);

            if (itemDaOs is null || !item.PodeReservar(itemDaOs.Quantidade)) continue;

            item.Reservar(os.Id, itemDaOs.Quantidade);
            os.ConfirmarReservaDaPeca(evento.PecaId);

            logger.LogInformation("Peça {PecaId} reservada para a OS {OsId} após entrada em estoque.",
                evento.PecaId.Valor, os.Id.Valor);
        }
    }
}

public class SubtrairPecasAoIniciarServico(
    IItemEstoqueRepository estoque,
    IOrdemDeServicoRepository ordens)
    : IPoliticaDeDominio<ServicoIniciado>
{
    public async Task ExecutarAsync(ServicoIniciado evento, CancellationToken ct = default)
    {
        var os = await ordens.ObterPorIdAsync(evento.OrdemDeServicoId, ct);
        if (os is null) return;

        var comReserva = await estoque.ObterComReservaDaOsAsync(evento.OrdemDeServicoId, ct);

        foreach (var item in comReserva)
            item.Subtrair(evento.OrdemDeServicoId);

        os.ConfirmarSubtracaoDasPecas();
    }
}

public class LiberarReservasAoCancelarOs(IItemEstoqueRepository estoque)
    : IPoliticaDeDominio<OsCancelada>
{
    public async Task ExecutarAsync(OsCancelada evento, CancellationToken ct = default)
    {
        var comReserva = await estoque.ObterComReservaDaOsAsync(evento.OrdemDeServicoId, ct);

        foreach (var item in comReserva)
            item.LiberarReserva(evento.OrdemDeServicoId);
    }
}
