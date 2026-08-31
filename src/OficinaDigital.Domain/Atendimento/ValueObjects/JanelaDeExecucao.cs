using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Atendimento.ValueObjects;

public sealed class JanelaDeExecucao : ValueObject
{
    public DateTime? Inicio { get; }
    public DateTime? Fim { get; }
    public TimeSpan TempoAguardandoAprovacao { get; }

    public DateTime? SuspensaEm { get; }

    private JanelaDeExecucao(DateTime? inicio, DateTime? fim, TimeSpan tempoAguardandoAprovacao,
        DateTime? suspensaEm)
    {
        Inicio = inicio;
        Fim = fim;
        TempoAguardandoAprovacao = tempoAguardandoAprovacao;
        SuspensaEm = suspensaEm;
    }

    public static JanelaDeExecucao NaoIniciada() => new(null, null, TimeSpan.Zero, null);

    public JanelaDeExecucao Iniciar()
    {
        DomainException.Se(Inicio is not null, "A execução desta OS já foi iniciada.");
        return new JanelaDeExecucao(DateTime.UtcNow, null, TimeSpan.Zero, null);
    }

    public JanelaDeExecucao Suspender()
    {
        DomainException.Se(Inicio is null, "Não é possível suspender uma execução que não começou.");
        DomainException.Se(SuspensaEm is not null, "A execução já está suspensa aguardando aprovação.");
        return new JanelaDeExecucao(Inicio, Fim, TempoAguardandoAprovacao, DateTime.UtcNow);
    }

    public JanelaDeExecucao Retomar()
    {
        DomainException.Se(SuspensaEm is null, "A execução não está suspensa.");
        var acumulado = TempoAguardandoAprovacao + (DateTime.UtcNow - SuspensaEm!.Value);
        return new JanelaDeExecucao(Inicio, Fim, acumulado, null);
    }

    public JanelaDeExecucao Concluir()
    {
        DomainException.Se(Inicio is null, "Não é possível concluir uma execução que não começou.");
        DomainException.Se(Fim is not null, "A execução desta OS já foi concluída.");

        // Se concluiu enquanto suspensa, fecha a suspensão antes de encerrar.
        var janela = SuspensaEm is not null ? Retomar() : this;
        return new JanelaDeExecucao(janela.Inicio, DateTime.UtcNow, janela.TempoAguardandoAprovacao, null);
    }

    public TimeSpan? DuracaoReal =>
        Inicio is null || Fim is null ? null : Fim.Value - Inicio.Value - TempoAguardandoAprovacao;

    public int? DuracaoRealEmMinutos => DuracaoReal is null ? null : (int)Math.Round(DuracaoReal.Value.TotalMinutes);

    protected override IEnumerable<object?> ComponentesDeIgualdade()
    {
        yield return Inicio;
        yield return Fim;
        yield return TempoAguardandoAprovacao;
        yield return SuspensaEm;
    }
}
