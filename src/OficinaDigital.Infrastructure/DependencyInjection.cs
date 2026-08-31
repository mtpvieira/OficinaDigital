using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Seguranca;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Cadastro.Repositories;
using OficinaDigital.Domain.Catalogo.Repositories;
using OficinaDigital.Domain.Estoque.Repositories;
using OficinaDigital.Domain.Identidade.Repositories;
using OficinaDigital.Domain.Orcamentos.Repositories;
using OficinaDigital.Infrastructure.Persistencia;
using OficinaDigital.Infrastructure.Persistencia.Repositorios;
using OficinaDigital.Infrastructure.Seguranca;

namespace OficinaDigital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("SqlServer")
                      ?? throw new InvalidOperationException(
                          "ConnectionStrings:SqlServer não configurada.");

        services.AddDbContext<OficinaDbContext>(opcoes =>
            opcoes.UseSqlServer(conexao, sql =>
            {
                sql.MigrationsAssembly(typeof(OficinaDbContext).Assembly.FullName);

                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

        return services
            .AddRepositorios()
            .AddSeguranca(configuration)
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IDomainEventDispatcher, DespachanteDeEventos>();
    }

    private static IServiceCollection AddRepositorios(this IServiceCollection services) => services
        .AddScoped<IClienteRepository, ClienteRepository>()
        .AddScoped<IVeiculoRepository, VeiculoRepository>()
        .AddScoped<IServicoRepository, ServicoRepository>()
        .AddScoped<IPecaRepository, PecaRepository>()
        .AddScoped<IItemEstoqueRepository, ItemEstoqueRepository>()
        .AddScoped<IOrdemDeServicoRepository, OrdemDeServicoRepository>()
        .AddScoped<IOrcamentoRepository, OrcamentoRepository>()
        .AddScoped<IUsuarioRepository, UsuarioRepository>();

    private static IServiceCollection AddSeguranca(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Secao));

        return services
            .AddSingleton<IPasswordHasher, PasswordHasher>()
            .AddScoped<ITokenService, TokenService>();
    }
}
