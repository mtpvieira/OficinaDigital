namespace OficinaDigital.Application.Catalogo;

public sealed record CriarPecaRequest(string Nome, decimal Preco, int QuantidadeEmEstoque);
