using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.OrdensServico;

public sealed class ItemPeca : Entity
{
    public Guid PecaId { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public Money PrecoUnitario { get; private set; } = null!;

    public Money Subtotal => PrecoUnitario * Quantidade;

    private ItemPeca()
    {
    }

    internal ItemPeca(Guid pecaId, string descricao, int quantidade, Money precoUnitario)
    {
        Id = Guid.NewGuid();
        PecaId = pecaId;
        Descricao = descricao;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }
}
