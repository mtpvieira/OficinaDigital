using FluentValidation;

namespace OficinaDigital.Application.Catalogo;

public sealed class CriarPecaRequestValidator : AbstractValidator<CriarPecaRequest>
{
    public CriarPecaRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Preco).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuantidadeEmEstoque).GreaterThanOrEqualTo(0);
    }
}

public sealed class AtualizarPecaRequestValidator : AbstractValidator<AtualizarPecaRequest>
{
    public AtualizarPecaRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Preco).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReporEstoqueRequestValidator : AbstractValidator<ReporEstoqueRequest>
{
    public ReporEstoqueRequestValidator()
    {
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
