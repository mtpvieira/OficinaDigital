using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Domain.Orcamentos;

public class ItemOrcado
{
    public Guid Id { get; private set; }
    public TipoItemOrcado Tipo { get; private set; }
    public Guid ReferenciaId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public Dinheiro PrecoUnitario { get; private set; } = null!;

    protected ItemOrcado() { }

    internal ItemOrcado(TipoItemOrcado tipo, Guid referenciaId, string descricao, decimal quantidade,
        Dinheiro precoUnitario)
    {
        DomainException.Se(quantidade <= 0, "A quantidade do item orçado deve ser maior que zero.");
        DomainException.Se(string.IsNullOrWhiteSpace(descricao), "A descrição do item orçado é obrigatória.");

        Tipo = tipo;
        ReferenciaId = referenciaId;
        Descricao = descricao.Trim();
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public Dinheiro Subtotal => PrecoUnitario.Multiplicar(Quantidade);
}

public class VersaoOrcamento
{
    private readonly List<ItemOrcado> _itens = [];

    public Guid Id { get; private set; }
    public int Numero { get; private set; }
    public IReadOnlyCollection<ItemOrcado> Itens => _itens.AsReadOnly();
    public Dinheiro Total { get; private set; } = null!;
    public DateTime GeradoEm { get; private set; }
    public DateTime? EnviadoEm { get; private set; }
    public RespostaCliente? Resposta { get; private set; }

    protected VersaoOrcamento() { }

    internal VersaoOrcamento(int numero, IEnumerable<ItemOrcado> itens)
    {
        var lista = itens.ToList();
        DomainException.Se(lista.Count == 0, "O orçamento precisa de ao menos um item.");

        Numero = numero;
        _itens.AddRange(lista);
        Total = lista.Aggregate(Dinheiro.Zero, (acc, i) => acc.Somar(i.Subtotal));
        GeradoEm = DateTime.UtcNow;
    }

    public bool FoiRespondida => Resposta is not null;

    public bool EstaVigenteParaResposta => !FoiRespondida && EnviadoEm is not null;

    internal void MarcarComoEnviada() => EnviadoEm = DateTime.UtcNow;

    internal void Responder(DecisaoCliente decisao)
    {
        DomainException.Se(FoiRespondida,
            "Esta versão do orçamento já foi respondida. A resposta do cliente é irreversível.");
        DomainException.Se(EnviadoEm is null, "O orçamento ainda não foi enviado ao cliente.");

        Resposta = RespostaCliente.De(decisao);
    }
}
