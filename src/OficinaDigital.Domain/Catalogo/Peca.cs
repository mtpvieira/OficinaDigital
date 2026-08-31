using OficinaDigital.Domain.Catalogo.Events;
using OficinaDigital.Domain.Catalogo.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Catalogo;

public class Peca : AggregateRoot<PecaId>
{
    private readonly List<PrecoVigente> _precos = [];

    public Sku Sku { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public UnidadeDeMedida Unidade { get; private set; }
    public IReadOnlyCollection<PrecoVigente> Precos => _precos.AsReadOnly();
    public bool Ativa { get; private set; }

    protected Peca() { }

    private Peca(PecaId id, Sku sku, string descricao, UnidadeDeMedida unidade, PrecoVigente preco)
    {
        Id = id;
        Sku = sku;
        Descricao = descricao;
        Unidade = unidade;
        _precos.Add(preco);
        Ativa = true;
    }

    public static Peca Cadastrar(string? sku, string? descricao, UnidadeDeMedida unidade, Dinheiro precoVenda,
        decimal estoqueMinimo, DateOnly? vigenteDe = null)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(descricao), "Descrição da peça é obrigatória.");
        DomainException.Se(estoqueMinimo < 0, "Estoque mínimo não pode ser negativo.");

        var peca = new Peca(
            PecaId.Novo(),
            Sku.Criar(sku),
            descricao!.Trim(),
            unidade,
            PrecoVigente.De(precoVenda, vigenteDe ?? DateOnly.FromDateTime(DateTime.UtcNow)));

        peca.Publicar(new PecaCadastrada(peca.Id, peca.Sku.Codigo, unidade, estoqueMinimo));
        return peca;
    }

    public void AlterarDados(string? descricao)
    {
        DomainException.Se(!Ativa, "Peça inativa não pode ser alterada.");
        DomainException.Se(string.IsNullOrWhiteSpace(descricao), "Descrição da peça é obrigatória.");

        Descricao = descricao!.Trim();
    }

    public void AlterarPreco(Dinheiro novoValor, DateOnly vigenteDe)
    {
        DomainException.Se(!Ativa, "Peça inativa não pode ter preço alterado.");

        var precoNaMesmaData = _precos.FirstOrDefault(p => p.VigenteDe == vigenteDe);
        if (precoNaMesmaData is not null) _precos.Remove(precoNaMesmaData);

        _precos.Add(PrecoVigente.De(novoValor, vigenteDe));
        Publicar(new PrecoDaPecaAlterado(Id, novoValor.Valor, vigenteDe));
    }

    public void Inativar()
    {
        if (!Ativa) return;

        Ativa = false;
        Publicar(new PecaInativada(Id));
    }

    public Dinheiro PrecoVigenteEm(DateOnly data)
    {
        var preco = _precos
            .Where(p => p.VigenteDe <= data)
            .OrderByDescending(p => p.VigenteDe)
            .FirstOrDefault();

        DomainException.Se(preco is null, $"Peça {Sku} não possui preço vigente em {data:dd/MM/yyyy}.");
        return preco!.Valor;
    }
}
