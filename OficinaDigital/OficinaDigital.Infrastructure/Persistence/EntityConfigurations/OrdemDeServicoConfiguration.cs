using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.OrdensServico;

namespace OficinaDigital.Infrastructure.Persistence.EntityConfigurations;

public sealed class OrdemDeServicoConfiguration : IEntityTypeConfiguration<OrdemDeServico>
{
    public void Configure(EntityTypeBuilder<OrdemDeServico> builder)
    {
        builder.ToTable("OrdensServico");
        builder.HasKey(ordem => ordem.Id);

        builder.Property(ordem => ordem.Numero)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(ordem => ordem.Numero).IsUnique();

        builder.Property(ordem => ordem.ClienteId).IsRequired();
        builder.Property(ordem => ordem.VeiculoId).IsRequired();
        builder.Property(ordem => ordem.Status).HasConversion<int>()
            .IsRequired();

        builder.Property(ordem => ordem.CriadaEm).IsRequired();
        builder.Property(ordem => ordem.EnviadaAprovacaoEm);
        builder.Property(ordem => ordem.AprovadaEm);
        builder.Property(ordem => ordem.ExecucaoIniciadaEm);
        builder.Property(ordem => ordem.FinalizadaEm);
        builder.Property(ordem => ordem.EntregueEm);

        builder.OwnsOne(ordem => ordem.Orcamento, orcamento =>
        {
            orcamento.Property(m => m.Valor)
                .HasColumnName("OrcamentoValor")
                .HasPrecision(18, 2)
                .IsRequired();

            orcamento.Property(m => m.Moeda)
                .HasColumnName("OrcamentoMoeda")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(ordem => ordem.Orcamento).IsRequired();

        builder.Navigation(ordem => ordem.ItensServico)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(ordem => ordem.ItensServico, item =>
        {
            item.ToTable("OrdemServicoItens");
            item.WithOwner().HasForeignKey("OrdemDeServicoId");
            item.HasKey(i => i.Id);

            item.Property(i => i.ServicoId).IsRequired();
            item.Property(i => i.Descricao).IsRequired().HasMaxLength(200);

            item.OwnsOne(i => i.Preco, preco =>
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

            item.Navigation(i => i.Preco).IsRequired();
        });

        builder.Navigation(ordem => ordem.ItensPeca)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(ordem => ordem.ItensPeca, item =>
        {
            item.ToTable("OrdemServicoPecas");
            item.WithOwner().HasForeignKey("OrdemDeServicoId");
            item.HasKey(i => i.Id);

            item.Property(i => i.PecaId).IsRequired();
            item.Property(i => i.Descricao).IsRequired().HasMaxLength(200);
            item.Property(i => i.Quantidade).IsRequired();

            item.OwnsOne(i => i.PrecoUnitario, preco =>
            {
                preco.Property(m => m.Valor)
                    .HasColumnName("PrecoUnitarioValor")
                    .HasPrecision(18, 2)
                    .IsRequired();

                preco.Property(m => m.Moeda)
                    .HasColumnName("PrecoUnitarioMoeda")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            item.Navigation(i => i.PrecoUnitario).IsRequired();
        });
    }
}
