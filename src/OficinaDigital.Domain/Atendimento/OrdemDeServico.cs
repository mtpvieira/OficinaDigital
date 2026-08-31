using OficinaDigital.Domain.Atendimento.Events;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Atendimento;

public class OrdemDeServico : AggregateRoot<OrdemDeServicoId>
{
    private readonly List<ItemDeServico> _itensDeServico = [];
    private readonly List<ItemDePeca> _itensDePeca = [];

    public long Numero { get; private set; }

    public ClienteId ClienteId { get; private set; }
    public VeiculoId VeiculoId { get; private set; }
    public StatusOS Status { get; private set; }
    public MotivoCancelamento? MotivoCancelamento { get; private set; }
    public string? ObservacaoCancelamento { get; private set; }
    public string? DescricaoDoProblema { get; private set; }
    public JanelaDeExecucao Execucao { get; private set; } = null!;
    public OrcamentoId? OrcamentoVigenteId { get; private set; }
    public bool OrcamentoAprovado { get; private set; }
    public Dinheiro Total { get; private set; } = null!;
    public DateTime AbertaEm { get; private set; }

    public IReadOnlyCollection<ItemDeServico> ItensDeServico => _itensDeServico.AsReadOnly();
    public IReadOnlyCollection<ItemDePeca> ItensDePeca => _itensDePeca.AsReadOnly();

    public bool EstaAtiva => Status is not (StatusOS.ENTREGUE or StatusOS.CANCELADA);

    public bool PossuiPecaPendenteDeCompra =>
        _itensDePeca.Any(i => i.Status == StatusItemPeca.PENDENTE_COMPRA);

    public bool PossuiItemAdicionalPendente =>
        _itensDeServico.Any(i => i.AguardandoAprovacaoDoCliente) ||
        _itensDePeca.Any(i => i.AguardandoAprovacaoDoCliente);

    protected OrdemDeServico() { }

    private OrdemDeServico(OrdemDeServicoId id, ClienteId clienteId, VeiculoId veiculoId, string? descricaoDoProblema)
    {
        Id = id;
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        DescricaoDoProblema = descricaoDoProblema?.Trim();
        Status = StatusOS.RECEBIDA;
        Execucao = JanelaDeExecucao.NaoIniciada();
        Total = Dinheiro.Zero;
        AbertaEm = DateTime.UtcNow;
    }

    public static OrdemDeServico Abrir(ClienteId clienteId, VeiculoId veiculoId, string? descricaoDoProblema = null)
    {
        DomainException.Se(clienteId.Valor == Guid.Empty, "A OS exige um cliente identificado.");
        DomainException.Se(veiculoId.Valor == Guid.Empty, "A OS exige um veículo identificado.");

        var os = new OrdemDeServico(OrdemDeServicoId.Novo(), clienteId, veiculoId, descricaoDoProblema);
        os.Publicar(new OsAberta(os.Id, clienteId, veiculoId));
        return os;
    }

    public void IniciarDiagnostico()
    {
        TransicionarPara(StatusOS.EM_DIAGNOSTICO);
        Publicar(new DiagnosticoIniciado(Id));
    }

    public void RegistrarServicos(IEnumerable<(ServicoId ServicoId, string Descricao, Dinheiro Preco, int TempoPadraoMinutos)> servicos)
    {
        GarantirStatus(StatusOS.EM_DIAGNOSTICO,
            "Serviços só podem ser registrados durante o diagnóstico.");

        var lista = servicos.ToList();
        DomainException.Se(lista.Count == 0, "Informe ao menos um serviço.");

        foreach (var (servicoId, descricao, preco, tempo) in lista)
        {
            DomainException.Se(_itensDeServico.Any(i => i.ServicoId == servicoId),
                $"O serviço {descricao} já está registrado nesta OS.");

            _itensDeServico.Add(new ItemDeServico(servicoId, descricao, preco, tempo, OrigemItem.DIAGNOSTICO));
        }

        RecalcularTotal();
        Publicar(new ServicosDaOsRegistrados(Id, lista.Count));
    }

    public void RemoverServico(Guid itemId)
    {
        GarantirStatus(StatusOS.EM_DIAGNOSTICO, "Serviços só podem ser removidos durante o diagnóstico.");

        var item = _itensDeServico.FirstOrDefault(i => i.Id == itemId);
        DomainException.Se(item is null, "Item de serviço não encontrado nesta OS.");

        _itensDeServico.Remove(item!);
        RecalcularTotal();
    }

    public void RegistrarPecas(IEnumerable<(PecaId PecaId, string Descricao, decimal Quantidade, Dinheiro Preco)> pecas)
    {
        GarantirStatus(StatusOS.EM_DIAGNOSTICO, "Peças só podem ser registradas durante o diagnóstico.");

        var lista = pecas.ToList();
        DomainException.Se(lista.Count == 0, "Informe ao menos uma peça.");

        foreach (var (pecaId, descricao, quantidade, preco) in lista)
        {
            DomainException.Se(_itensDePeca.Any(i => i.PecaId == pecaId),
                $"A peça {descricao} já está registrada nesta OS. Ajuste a quantidade do item existente.");

            _itensDePeca.Add(new ItemDePeca(pecaId, descricao, quantidade, preco, OrigemItem.DIAGNOSTICO));
        }

        RecalcularTotal();
        Publicar(new PecasDaOsRegistradas(Id, lista.Select(p => (p.PecaId, p.Quantidade)).ToList()));
    }

