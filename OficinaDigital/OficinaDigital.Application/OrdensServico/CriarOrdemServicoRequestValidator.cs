using FluentValidation;

namespace OficinaDigital.Application.OrdensServico;

public sealed class CriarOrdemServicoRequestValidator : AbstractValidator<CriarOrdemServicoRequest>
{
    public CriarOrdemServicoRequestValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.VeiculoId).NotEmpty();

        RuleFor(x => x)
            .Must(x => (x.Servicos?.Count ?? 0) > 0 || (x.Pecas?.Count ?? 0) > 0)
            .WithMessage("A ordem de serviço deve conter ao menos um serviço ou uma peça.");

        RuleForEach(x => x.Servicos).ChildRules(servico =>
        {
            servico.RuleFor(s => s.ServicoId).NotEmpty();
        });

        RuleForEach(x => x.Pecas).ChildRules(peca =>
        {
            peca.RuleFor(p => p.PecaId).NotEmpty();
            peca.RuleFor(p => p.Quantidade).GreaterThan(0);
        });
    }
}
