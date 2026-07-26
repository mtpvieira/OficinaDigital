using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.OrdensServico;

public sealed class OrdemDeServico : AggregateRoot
{
    private readonly List<ItemServico> _itensServico = [];
    private readonly List<ItemPeca> _itensPeca = [];

    public string Numero { get; private set; } = string.Empty;
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public StatusOS Status { get; private set; }
    public Money Orcamento { get; private set; } = Money.Zero;

    public DateTime CriadaEm { get; private set; }
    public DateTime? EnviadaAprovacaoEm { get; private set; }
    public DateTime? AprovadaEm { get; private set; }
    public DateTime? ExecucaoIniciadaEm { get; private set; }
    public DateTime? FinalizadaEm { get; private set; }
    public DateTime? EntregueEm { get; private set; }

    public IReadOnlyCollection<ItemServico> ItensServico => _itensServico.AsReadOnly();
    public IReadOnlyCollection<ItemPeca> ItensPeca => _itensPeca.AsReadOnly();

    public TimeSpan? TempoDeExecucao =>
        ExecucaoIniciadaEm.HasValue && FinalizadaEm.HasValue
            ? FinalizadaEm.Value - ExecucaoIniciadaEm.Value
            : null;

    private OrdemDeServico()
    {
    }

    private OrdemDeServico(string numero, Guid clienteId, Guid veiculoId)
    {
        Id = Guid.NewGuid();
        Numero = numero;
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOS.Recebida;
        CriadaEm = DateTime.UtcNow;

        RaiseDomainEvent(new OSCriada(Id, Numero, CriadaEm));
    }

    public static OrdemDeServico Criar(string numero, Guid clienteId, Guid veiculoId)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new DomainException("Número da ordem de serviço é obrigatório.");
        if (clienteId == Guid.Empty)
            throw new DomainException("Ordem de serviço deve estar vinculada a um cliente.");
        if (veiculoId == Guid.Empty)
            throw new DomainException("Ordem de serviço deve estar vinculada a um veículo.");

        return new OrdemDeServico(numero.Trim(), clienteId, veiculoId);
    }

    public void AdicionarItemServico(Guid servicoId, string descricao, Money preco)
    {
        GarantirEdicaoDeItensPermitida();

        if (servicoId == Guid.Empty)
            throw new DomainException("Serviço inválido.");

        _itensServico.Add(new ItemServico(servicoId, descricao, preco));
        RecalcularOrcamento();
    }

    public void AdicionarItemPeca(Guid pecaId, string descricao, int quantidade, Money precoUnitario)
    {
        GarantirEdicaoDeItensPermitida();

        if (pecaId == Guid.Empty)
            throw new DomainException("Peça inválida.");
        if (quantidade <= 0)
            throw new DomainException("Quantidade da peça deve ser maior que zero.");

        _itensPeca.Add(new ItemPeca(pecaId, descricao, quantidade, precoUnitario));
        RecalcularOrcamento();
    }

    public void IniciarDiagnostico()
    {
        GarantirTransicao(StatusOS.Recebida, "iniciar diagnóstico");
        Status = StatusOS.EmDiagnostico;
    }

    public void EnviarOrcamento()
    {
        GarantirTransicao(StatusOS.EmDiagnostico, "enviar orçamento");

        if (_itensServico.Count == 0 && _itensPeca.Count == 0)
            throw new DomainException("Não é possível enviar orçamento sem itens de serviço ou peça.");

        RecalcularOrcamento();
        Status = StatusOS.AguardandoAprovacao;
        EnviadaAprovacaoEm = DateTime.UtcNow;

        RaiseDomainEvent(new OrcamentoEnviado(Id, Numero, Orcamento, EnviadaAprovacaoEm.Value));
    }

    public void Aprovar()
    {
        GarantirTransicao(StatusOS.AguardandoAprovacao, "aprovar orçamento");

        var agora = DateTime.UtcNow;
        Status = StatusOS.EmExecucao;
        AprovadaEm = agora;
        ExecucaoIniciadaEm = agora;

        RaiseDomainEvent(new OSAprovada(Id, Numero, agora));
        RaiseDomainEvent(new ExecucaoIniciada(Id, Numero, agora));
    }

    public void ConcluirExecucao()
    {
        GarantirTransicao(StatusOS.EmExecucao, "concluir execução");

        Status = StatusOS.Finalizada;
        FinalizadaEm = DateTime.UtcNow;

        RaiseDomainEvent(new OSFinalizada(Id, Numero, FinalizadaEm.Value));
    }

    public void Entregar()
    {
        GarantirTransicao(StatusOS.Finalizada, "entregar veículo");

        Status = StatusOS.Entregue;
        EntregueEm = DateTime.UtcNow;

        RaiseDomainEvent(new OSEntregue(Id, Numero, EntregueEm.Value));
    }

    private void RecalcularOrcamento()
    {
        var totalServicos = _itensServico.Aggregate(Money.Zero, (total, item) => total + item.Preco);
        var totalPecas = _itensPeca.Aggregate(Money.Zero, (total, item) => total + item.Subtotal);

        Orcamento = totalServicos + totalPecas;
    }

    private void GarantirEdicaoDeItensPermitida()
    {
        if (Status != StatusOS.Recebida && Status != StatusOS.EmDiagnostico)
            throw new DomainException($"Não é possível alterar itens de uma ordem de serviço no status '{Status}'.");
    }

    private void GarantirTransicao(StatusOS statusEsperado, string acao)
    {
        if (Status != statusEsperado)
            throw new DomainException(
                $"Não é possível '{acao}': a ordem de serviço está no status '{Status}', esperado '{statusEsperado}'.");
    }
}
