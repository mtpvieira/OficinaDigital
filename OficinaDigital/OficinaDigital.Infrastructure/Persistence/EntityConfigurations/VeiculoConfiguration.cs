using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Veiculos;

namespace OficinaDigital.Infrastructure.Persistence.EntityConfigurations;

public sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("Veiculos");
        builder.HasKey(veiculo => veiculo.Id);

        builder.Property(veiculo => veiculo.ClienteId)
            .IsRequired();

        builder.Property(veiculo => veiculo.Placa)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(veiculo => veiculo.Marca)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(veiculo => veiculo.Modelo)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(veiculo => veiculo.Ano)
            .IsRequired();

        builder.HasIndex(veiculo => veiculo.Placa).IsUnique();
        builder.HasIndex(veiculo => veiculo.ClienteId);
    }
}
