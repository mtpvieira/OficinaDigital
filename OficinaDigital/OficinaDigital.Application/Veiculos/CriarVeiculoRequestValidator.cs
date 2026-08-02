using FluentValidation;

namespace OficinaDigital.Application.Veiculos;

public sealed class CriarVeiculoRequestValidator : AbstractValidator<CriarVeiculoRequest>
{
    private const string PadraoPlaca = "^[A-Za-z]{3}-?[0-9][0-9A-Za-z][0-9]{2}$";

    public CriarVeiculoRequestValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Placa).NotEmpty().Matches(PadraoPlaca)
            .WithMessage("Placa inválida: formatos aceitos são AAA-9999 (antigo) ou AAA9A99 (Mercosul).");
        RuleFor(x => x.Marca).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Modelo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ano).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}
