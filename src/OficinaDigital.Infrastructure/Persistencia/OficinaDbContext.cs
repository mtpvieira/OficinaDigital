using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Atendimento;
using OficinaDigital.Domain.Cadastro;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Estoque;
using OficinaDigital.Domain.Identidade;
using OficinaDigital.Domain.Orcamentos;

namespace OficinaDigital.Infrastructure.Persistencia;

public class OficinaDbContext(DbContextOptions<OficinaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Peca> Pecas => Set<Peca>();

    public DbSet<ItemEstoque> ItensDeEstoque => Set<ItemEstoque>();

    public DbSet<OrdemDeServico> OrdensDeServico => Set<OrdemDeServico>();

    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    private bool ProvedorGeraSequencial =>
        Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaDbContext).Assembly);

        if (!ProvedorGeraSequencial)
        {
            // No SQLite (testes) quem atribui o número da OS é o repositório.
            modelBuilder.Entity<OrdemDeServico>().Property(o => o.Numero).ValueGeneratedNever();
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(500);

        base.ConfigureConventions(configurationBuilder);
    }
}
