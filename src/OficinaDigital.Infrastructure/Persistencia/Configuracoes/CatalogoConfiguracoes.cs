using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Catalogo;

namespace OficinaDigital.Infrastructure.Persistencia.Configuracoes;

public class ServicoConfiguracao : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("Servicos", "catalogo");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasConversion<Conversores.ServicoIdConverter>().ValueGeneratedNever();

        builder.Property(s => s.Nome).HasMaxLength(120).IsRequired();
        builder.HasIndex(s => s.Nome).IsUnique().HasDatabaseName("IX_Servicos_Nome");

        builder.Property(s => s.Descricao).HasMaxLength(500);
        builder.Property(s => s.Ativo).IsRequired();

        builder.Property(s => s.TempoPadraoExecucao)
            .HasColumnName("TempoPadraoMinutos")
            .HasConversion<Conversores.DuracaoConverter>()
            .IsRequired();

        builder.OwnsMany(s => s.Precos, preco =>
        {
            preco.ToTable("ServicoPrecos", "catalogo");
            preco.WithOwner().HasForeignKey("ServicoId");
            preco.Property<int>("Id").ValueGeneratedOnAdd();
            preco.HasKey("Id");

            preco.Property(p => p.Valor).HasColumnName("Valor")
                .HasConversion<Conversores.DinheiroConverter>().HasPrecision(18, 2).IsRequired();
            preco.Property(p => p.VigenteDe).HasColumnName("VigenteDe").IsRequired();
        });

        builder.Ignore(s => s.EventosDeDominio);
    }
}

public class PecaConfiguracao : IEntityTypeConfiguration<Peca>
{
    public void Configure(EntityTypeBuilder<Peca> builder)
    {
        builder.ToTable("Pecas", "catalogo");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasConversion<Conversores.PecaIdConverter>().ValueGeneratedNever();

        builder.Property(p => p.Sku)
            .HasColumnName("Sku")
            .HasConversion<Conversores.SkuConverter>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("IX_Pecas_Sku");

        builder.Property(p => p.Descricao).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Unidade).HasConversion<int>().IsRequired();
        builder.Property(p => p.Ativa).IsRequired();

        builder.OwnsMany(p => p.Precos, preco =>
        {
            preco.ToTable("PecaPrecos", "catalogo");
            preco.WithOwner().HasForeignKey("PecaId");
            preco.Property<int>("Id").ValueGeneratedOnAdd();
            preco.HasKey("Id");

            preco.Property(x => x.Valor).HasColumnName("Valor")
                .HasConversion<Conversores.DinheiroConverter>().HasPrecision(18, 2).IsRequired();
            preco.Property(x => x.VigenteDe).HasColumnName("VigenteDe").IsRequired();
        });

        builder.Ignore(p => p.EventosDeDominio);
    }
}
