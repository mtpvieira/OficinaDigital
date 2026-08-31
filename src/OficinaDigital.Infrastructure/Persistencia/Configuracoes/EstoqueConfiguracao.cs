using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Estoque;

namespace OficinaDigital.Infrastructure.Persistencia.Configuracoes;

public class ItemEstoqueConfiguracao : IEntityTypeConfiguration<ItemEstoque>
{
    public void Configure(EntityTypeBuilder<ItemEstoque> builder)
    {
        builder.ToTable("ItensDeEstoque", "estoque");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasConversion<Conversores.ItemEstoqueIdConverter>().ValueGeneratedNever();

        builder.Property(i => i.PecaId).HasConversion<Conversores.PecaIdConverter>().IsRequired();
        builder.HasIndex(i => i.PecaId).IsUnique().HasDatabaseName("IX_ItensDeEstoque_PecaId");

        builder.Property(i => i.Unidade).HasConversion<int>().IsRequired();

        builder.Property<decimal>("_saldo").HasColumnName("Saldo").HasPrecision(18, 3).IsRequired();
        builder.Property<decimal>("_reservado").HasColumnName("Reservado").HasPrecision(18, 3).IsRequired();
        builder.Property<decimal>("_estoqueMinimo").HasColumnName("EstoqueMinimo").HasPrecision(18, 3).IsRequired();

        builder.OwnsMany(i => i.Reservas, reserva =>
        {
            reserva.ToTable("ReservasDeEstoque", "estoque");
            reserva.WithOwner().HasForeignKey("ItemEstoqueId");
            reserva.HasKey(r => r.Id);

            reserva.Property(r => r.OrdemDeServicoId)
                .HasConversion<Conversores.OrdemDeServicoIdConverter>().IsRequired();
            reserva.HasIndex(r => r.OrdemDeServicoId).HasDatabaseName("IX_ReservasDeEstoque_OsId");

            reserva.Property(r => r.Quantidade).HasPrecision(18, 3).IsRequired();
            reserva.Property(r => r.ReservadaEm).IsRequired();
        });

        builder.OwnsMany(i => i.Movimentos, mov =>
        {
            mov.ToTable("MovimentosDeEstoque", "estoque");
            mov.WithOwner().HasForeignKey("ItemEstoqueId");
            mov.HasKey(m => m.Id);

            mov.Property(m => m.Tipo).HasConversion<int>().IsRequired();
            mov.Property(m => m.Quantidade).HasPrecision(18, 3).IsRequired();
            mov.Property(m => m.SaldoResultante).HasPrecision(18, 3).IsRequired();
            mov.Property(m => m.OrdemDeServicoId).HasConversion<Conversores.OrdemDeServicoIdNulavelConverter>();
            mov.Property(m => m.Motivo).HasMaxLength(300);
            mov.Property(m => m.RegistradoEm).IsRequired();
        });

        builder.Ignore(i => i.Saldo);
        builder.Ignore(i => i.Reservado);
        builder.Ignore(i => i.EstoqueMinimo);
        builder.Ignore(i => i.Disponivel);
        builder.Ignore(i => i.AbaixoDoEstoqueMinimo);
        builder.Ignore(i => i.EventosDeDominio);
    }
}
