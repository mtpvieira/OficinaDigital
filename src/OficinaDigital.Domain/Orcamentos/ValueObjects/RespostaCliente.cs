using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Orcamentos.ValueObjects;

public enum TipoOrcamento
{
    PRINCIPAL = 1,
    COMPLEMENTAR = 2
}

public enum DecisaoCliente
{
    APROVADO = 1,
    REPROVADO = 2
}

public enum TipoItemOrcado
{
    SERVICO = 1,
    PECA = 2
}

public sealed class RespostaCliente : ValueObject
{
    public DecisaoCliente Decisao { get; }
    public DateTime RespondidoEm { get; }

    private RespostaCliente(DecisaoCliente decisao, DateTime respondidoEm)
    {
        Decisao = decisao;
        RespondidoEm = respondidoEm;
    }

    public static RespostaCliente De(DecisaoCliente decisao) => new(decisao, DateTime.UtcNow);

    public bool Aprovou => Decisao == DecisaoCliente.APROVADO;

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Decisao;
        yield return RespondidoEm;
    }
}
