using FluentValidation;

namespace OficinaDigital.Application.Catalogo;

public sealed class CriarServicoRequestValidator : AbstractValidator<CriarServicoRequest>
{
    public CriarServicoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrecoBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.TempoEstimadoMinutos).GreaterThanOrEqualTo(0).When(x => x.TempoEstimadoMinutos.HasValue);
    }
}

public sealed class AtualizarServicoRequestValidator : AbstractValidator<AtualizarServicoRequest>
{
    public AtualizarServicoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrecoBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.TempoEstimadoMinutos).GreaterThanOrEqualTo(0).When(x => x.TempoEstimadoMinutos.HasValue);
    }
}
