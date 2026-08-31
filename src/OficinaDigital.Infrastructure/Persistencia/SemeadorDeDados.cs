using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OficinaDigital.Application.Seguranca;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Estoque;
using OficinaDigital.Domain.Identidade;

namespace OficinaDigital.Infrastructure.Persistencia;

public static class SemeadorDeDados
{
    public static async Task PrepararBancoAsync(IServiceProvider provedor, CancellationToken ct = default)
    {
        using var escopo = provedor.CreateScope();

        var servicos = escopo.ServiceProvider;
        var contexto = servicos.GetRequiredService<OficinaDbContext>();
        var logger = servicos.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SemeadorDeDados));

        await contexto.Database.MigrateAsync(ct);
        logger.LogInformation("Migrations aplicadas.");

        await SemearUsuarioAdministradorAsync(contexto, servicos, logger, ct);
        await SemearCatalogoDeExemploAsync(contexto, logger, ct);
    }

    private static async Task SemearUsuarioAdministradorAsync(OficinaDbContext contexto,
        IServiceProvider servicos, ILogger logger, CancellationToken ct)
    {
        if (await contexto.Usuarios.AnyAsync(ct)) return;

        var configuracao = servicos.GetRequiredService<IConfiguration>();
        var email = configuracao["Seed:EmailAdministrador"] ?? "admin@oficinadigital.local";
        var senha = configuracao["Seed:SenhaAdministrador"];

        if (string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning(
                "Nenhum usuário cadastrado e Seed:SenhaAdministrador não configurada. " +
                "Defina a variável de ambiente Seed__SenhaAdministrador para criar o administrador inicial.");
            return;
        }

        var hasher = servicos.GetRequiredService<IPasswordHasher>();
        var (hash, salt) = hasher.Gerar(senha);

        contexto.Usuarios.Add(Usuario.Criar("Administrador", email, hash, salt, PerfilUsuario.ADMINISTRADOR));
        await contexto.SaveChangesAsync(ct);

        logger.LogInformation("Usuário administrador inicial criado: {Email}.", email);
    }

    private static async Task SemearCatalogoDeExemploAsync(OficinaDbContext contexto, ILogger logger,
        CancellationToken ct)
    {
        if (await contexto.Servicos.AnyAsync(ct) || await contexto.Pecas.AnyAsync(ct)) return;

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        contexto.Servicos.AddRange(
            Servico.Cadastrar("Troca de óleo", "Troca de óleo do motor e do filtro",
                Dinheiro.De(180m), 45, hoje),
            Servico.Cadastrar("Alinhamento e balanceamento", "Alinhamento da direção e balanceamento das rodas",
                Dinheiro.De(150m), 60, hoje),
            Servico.Cadastrar("Revisão de freios", "Inspeção e substituição de pastilhas e discos",
                Dinheiro.De(320m), 120, hoje),
            Servico.Cadastrar("Troca de correia dentada", "Substituição da correia dentada e tensor",
                Dinheiro.De(890m), 240, hoje));

        var pecas = new[]
        {
            Peca.Cadastrar("OLEO-5W30", "Óleo sintético 5W30", UnidadeDeMedida.L, Dinheiro.De(58m), 20m, hoje),
            Peca.Cadastrar("FILTRO-OLEO-01", "Filtro de óleo", UnidadeDeMedida.UN, Dinheiro.De(42m), 10m, hoje),
            Peca.Cadastrar("PAST-FREIO-D", "Pastilha de freio dianteira (par)", UnidadeDeMedida.PC,
                Dinheiro.De(215m), 6m, hoje),
            Peca.Cadastrar("CORREIA-DENT-01", "Correia dentada", UnidadeDeMedida.UN, Dinheiro.De(310m), 4m, hoje)
        };

        contexto.Pecas.AddRange(pecas);

        var minimos = new Dictionary<string, decimal>
        {
            ["OLEO-5W30"] = 20m,
            ["FILTRO-OLEO-01"] = 10m,
            ["PAST-FREIO-D"] = 6m,
            ["CORREIA-DENT-01"] = 4m
        };

        foreach (var peca in pecas)
        {
            var item = ItemEstoque.Abrir(peca.Id, peca.Unidade, minimos[peca.Sku.Codigo]);
            item.RegistrarEntrada(minimos[peca.Sku.Codigo] * 3, peca.PrecoVigenteEm(hoje));
            item.LimparEventos();

            contexto.ItensDeEstoque.Add(item);
        }

        await contexto.SaveChangesAsync(ct);
        logger.LogInformation("Catálogo de exemplo criado com serviços, peças e estoque inicial.");
    }
}
