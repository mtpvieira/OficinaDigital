using FluentValidation;

namespace OficinaDigital.Application.OrdensServico;

public sealed class AprovarPublicoRequestValidator : AbstractValidator<AprovarPublicoRequest>
{
    public AprovarPublicoRequestValidator()
    {
        RuleFor(x => x.Documento).NotEmpty();
    }
}