    public void RemoverPeca(Guid itemId)
    {
        GarantirStatus(StatusOS.EM_DIAGNOSTICO, "Peças só podem ser removidas durante o diagnóstico.");

        var item = _itensDePeca.FirstOrDefault(i => i.Id == itemId);
        DomainException.Se(item is null, "Item de peça não encontrado nesta OS.");
        DomainException.Se(item!.Status == StatusItemPeca.SUBTRAIDA,
            "Peça já subtraída do estoque não pode ser removida da OS. Use a devolução ao estoque.");

        _itensDePeca.Remove(item);
        RecalcularTotal();
    }

    public void ConfirmarReservaDaPeca(PecaId pecaId)
    {
        var item = _itensDePeca.FirstOrDefault(i => i.PecaId == pecaId);
        item?.MarcarComoReservada();
    }

    public void MarcarPecaComoPendenteDeCompra(PecaId pecaId)
    {
        var item = _itensDePeca.FirstOrDefault(i => i.PecaId == pecaId);
        item?.MarcarComoPendenteDeCompra();
    }

    public void FinalizarDiagnostico()
    {
        GarantirStatus(StatusOS.EM_DIAGNOSTICO, "Só é possível finalizar um diagnóstico em andamento.");

        DomainException.Se(_itensDeServico.Count == 0,
            "O diagnóstico só fecha com ao menos um item de serviço registrado.");
        DomainException.Se(_itensDePeca.Any(i => i.Status == StatusItemPeca.AGUARDANDO_RESERVA),
            "Há peças aguardando reserva. O diagnóstico só fecha com todas reservadas ou marcadas como pendentes de compra.");

        Publicar(new DiagnosticoFinalizado(Id, PossuiPecaPendenteDeCompra));
    }

    public void VincularOrcamento(OrcamentoId orcamentoId)
    {
        OrcamentoVigenteId = orcamentoId;
        OrcamentoAprovado = false;
    }

    public void MarcarOrcamentoEnviado()
    {
        DomainException.Se(OrcamentoVigenteId is null, "Não há orçamento vinculado a esta OS.");
        TransicionarPara(StatusOS.AGUARDANDO_APROVACAO);
    }

    public void RegistrarAprovacaoDoOrcamento()
    {
        GarantirStatus(StatusOS.AGUARDANDO_APROVACAO, "A OS não está aguardando aprovação de orçamento.");
        OrcamentoAprovado = true;
    }

    public void IniciarServico()
    {
        GarantirStatus(StatusOS.AGUARDANDO_APROVACAO, "O serviço só inicia a partir de AGUARDANDO_APROVACAO.");
        DomainException.Se(!OrcamentoAprovado, "O serviço só inicia com o orçamento aprovado pelo cliente.");
        DomainException.Se(PossuiPecaPendenteDeCompra,
            "Há peças pendentes de compra. Registre a entrada no estoque antes de iniciar o serviço.");

        Execucao = Execucao.Iniciar();
        TransicionarPara(StatusOS.EM_EXECUCAO);
        Publicar(new ServicoIniciado(Id, Execucao.Inicio!.Value));
    }

    public void ConfirmarSubtracaoDasPecas()
    {
        foreach (var item in _itensDePeca.Where(i => i.Status == StatusItemPeca.RESERVADA))
            item.MarcarComoSubtraida();
    }

    public void RegistrarProblemaAdicional(string? descricao)
    {
        GarantirStatus(StatusOS.EM_EXECUCAO, "Problemas adicionais só são registrados durante a execução.");
        DomainException.Se(string.IsNullOrWhiteSpace(descricao), "Descreva o problema adicional encontrado.");

        Publicar(new ProblemaAdicionalRegistrado(Id, descricao!.Trim()));
    }

    public void RegistrarItensAdicionais(
        IEnumerable<(ServicoId ServicoId, string Descricao, Dinheiro Preco, int TempoPadraoMinutos)> servicos,
        IEnumerable<(PecaId PecaId, string Descricao, decimal Quantidade, Dinheiro Preco)> pecas)
    {
        GarantirStatus(StatusOS.EM_EXECUCAO, "Itens adicionais só são registrados durante a execução.");

        var listaServicos = servicos.ToList();
        var listaPecas = pecas.ToList();
        DomainException.Se(listaServicos.Count == 0 && listaPecas.Count == 0,
            "Informe ao menos um serviço ou peça adicional.");

        foreach (var (servicoId, descricao, preco, tempo) in listaServicos)
            _itensDeServico.Add(new ItemDeServico(servicoId, descricao, preco, tempo, OrigemItem.ADICIONAL));

        foreach (var (pecaId, descricao, quantidade, preco) in listaPecas)
            _itensDePeca.Add(new ItemDePeca(pecaId, descricao, quantidade, preco, OrigemItem.ADICIONAL));

        Publicar(new ServicosEPecasAdicionaisRegistrados(Id));
    }

