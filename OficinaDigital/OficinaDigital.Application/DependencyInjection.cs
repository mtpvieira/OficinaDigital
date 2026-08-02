using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OficinaDigital.Application.Auth;
using OficinaDigital.Application.Catalogo;
using OficinaDigital.Application.Clientes;
using OficinaDigital.Application.OrdensServico;
using OficinaDigital.Application.Veiculos;

namespace OficinaDigital.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CriarClienteRequestValidator>(includeInternalTypes: true);

        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IVeiculoService, VeiculoService>();
        services.AddScoped<IServicoService, ServicoService>();
        services.AddScoped<IPecaService, PecaService>();
        services.AddScoped<IOrdemServicoService, OrdemServicoService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
