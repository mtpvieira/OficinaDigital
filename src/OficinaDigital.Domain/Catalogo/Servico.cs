using OficinaDigital.Domain.Catalogo.Events;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Catalogo;

public class Servico : AggregateRoot<ServicoId>
{
    private readonly List<PrecoVigente> _precos = [];

    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public IReadOnlyCollection<PrecoVigente> Precos => _precos.AsReadOnly();
    public Duracao TempoPadraoExecucao { get; private set; } = null!;
    public bool Ativo { get; private set; }

    protected Servico() { }

    private Servico(ServicoId id, string nome, string? descricao, PrecoVigente preco, Duracao tempoPadrao)
    {
        Id = id;
        Nome = nome;
        Descricao = descricao;
        _precos.Add(preco);
        TempoPadraoExecucao = tempoPadrao;
        Ativo = true;
    }

    public static Servico Cadastrar(string? nome, string? descricao, Dinheiro preco, int tempoPadraoMinutos,
        DateOnly? vigenteDe = null)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(nome), "Nome do serviço é obrigatório.");

        var servico = new Servico(
            ServicoId.Novo(),
            nome!.Trim(),
            descricao?.Trim(),
            PrecoVigente.De(preco, vigenteDe ?? DateOnly.FromDateTime(DateTime.UtcNow)),
            Duracao.DeMinutos(tempoPadraoMinutos));

        servico.Publicar(new ServicoCadastradoNoCatalogo(servico.Id, servico.Nome));
        return servico;
    }

    public void AlterarDados(string? nome, string? descricao, int tempoPadraoMinutos)
    {
        DomainException.Se(!Ativo, "Serviço inativo não pode ser alterado.");
        DomainException.Se(string.IsNullOrWhiteSpace(nome), "Nome do serviço é obrigatório.");

        Nome = nome!.Trim();
        Descricao = descricao?.Trim();
        TempoPadraoExecucao = Duracao.DeMinutos(tempoPadraoMinutos);
    }

    public void AlterarPreco(Dinheiro novoValor, DateOnly vigenteDe)
    {
        DomainException.Se(!Ativo, "Serviço inativo não pode ter preço alterado.");

        var precoNaMesmaData = _precos.FirstOrDefault(p => p.VigenteDe == vigenteDe);
        if (precoNaMesmaData is not null) _precos.Remove(precoNaMesmaData);

        _precos.Add(PrecoVigente.De(novoValor, vigenteDe));
        Publicar(new PrecoDoServicoAlterado(Id, novoValor.Valor, vigenteDe));
    }

    public void Inativar()
    {
        if (!Ativo) return;

        Ativo = false;
        Publicar(new ServicoInativado(Id));
    }

    public Dinheiro PrecoVigenteEm(DateOnly data)
    {
        var preco = _precos
            .Where(p => p.VigenteDe <= data)
            .OrderByDescending(p => p.VigenteDe)
            .FirstOrDefault();

        DomainException.Se(preco is null, $"Serviço {Nome} não possui preço vigente em {data:dd/MM/yyyy}.");
        return preco!.Valor;
    }
}
