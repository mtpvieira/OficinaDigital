using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Clientes;
using OficinaDigital.Domain.OrdensServico;
using OficinaDigital.Domain.Veiculos;

namespace OficinaDigital.Infrastructure.Persistence;

public sealed class OficinaDigitalDbContext(DbContextOptions<OficinaDigitalDbContext> options)
    : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<OrdemDeServico> OrdensServico => Set<OrdemDeServico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaDigitalDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
