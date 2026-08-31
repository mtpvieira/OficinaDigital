using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OficinaDigital.Application;
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

namespace OficinaDigital.Application.Tests.Infra;

public sealed class AmbienteDeTeste : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly ServiceProvider _provedor;

    public IServiceScope Escopo { get; }

    public AmbienteDeTeste()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        var servicos = new ServiceCollection();

        servicos.AddLogging(l => l.SetMinimumLevel(LogLevel.Warning));
        servicos.AddDbContext<OficinaDbContext>(o => o.UseSqlite(_conexao));

        servicos.AddApplication();

        servicos
            .AddScoped<IClienteRepository, ClienteRepository>()
            .AddScoped<IVeiculoRepository, VeiculoRepository>()
            .AddScoped<IServicoRepository, ServicoRepository>()
            .AddScoped<IPecaRepository, PecaRepository>()
            .AddScoped<IItemEstoqueRepository, ItemEstoqueRepository>()
            .AddScoped<IOrdemDeServicoRepository, OrdemDeServicoRepository>()
            .AddScoped<IOrcamentoRepository, OrcamentoRepository>()
            .AddScoped<IUsuarioRepository, UsuarioRepository>()
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IDomainEventDispatcher, DespachanteDeEventos>()
            .AddSingleton<IPasswordHasher, PasswordHasher>()
            .AddScoped<ITokenService, TokenService>();

        servicos.Configure<JwtOptions>(o =>
        {
            o.ChaveSecreta = "chave-de-teste-com-mais-de-32-caracteres-para-hmac-sha256";
            o.ExpiracaoEmMinutos = 15;
        });

        _provedor = servicos.BuildServiceProvider();
        Escopo = _provedor.CreateScope();

        Contexto.Database.EnsureCreated();
    }

    public OficinaDbContext Contexto => Escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();

    public T Servico<T>() where T : notnull => Escopo.ServiceProvider.GetRequiredService<T>();

    public void LimparRastreador() => Contexto.ChangeTracker.Clear();

    public void Dispose()
    {
        Escopo.Dispose();
        _provedor.Dispose();
        _conexao.Dispose();
    }
}
