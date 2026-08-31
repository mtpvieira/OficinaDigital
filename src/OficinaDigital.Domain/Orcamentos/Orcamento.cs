using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Orcamentos.Events;
using OficinaDigital.Domain.Orcamentos.ValueObjects;

namespace OficinaDigital.Domain.Orcamentos;

public class Orcamento : AggregateRoot<OrcamentoId>
{
    private readonly List<VersaoOrcamento> _versoes = [];

    public OrdemDeServicoId OrdemDeServicoId { get; private set; }
    public TipoOrcamento Tipo { get; private set; }
    public int VersaoVigente { get; private set; }
    public IReadOnlyCollection<VersaoOrcamento> Versoes => _versoes.AsReadOnly();

    protected Orcamento() { }

    private Orcamento(OrcamentoId id, OrdemDeServicoId ordemDeServicoId, TipoOrcamento tipo)
    {
        Id = id;
        OrdemDeServicoId = ordemDeServicoId;
        Tipo = tipo;
    }

    public static Orcamento Gerar(OrdemDeServicoId ordemDeServicoId, TipoOrcamento tipo,
        IEnumerable<(TipoItemOrcado Tipo, Guid ReferenciaId, string Descricao, decimal Quantidade, Dinheiro PrecoUnitario)> itens)
    {
        var orcamento = new Orcamento(OrcamentoId.Novo(), ordemDeServicoId, tipo);
        var versao = orcamento.CriarVersao(itens);

        orcamento.Publicar(new OrcamentoGerado(orcamento.Id, ordemDeServicoId, tipo, versao.Numero,
            versao.Total.Valor));

        return orcamento;
    }

    public void Alterar(
        IEnumerable<(TipoItemOrcado Tipo, Guid ReferenciaId, string Descricao, decimal Quantidade, Dinheiro PrecoUnitario)> itens)
    {
        var vigente = ObterVersaoVigente();

        DomainException.Se(vigente.FoiRespondida,
            "A versão vigente já foi respondida pelo cliente e não pode ser alterada. Gere um orçamento complementar.");

        var nova = CriarVersao(itens);
        Publicar(new OrcamentoAlterado(Id, OrdemDeServicoId, nova.Numero));
    }

    public void Enviar()
    {
        var vigente = ObterVersaoVigente();

        DomainException.Se(vigente.FoiRespondida, "Esta versão do orçamento já foi respondida.");

        vigente.MarcarComoEnviada();
        Publicar(new OrcamentoEnviadoAoCliente(Id, OrdemDeServicoId, Tipo, vigente.Numero));
    }

    public void Responder(DecisaoCliente decisao)
    {
        var vigente = ObterVersaoVigente();
        vigente.Responder(decisao);

        if (decisao == DecisaoCliente.APROVADO)
            Publicar(new OrcamentoAprovado(Id, OrdemDeServicoId, Tipo, vigente.Numero));
        else
            Publicar(new OrcamentoReprovado(Id, OrdemDeServicoId, Tipo, vigente.Numero));
    }

    public VersaoOrcamento ObterVersaoVigente()
    {
        var versao = _versoes.FirstOrDefault(v => v.Numero == VersaoVigente);
        DomainException.Se(versao is null, "Orçamento sem versão vigente.");
        return versao!;
    }

    public bool EstaAprovadoEVigente()
    {
        var vigente = ObterVersaoVigente();
        return vigente.Resposta?.Aprovou == true;
    }

    private VersaoOrcamento CriarVersao(
        IEnumerable<(TipoItemOrcado Tipo, Guid ReferenciaId, string Descricao, decimal Quantidade, Dinheiro PrecoUnitario)> itens)
    {
        var itensOrcados = itens
            .Select(i => new ItemOrcado(i.Tipo, i.ReferenciaId, i.Descricao, i.Quantidade, i.PrecoUnitario))
            .ToList();

        var numero = _versoes.Count + 1;
        var versao = new VersaoOrcamento(numero, itensOrcados);

        _versoes.Add(versao);
        VersaoVigente = numero;

        return versao;
    }
}
