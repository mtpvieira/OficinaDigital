using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.OrdensServico;

namespace OficinaDigital.Domain.Catalogo;

public sealed class Servico : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public Money PrecoBase { get; private set; } = null!;
    public int? TempoEstimadoMinutos { get; private set; }

    private Servico()
    {
    }

    private Servico(string nome, string? descricao, Money precoBase, int? tempoEstimadoMinutos)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Descricao = descricao;
        PrecoBase = precoBase;
        TempoEstimadoMinutos = tempoEstimadoMinutos;
    }

    public static Servico Criar(string nome, decimal precoBase, string? descricao = null, int? tempoEstimadoMinutos = null)
    {
        ValidarDados(nome, tempoEstimadoMinutos);
        return new Servico(nome.Trim(), descricao, Money.Criar(precoBase), tempoEstimadoMinutos);
    }

    public void AtualizarDados(string nome, decimal precoBase, string? descricao, int? tempoEstimadoMinutos)
    {
        ValidarDados(nome, tempoEstimadoMinutos);
        Nome = nome.Trim();
        Descricao = descricao;
        PrecoBase = Money.Criar(precoBase);
        TempoEstimadoMinutos = tempoEstimadoMinutos;
    }

    private static void ValidarDados(string nome, int? tempoEstimadoMinutos)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do serviço é obrigatório.");
        if (tempoEstimadoMinutos is < 0)
            throw new DomainException("Tempo estimado não pode ser negativo.");
    }
}
