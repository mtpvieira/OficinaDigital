using FluentValidation;

namespace OficinaDigital.Application.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Usuario).NotEmpty();
        RuleFor(x => x.Senha).NotEmpty();
    }
}
