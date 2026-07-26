using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.OrdensServico;

public sealed class ItemServico : Entity
{
    public Guid ServicoId { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public Money Preco { get; private set; } = null!;

    private ItemServico()
    {
    }

    internal ItemServico(Guid servicoId, string descricao, Money preco)
    {
        Id = Guid.NewGuid();
        ServicoId = servicoId;
        Descricao = descricao;
        Preco = preco;
    }
}
