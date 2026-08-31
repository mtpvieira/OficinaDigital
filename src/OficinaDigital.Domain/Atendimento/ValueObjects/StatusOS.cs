namespace OficinaDigital.Domain.Atendimento.ValueObjects;

public enum StatusOS
{
    RECEBIDA = 1,
    EM_DIAGNOSTICO = 2,
    AGUARDANDO_APROVACAO = 3,
    EM_EXECUCAO = 4,
    FINALIZADA = 5,
    ENTREGUE = 6,
    CANCELADA = 7
}

public enum MotivoCancelamento
{
    REPROVADO_PELO_CLIENTE = 1,
    CANCELADO_PELA_OFICINA = 2
}

public enum OrigemItem
{
    DIAGNOSTICO = 1,
    ADICIONAL = 2
}

public enum StatusItemPeca
{
    AGUARDANDO_RESERVA = 1,
    RESERVADA = 2,
    PENDENTE_COMPRA = 3,
    SUBTRAIDA = 4
}
