using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Cadastro.Repositories;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.Repositories;

namespace OficinaDigital.Application.Atendimento;

public sealed record ItemAcompanhamentoDto(string Descricao, decimal Valor, bool AguardandoSuaAprovacao);

public sealed record AcompanhamentoResponse(
    Guid Id,
    long Numero,
    string Placa,
    StatusOS Status,
    string StatusDescricao,
    string MensagemParaOCliente,
    decimal Total,
    bool PossuiOrcamentoAguardandoResposta,
    Guid? OrcamentoParaResponderId,
    IReadOnlyList<ItemAcompanhamentoDto> Servicos,
    IReadOnlyList<ItemAcompanhamentoDto> Pecas,
    DateTime AbertaEm);

public class AcompanhamentoService(
    IOrdemDeServicoRepository ordens,
    IVeiculoRepository veiculos,
    IOrcamentoRepository orcamentos)
{
    public async Task<IReadOnlyList<AcompanhamentoResponse>> ListarDoClienteAsync(ClienteId clienteId,
        CancellationToken ct = default)
    {
        var lista = await ordens.ListarAsync(null, clienteId, null, 1, 100, ct);

        var resultado = new List<AcompanhamentoResponse>();
        foreach (var os in lista)
            resultado.Add(await MontarAsync(os.Id, clienteId, ct));

        return resultado;
    }

    public async Task<AcompanhamentoResponse> ObterAsync(Guid ordemDeServicoId, ClienteId clienteId,
        CancellationToken ct = default) =>
        await MontarAsync(new OrdemDeServicoId(ordemDeServicoId), clienteId, ct);

    private async Task<AcompanhamentoResponse> MontarAsync(OrdemDeServicoId osId, ClienteId clienteId,
        CancellationToken ct)
    {
        var os = await ordens.ObterPorIdAsync(osId, ct)
                 ?? throw new RecursoNaoEncontradoException("Ordem de serviço", osId.Valor);

        if (os.ClienteId != clienteId)
            throw new AcessoNegadoException("Esta ordem de serviço pertence a outro cliente.");

        var veiculo = await veiculos.ObterPorIdAsync(os.VeiculoId, ct);

        var aguardando = await orcamentos.ListarPorOsAsync(os.Id, ct);

        var paraResponder = aguardando
            .Select(o => new { Orcamento = o, Versao = o.ObterVersaoVigente() })
            .FirstOrDefault(x => x.Versao.EnviadoEm is not null && x.Versao.EstaVigenteParaResposta);

        return new AcompanhamentoResponse(
            os.Id.Valor,
            os.Numero,
            veiculo?.Placa.Valor ?? "-",
            os.Status,
            OrdemDeServicoResponse.DescreverStatus(os.Status),
            MensagemPara(os.Status, paraResponder is not null),
            os.Total.Valor,
            paraResponder is not null,
            paraResponder?.Orcamento.Id.Valor,
            os.ItensDeServico
                .Select(i => new ItemAcompanhamentoDto(i.Descricao, i.PrecoCongelado.Valor,
                    i.AguardandoAprovacaoDoCliente))
                .ToList(),
            os.ItensDePeca
                .Select(i => new ItemAcompanhamentoDto(i.Descricao, i.Subtotal.Valor,
                    i.AguardandoAprovacaoDoCliente))
                .ToList(),
            os.AbertaEm);
    }

    private static string MensagemPara(StatusOS status, bool temOrcamentoParaResponder) => status switch
    {
        StatusOS.RECEBIDA => "Recebemos seu veículo. Em breve iniciaremos o diagnóstico.",
        StatusOS.EM_DIAGNOSTICO => "Nosso mecânico está avaliando seu veículo.",
        StatusOS.AGUARDANDO_APROVACAO when temOrcamentoParaResponder =>
            "Há um orçamento aguardando sua aprovação. Responda pelo aplicativo para seguirmos.",
        StatusOS.AGUARDANDO_APROVACAO => "Aguardando aprovação do orçamento.",
        StatusOS.EM_EXECUCAO => "Serviço em execução.",
        StatusOS.FINALIZADA => "Serviço concluído! Seu veículo está pronto para retirada.",
        StatusOS.ENTREGUE => "Veículo entregue. Obrigado pela preferência!",
        StatusOS.CANCELADA => "Esta ordem de serviço foi cancelada.",
        _ => string.Empty
    };
}
