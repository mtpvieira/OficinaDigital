using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OficinaDigital.Application.Seguranca;
using OficinaDigital.Domain.Identidade;
using OficinaDigital.Infrastructure.Persistencia;

namespace OficinaDigital.Api.IntegrationTests.Infra;

public class ApiDeTeste : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.UseSetting("ConnectionStrings:SqlServer", "Server=nao-usado;Database=nao-usado;");
        builder.UseSetting("Jwt:ChaveSecreta", "chave-de-teste-com-mais-de-32-caracteres-para-hmac-sha256");
        builder.UseSetting("AplicarMigrationsNaInicializacao", "false");

        builder.ConfigureServices(servicos =>
        {
            RemoverRegistroDoSqlServer(servicos);

            _conexao.Open();
            servicos.AddDbContext<OficinaDbContext>(o => o.UseSqlite(_conexao));
        });
    }

    private static void RemoverRegistroDoSqlServer(IServiceCollection servicos)
    {
        var paraRemover = servicos
            .Where(d =>
                d.ServiceType == typeof(OficinaDbContext) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<OficinaDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")) ||
                d.ServiceType.FullName?.Contains("SqlServer", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var descritor in paraRemover)
            servicos.Remove(descritor);
    }

    // IAsyncLifetime usa Task e WebApplicationFactory usa ValueTask: implementação explícita.
    async Task IAsyncLifetime.InitializeAsync()
    {
        using var escopo = Services.CreateScope();
        var ctx = escopo.ServiceProvider.GetRequiredService<OficinaDbContext>();

        await ctx.Database.EnsureCreatedAsync();
        await CriarUsuariosDeTesteAsync(escopo.ServiceProvider, ctx);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    public override async ValueTask DisposeAsync()
    {
        await _conexao.DisposeAsync();
        await base.DisposeAsync();
    }

    public const string SenhaPadrao = "SenhaForte@123";
    public const string EmailAtendente = "atendente@oficina.local";
    public const string EmailMecanico = "mecanico@oficina.local";
    public const string EmailAdmin = "admin@oficina.local";

    private static async Task CriarUsuariosDeTesteAsync(IServiceProvider servicos, OficinaDbContext ctx)
    {
        if (await ctx.Usuarios.AnyAsync()) return;

        var hasher = servicos.GetRequiredService<IPasswordHasher>();
        var (hash, salt) = hasher.Gerar(SenhaPadrao);

        ctx.Usuarios.AddRange(
            Usuario.Criar("Atendente", EmailAtendente, hash, salt, PerfilUsuario.ATENDENTE),
            Usuario.Criar("Mecânico", EmailMecanico, hash, salt, PerfilUsuario.MECANICO),
            Usuario.Criar("Administrador", EmailAdmin, hash, salt, PerfilUsuario.ADMINISTRADOR));

        await ctx.SaveChangesAsync();
    }

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpClient Anonimo() => CreateClient();

    public async Task<HttpClient> ComoAsync(string email)
    {
        var cliente = CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { email, senha = SenhaPadrao }, Json);

        resposta.EnsureSuccessStatusCode();

        var token = await resposta.Content.ReadFromJsonAsync<TokenResponse>(Json);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        return cliente;
    }

    public Task<HttpClient> ComoAtendenteAsync() => ComoAsync(EmailAtendente);
    public Task<HttpClient> ComoMecanicoAsync() => ComoAsync(EmailMecanico);
    public Task<HttpClient> ComoAdminAsync() => ComoAsync(EmailAdmin);

    public async Task<HttpClient> ComoClienteAsync(string documento, string placa)
    {
        var cliente = CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/api/auth/login-cliente",
            new { documento, placa }, Json);

        resposta.EnsureSuccessStatusCode();

        var token = await resposta.Content.ReadFromJsonAsync<TokenResponse>(Json);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        return cliente;
    }
}
