namespace OficinaDigital.Application.Common;

public sealed record ResultadoPaginado<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    int TotalDeItens)
{
    public int TotalDePaginas => TamanhoPagina == 0 ? 0 : (int)Math.Ceiling(TotalDeItens / (double)TamanhoPagina);
    public bool TemProximaPagina => Pagina < TotalDePaginas;
}

public sealed record Paginacao
{
    private const int TamanhoMaximo = 100;

    public int Pagina { get; init; } = 1;
    public int TamanhoPagina { get; init; } = 20;

    public Paginacao Normalizar() => new()
    {
        Pagina = Pagina < 1 ? 1 : Pagina,
        TamanhoPagina = TamanhoPagina switch
        {
            < 1 => 20,
            > TamanhoMaximo => TamanhoMaximo,
            _ => TamanhoPagina
        }
    };
}
