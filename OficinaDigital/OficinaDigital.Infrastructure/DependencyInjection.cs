using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OficinaDigital.Application.Auth;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Clientes;
using OficinaDigital.Domain.OrdensServico;
using OficinaDigital.Domain.Veiculos;
using OficinaDigital.Infrastructure.Auth;
using OficinaDigital.Infrastructure.Persistence;
using OficinaDigital.Infrastructure.Persistence.Repositories;

namespace OficinaDigital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useInMemory = true)
    {
        services.AddDbContext<OficinaDigitalDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase("oficinadigital");
            }
            else
            {
                throw new NotSupportedException("Persistência relacional ainda não configurada.");
            }
        });

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IPecaRepository, PecaRepository>();
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddJwtAuthentication(configuration);

        services.Configure<AdminCredentialsOptions>(configuration.GetSection(AdminCredentialsOptions.SectionName));
        services.AddScoped<IAdminAuthenticator, AdminAuthenticator>();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.ChaveSecreta)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }
}
