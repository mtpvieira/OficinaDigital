using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Clientes;

namespace OficinaDigital.Infrastructure.Persistence.EntityConfigurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(cliente => cliente.Id);

        builder.Property(cliente => cliente.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cliente => cliente.Email)
            .HasMaxLength(200);

        builder.Property(cliente => cliente.Telefone)
            .HasMaxLength(20);

        builder.OwnsOne(cliente => cliente.Documento, documento =>
        {
            documento.Property(d => d.Numero)
                .HasColumnName("Documento")
                .IsRequired()
                .HasMaxLength(14);

            documento.Property(d => d.Tipo)
                .HasColumnName("DocumentoTipo")
                .IsRequired();

            documento.HasIndex(d => d.Numero).IsUnique();
        });

        builder.Navigation(cliente => cliente.Documento).IsRequired();
    }
}
