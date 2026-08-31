using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Atendimento;

public class ItemDeServico
{
    public Guid Id { get; private set; }
    public ServicoId ServicoId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public Dinheiro PrecoCongelado { get; private set; } = null!;
    public int TempoPadraoMinutos { get; private set; }
    public OrigemItem Origem { get; private set; }

    public bool AguardandoAprovacaoDoCliente { get; private set; }

    public DateTime RegistradoEm { get; private set; }

    protected ItemDeServico() { }

    internal ItemDeServico(ServicoId servicoId, string descricao, Dinheiro precoCongelado,
        int tempoPadraoMinutos, OrigemItem origem)
    {
        ServicoId = servicoId;
        Descricao = descricao;
        PrecoCongelado = precoCongelado;
        TempoPadraoMinutos = tempoPadraoMinutos;
        Origem = origem;
        AguardandoAprovacaoDoCliente = origem == OrigemItem.ADICIONAL;
        RegistradoEm = DateTime.UtcNow;
    }

    internal void Aprovar() => AguardandoAprovacaoDoCliente = false;

    public Dinheiro Subtotal => PrecoCongelado;
}

public class ItemDePeca
{
    public Guid Id { get; private set; }
    public PecaId PecaId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public Dinheiro PrecoCongelado { get; private set; } = null!;
    public OrigemItem Origem { get; private set; }
    public StatusItemPeca Status { get; private set; }
    public bool AguardandoAprovacaoDoCliente { get; private set; }
    public DateTime RegistradoEm { get; private set; }

    protected ItemDePeca() { }

    internal ItemDePeca(PecaId pecaId, string descricao, decimal quantidade, Dinheiro precoCongelado,
        OrigemItem origem)
    {
        DomainException.Se(quantidade <= 0, "A quantidade da peça deve ser maior que zero.");

        PecaId = pecaId;
        Descricao = descricao;
        Quantidade = quantidade;
        PrecoCongelado = precoCongelado;
        Origem = origem;
        Status = StatusItemPeca.AGUARDANDO_RESERVA;
        AguardandoAprovacaoDoCliente = origem == OrigemItem.ADICIONAL;
        RegistradoEm = DateTime.UtcNow;
    }

    internal void MarcarComoReservada() => Status = StatusItemPeca.RESERVADA;

    internal void MarcarComoPendenteDeCompra() => Status = StatusItemPeca.PENDENTE_COMPRA;

    internal void MarcarComoSubtraida() => Status = StatusItemPeca.SUBTRAIDA;

    internal void Aprovar() => AguardandoAprovacaoDoCliente = false;

    public Dinheiro Subtotal => PrecoCongelado.Multiplicar(Quantidade);
}
