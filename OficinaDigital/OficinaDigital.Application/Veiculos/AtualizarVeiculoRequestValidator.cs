using FluentValidation;

namespace OficinaDigital.Application.Veiculos;

public sealed class AtualizarVeiculoRequestValidator : AbstractValidator<AtualizarVeiculoRequest>
{
    public AtualizarVeiculoRequestValidator()
    {
        RuleFor(x => x.Marca).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Modelo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ano).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}
