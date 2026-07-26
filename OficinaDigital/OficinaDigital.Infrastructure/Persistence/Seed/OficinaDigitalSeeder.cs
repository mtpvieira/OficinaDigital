using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Infrastructure.Persistence.Seed;

public static class OficinaDigitalSeeder
{
    public static async Task SeedAsync(OficinaDigitalDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (!await context.Servicos.AnyAsync(cancellationToken))
        {
            context.Servicos.AddRange(
                Servico.Criar("Troca de óleo", 120m, "Troca de óleo e filtro", 30),
                Servico.Criar("Alinhamento e balanceamento", 150m, "Alinhamento e balanceamento das quatro rodas", 60),
                Servico.Criar("Revisão de freios", 200m, "Inspeção e troca de pastilhas de freio", 90));
        }

        if (!await context.Pecas.AnyAsync(cancellationToken))
        {
            context.Pecas.AddRange(
                Peca.Criar("Filtro de óleo", 35m, 50),
                Peca.Criar("Pastilha de freio (jogo)", 180m, 30),
                Peca.Criar("Óleo do motor 1L", 45m, 100));
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