    public void SuspenderParaAprovacaoComplementar()
    {
        GarantirStatus(StatusOS.EM_EXECUCAO, "Só a execução em andamento pode ser suspensa.");

        Execucao = Execucao.Suspender();
        TransicionarPara(StatusOS.AGUARDANDO_APROVACAO);
    }

    public void AprovarItensAdicionais()
    {
        GarantirStatus(StatusOS.AGUARDANDO_APROVACAO, "A OS não está aguardando aprovação.");

        foreach (var item in _itensDeServico.Where(i => i.AguardandoAprovacaoDoCliente)) item.Aprovar();
        foreach (var item in _itensDePeca.Where(i => i.AguardandoAprovacaoDoCliente)) item.Aprovar();

        RecalcularTotal();
        Execucao = Execucao.Retomar();
        TransicionarPara(StatusOS.EM_EXECUCAO);
    }

    public void RemoverItensAdicionais()
    {
        GarantirStatus(StatusOS.AGUARDANDO_APROVACAO, "A OS não está aguardando aprovação.");

        _itensDeServico.RemoveAll(i => i.AguardandoAprovacaoDoCliente);
        _itensDePeca.RemoveAll(i => i.AguardandoAprovacaoDoCliente);

        RecalcularTotal();
        Execucao = Execucao.Retomar();
        TransicionarPara(StatusOS.EM_EXECUCAO);
        Publicar(new ItensAdicionaisRemovidosDaOs(Id));
    }

    public void FinalizarServico()
    {
        GarantirStatus(StatusOS.EM_EXECUCAO, "Só é possível concluir um serviço em execução.");
        DomainException.Se(PossuiItemAdicionalPendente,
            "A OS não conclui com item adicional pendente de resposta do cliente.");

        Execucao = Execucao.Concluir();
        TransicionarPara(StatusOS.FINALIZADA);

        Publicar(new ServicoConcluido(Id, Execucao.Fim!.Value));
        Publicar(new DuracaoRealDaExecucaoRegistrada(Id, Execucao.DuracaoRealEmMinutos!.Value));
    }

    public void RegistrarEntrega()
    {
        GarantirStatus(StatusOS.FINALIZADA, "Só uma OS finalizada pode ter o veículo entregue.");

        TransicionarPara(StatusOS.ENTREGUE);
        Publicar(new VeiculoEntregue(Id, ClienteId, VeiculoId));
    }

    public void Cancelar(MotivoCancelamento motivo, string? observacao = null)
    {
        DomainException.Se(Status is StatusOS.FINALIZADA or StatusOS.ENTREGUE,
            "A OS não pode ser cancelada depois do serviço concluído.");
        DomainException.Se(Status == StatusOS.CANCELADA, "Esta OS já está cancelada.");

        MotivoCancelamento = motivo;
        ObservacaoCancelamento = observacao?.Trim();
        TransicionarPara(StatusOS.CANCELADA);

        Publicar(new OsCancelada(Id, motivo, ObservacaoCancelamento));
    }

    private static readonly Dictionary<StatusOS, StatusOS[]> TransicoesPermitidas = new()
    {
        [StatusOS.RECEBIDA] = [StatusOS.EM_DIAGNOSTICO, StatusOS.CANCELADA],
        [StatusOS.EM_DIAGNOSTICO] = [StatusOS.AGUARDANDO_APROVACAO, StatusOS.CANCELADA],
        [StatusOS.AGUARDANDO_APROVACAO] = [StatusOS.EM_EXECUCAO, StatusOS.CANCELADA],
        [StatusOS.EM_EXECUCAO] = [StatusOS.AGUARDANDO_APROVACAO, StatusOS.FINALIZADA, StatusOS.CANCELADA],
        [StatusOS.FINALIZADA] = [StatusOS.ENTREGUE],
        [StatusOS.ENTREGUE] = [],
        [StatusOS.CANCELADA] = []
    };

    private void TransicionarPara(StatusOS novoStatus)
    {
        var permitidas = TransicoesPermitidas[Status];

        DomainException.Se(!permitidas.Contains(novoStatus),
            $"Transição de status inválida: {Status} para {novoStatus}.");

        var anterior = Status;
        Status = novoStatus;
        Publicar(new StatusDaOsAlterado(Id, anterior, novoStatus));
    }

    private void GarantirStatus(StatusOS esperado, string mensagem) =>
        DomainException.Se(Status != esperado, $"{mensagem} Status atual: {Status}.");

    private void RecalcularTotal()
    {
        var servicos = _itensDeServico
            .Where(i => !i.AguardandoAprovacaoDoCliente)
            .Aggregate(Dinheiro.Zero, (acc, i) => acc.Somar(i.Subtotal));

        var pecas = _itensDePeca
            .Where(i => !i.AguardandoAprovacaoDoCliente)
            .Aggregate(Dinheiro.Zero, (acc, i) => acc.Somar(i.Subtotal));

        Total = servicos.Somar(pecas);
    }
}
