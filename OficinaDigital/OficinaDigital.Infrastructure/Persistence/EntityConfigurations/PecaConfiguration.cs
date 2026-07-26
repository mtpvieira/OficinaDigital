using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Infrastructure.Persistence.EntityConfigurations;

public sealed class PecaConfiguration : IEntityTypeConfiguration<Peca>
{
    public void Configure(EntityTypeBuilder<Peca> builder)
    {
        builder.ToTable("Pecas");
        builder.HasKey(peca => peca.Id);

        builder.Property(peca => peca.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(peca => peca.QuantidadeEmEstoque)
            .IsRequired();

        builder.OwnsOne(peca => peca.Preco, preco =>
        {
            preco.Property(m => m.Valor)
                .HasColumnName("PrecoValor")
                .HasPrecision(18, 2)
                .IsRequired();

            preco.Property(m => m.Moeda)
                .HasColumnName("PrecoMoeda")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(peca => peca.Preco).IsRequired();
    }
}
