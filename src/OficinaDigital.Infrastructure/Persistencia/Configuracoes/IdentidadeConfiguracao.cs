using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Identidade;

namespace OficinaDigital.Infrastructure.Persistencia.Configuracoes;

public class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios", "identidade");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasConversion<Conversores.UsuarioIdConverter>().ValueGeneratedNever();

        builder.Property(u => u.Nome).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Usuarios_Email");

        builder.Property(u => u.SenhaHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.SenhaSalt).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Perfil).HasConversion<int>().IsRequired();
        builder.Property(u => u.Ativo).IsRequired();
        builder.Property(u => u.CriadoEm).IsRequired();
        builder.Property(u => u.UltimoAcessoEm);

        builder.Ignore(u => u.EventosDeDominio);
    }
}
