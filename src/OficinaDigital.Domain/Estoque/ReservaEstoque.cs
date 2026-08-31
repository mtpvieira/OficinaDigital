using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Estoque;

public class ReservaEstoque
{
    public Guid Id { get; private set; }
    public OrdemDeServicoId OrdemDeServicoId { get; private set; }
    public decimal Quantidade { get; private set; }
    public DateTime ReservadaEm { get; private set; }

    protected ReservaEstoque() { }

    internal ReservaEstoque(OrdemDeServicoId ordemDeServicoId, decimal quantidade)
    {
        OrdemDeServicoId = ordemDeServicoId;
        Quantidade = quantidade;
        ReservadaEm = DateTime.UtcNow;
    }

    internal void Acrescentar(decimal adicional) => Quantidade += adicional;
}
