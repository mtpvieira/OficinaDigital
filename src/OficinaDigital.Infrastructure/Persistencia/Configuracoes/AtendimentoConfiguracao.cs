using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Atendimento;

namespace OficinaDigital.Infrastructure.Persistencia.Configuracoes;

public class OrdemDeServicoConfiguracao : IEntityTypeConfiguration<OrdemDeServico>
{
    public void Configure(EntityTypeBuilder<OrdemDeServico> builder)
    {
        builder.ToTable("OrdensDeServico", "atendimento");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasConversion<Conversores.OrdemDeServicoIdConverter>().ValueGeneratedNever();

        builder.Property(o => o.Numero).ValueGeneratedOnAdd().UseIdentityColumn(1000, 1);
        builder.HasIndex(o => o.Numero).IsUnique().HasDatabaseName("IX_OrdensDeServico_Numero");

        builder.Property(o => o.ClienteId).HasConversion<Conversores.ClienteIdConverter>().IsRequired();
        builder.Property(o => o.VeiculoId).HasConversion<Conversores.VeiculoIdConverter>().IsRequired();
        builder.HasIndex(o => o.ClienteId);
        builder.HasIndex(o => o.VeiculoId);

        builder.Property(o => o.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(o => o.Status);

        builder.Property(o => o.MotivoCancelamento).HasConversion<int?>();
        builder.Property(o => o.ObservacaoCancelamento).HasMaxLength(500);
        builder.Property(o => o.DescricaoDoProblema).HasMaxLength(1000);
        builder.Property(o => o.OrcamentoVigenteId).HasConversion<Conversores.OrcamentoIdNulavelConverter>();
        builder.Property(o => o.OrcamentoAprovado).IsRequired();
        builder.Property(o => o.AbertaEm).IsRequired();

        builder.Property(o => o.Total)
            .HasColumnName("Total")
            .HasConversion<Conversores.DinheiroConverter>()
            .HasPrecision(18, 2)
            .IsRequired();

        builder.OwnsOne(o => o.Execucao, exec =>
        {
            exec.Property(e => e.Inicio).HasColumnName("ExecucaoInicio");
            exec.Property(e => e.Fim).HasColumnName("ExecucaoFim");
            // Em ticks porque o tipo time do SQL Server estoura em 24h.
            exec.Property(e => e.TempoAguardandoAprovacao)
                .HasColumnName("TempoAguardandoAprovacaoTicks")
                .HasConversion(t => t.Ticks, v => TimeSpan.FromTicks(v));
            exec.Property(e => e.SuspensaEm).HasColumnName("ExecucaoSuspensaEm");
            exec.Ignore(e => e.DuracaoReal);
            exec.Ignore(e => e.DuracaoRealEmMinutos);
        });
        builder.Navigation(o => o.Execucao).IsRequired();

        builder.OwnsMany(o => o.ItensDeServico, item =>
        {
            item.ToTable("ItensDeServicoDaOs", "atendimento");
            item.WithOwner().HasForeignKey("OrdemDeServicoId");
            item.HasKey(i => i.Id);

            item.Property(i => i.ServicoId).HasConversion<Conversores.ServicoIdConverter>().IsRequired();
            item.HasIndex(i => i.ServicoId);

            item.Property(i => i.Descricao).HasMaxLength(200).IsRequired();
            item.Property(i => i.PrecoCongelado).HasColumnName("PrecoCongelado")
                .HasConversion<Conversores.DinheiroConverter>().HasPrecision(18, 2).IsRequired();
            item.Property(i => i.TempoPadraoMinutos).IsRequired();
            item.Property(i => i.Origem).HasConversion<int>().IsRequired();
            item.Property(i => i.AguardandoAprovacaoDoCliente).IsRequired();
            item.Property(i => i.RegistradoEm).IsRequired();

            item.Ignore(i => i.Subtotal);
        });

        builder.OwnsMany(o => o.ItensDePeca, item =>
        {
            item.ToTable("ItensDePecaDaOs", "atendimento");
            item.WithOwner().HasForeignKey("OrdemDeServicoId");
            item.HasKey(i => i.Id);

            item.Property(i => i.PecaId).HasConversion<Conversores.PecaIdConverter>().IsRequired();
            item.HasIndex(i => i.PecaId);

            item.Property(i => i.Descricao).HasMaxLength(240).IsRequired();
            item.Property(i => i.Quantidade).HasPrecision(18, 3).IsRequired();
            item.Property(i => i.PrecoCongelado).HasColumnName("PrecoCongelado")
                .HasConversion<Conversores.DinheiroConverter>().HasPrecision(18, 2).IsRequired();
            item.Property(i => i.Origem).HasConversion<int>().IsRequired();
            item.Property(i => i.Status).HasConversion<int>().IsRequired();
            item.Property(i => i.AguardandoAprovacaoDoCliente).IsRequired();
            item.Property(i => i.RegistradoEm).IsRequired();

            item.Ignore(i => i.Subtotal);
        });

        builder.Ignore(o => o.EstaAtiva);
        builder.Ignore(o => o.PossuiPecaPendenteDeCompra);
        builder.Ignore(o => o.PossuiItemAdicionalPendente);
        builder.Ignore(o => o.EventosDeDominio);
    }
}
