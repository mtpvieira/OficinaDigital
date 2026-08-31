using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Estoque;

public enum TipoMovimentoEstoque
{
    ENTRADA = 1,
    SUBTRACAO = 2
}

public class MovimentoEstoque
{
    public Guid Id { get; private set; }
    public TipoMovimentoEstoque Tipo { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal SaldoResultante { get; private set; }
    public OrdemDeServicoId? OrdemDeServicoId { get; private set; }
    public string? Motivo { get; private set; }
    public DateTime RegistradoEm { get; private set; }

    protected MovimentoEstoque() { }

    internal MovimentoEstoque(TipoMovimentoEstoque tipo, decimal quantidade, decimal saldoResultante,
        OrdemDeServicoId? ordemDeServicoId = null, string? motivo = null)
    {
        Tipo = tipo;
        Quantidade = quantidade;
        SaldoResultante = saldoResultante;
        OrdemDeServicoId = ordemDeServicoId;
        Motivo = motivo;
        RegistradoEm = DateTime.UtcNow;
    }
}
