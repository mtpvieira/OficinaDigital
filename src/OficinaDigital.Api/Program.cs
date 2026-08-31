using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OficinaDigital.Api.Configuracao;
using OficinaDigital.Application;
using OficinaDigital.Application.Common;
using OficinaDigital.Infrastructure;
using OficinaDigital.Infrastructure.Persistencia;
using OficinaDigital.Infrastructure.Seguranca;

// A imagem de runtime não traz curl nem wget: o próprio executável responde pelo HEALTHCHECK.
if (args.Contains("--healthcheck"))
    return await VerificacaoDeSaude.ExecutarAsync();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

builder.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opcoes.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        opcoes.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

builder.Services.AddSwaggerDaOficina();

var jwt = builder.Configuration.GetSection(JwtOptions.Secao).Get<JwtOptions>() ?? new JwtOptions();
jwt.Validar();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opcoes.SaveToken = false;

        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Emissor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveSecreta)),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opcoes =>
{
    opcoes.AddPolicy(Politicas.Interno, p => p.RequireRole(
        PerfilAcesso.Atendente, PerfilAcesso.Mecanico, PerfilAcesso.Administrador));

    opcoes.AddPolicy(Politicas.Atendente, p => p.RequireRole(
        PerfilAcesso.Atendente, PerfilAcesso.Administrador));

    opcoes.AddPolicy(Politicas.Mecanico, p => p.RequireRole(
        PerfilAcesso.Mecanico, PerfilAcesso.Administrador));

    opcoes.AddPolicy(Politicas.Administrador, p => p.RequireRole(PerfilAcesso.Administrador));

    opcoes.AddPolicy(Politicas.Cliente, p => p.RequireRole(PerfilAcesso.Cliente));
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OficinaDbContext>("banco-de-dados");

var app = builder.Build();

app.UseTratamentoDeExcecoes();
app.UseCabecalhosDeSeguranca();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Oficina Digital v1");
        o.DocumentTitle = "Oficina Digital — API";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

if (app.Configuration.GetValue("AplicarMigrationsNaInicializacao", true))
    await SemeadorDeDados.PrepararBancoAsync(app.Services);

app.Run();
return 0;

public static class Politicas
{
    public const string Interno = "Interno";
    public const string Atendente = "Atendente";
    public const string Mecanico = "Mecanico";
    public const string Administrador = "Administrador";
    public const string Cliente = "Cliente";
}

public partial class Program;
