using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.OrdensServico;

namespace OficinaDigital.Domain.Catalogo;

public sealed class Peca : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    public Money Preco { get; private set; } = null!;
    public int QuantidadeEmEstoque { get; private set; }

    private Peca()
    {
    }

    private Peca(string nome, Money preco, int quantidadeEmEstoque)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Preco = preco;
        QuantidadeEmEstoque = quantidadeEmEstoque;
    }

    public static Peca Criar(string nome, decimal preco, int quantidadeEmEstoque = 0)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome da peça é obrigatório.");
        if (quantidadeEmEstoque < 0)
            throw new DomainException("Quantidade em estoque não pode ser negativa.");

        return new Peca(nome.Trim(), Money.Criar(preco), quantidadeEmEstoque);
    }

    public void AtualizarDados(string nome, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome da peça é obrigatório.");

        Nome = nome.Trim();
        Preco = Money.Criar(preco);
    }

    public void BaixarEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade a baixar deve ser maior que zero.");
        if (quantidade > QuantidadeEmEstoque)
            throw new DomainException(
                $"Estoque insuficiente para a peça '{Nome}'. Disponível: {QuantidadeEmEstoque}, solicitado: {quantidade}.");

        QuantidadeEmEstoque -= quantidade;
    }

    public void ReporEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade a repor deve ser maior que zero.");

        QuantidadeEmEstoque += quantidade;
    }
}
