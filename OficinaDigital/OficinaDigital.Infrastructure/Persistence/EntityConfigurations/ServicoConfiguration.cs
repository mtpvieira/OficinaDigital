using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Infrastructure.Persistence.EntityConfigurations;

public sealed class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("Servicos");
        builder.HasKey(servico => servico.Id);

        builder.Property(servico => servico.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(servico => servico.Descricao)
            .HasMaxLength(1000);

        builder.Property(servico => servico.TempoEstimadoMinutos);

        builder.OwnsOne(servico => servico.PrecoBase, preco =>
        {
            preco.Property(m => m.Valor)
                .HasColumnName("PrecoBaseValor")
                .HasPrecision(18, 2)
                .IsRequired();

            preco.Property(m => m.Moeda)
                .HasColumnName("PrecoBaseMoeda")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(servico => servico.PrecoBase).IsRequired();
    }
}
