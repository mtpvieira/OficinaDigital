using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Orcamentos;

namespace OficinaDigital.Infrastructure.Persistencia.Configuracoes;

public class OrcamentoConfiguracao : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("Orcamentos", "orcamento");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasConversion<Conversores.OrcamentoIdConverter>().ValueGeneratedNever();

        builder.Property(o => o.OrdemDeServicoId)
            .HasConversion<Conversores.OrdemDeServicoIdConverter>().IsRequired();
        builder.HasIndex(o => o.OrdemDeServicoId);

        builder.Property(o => o.Tipo).HasConversion<int>().IsRequired();
        builder.Property(o => o.VersaoVigente).IsRequired();

        builder.OwnsMany(o => o.Versoes, versao =>
        {
            versao.ToTable("VersoesDeOrcamento", "orcamento");
            versao.WithOwner().HasForeignKey("OrcamentoId");
            versao.HasKey(v => v.Id);

            versao.Property(v => v.Numero).IsRequired();
            versao.Property(v => v.Total).HasColumnName("Total")
                .HasConversion<Conversores.DinheiroConverter>().HasPrecision(18, 2).IsRequired();
            versao.Property(v => v.GeradoEm).IsRequired();
            versao.Property(v => v.EnviadoEm);

            versao.OwnsOne(v => v.Resposta, resp =>
            {
                resp.Property(r => r.Decisao).HasColumnName("Decisao").HasConversion<int>();
                resp.Property(r => r.RespondidoEm).HasColumnName("RespondidoEm");
            });

            versao.OwnsMany(v => v.Itens, item =>
            {
                item.ToTable("ItensDeOrcamento", "orcamento");
                item.WithOwner().HasForeignKey("VersaoOrcamentoId");
                item.HasKey(i => i.Id);

                item.Property(i => i.Tipo).HasConversion<int>().IsRequired();
                item.Property(i => i.ReferenciaId).IsRequired();
                item.Property(i => i.Descricao).HasMaxLength(240).IsRequired();
                item.Property(i => i.Quantidade).HasPrecision(18, 3).IsRequired();
                item.Property(i => i.PrecoUnitario).HasColumnName("PrecoUnitario")
                    .HasConversion<Conversores.DinheiroConverter>().HasPrecision(18, 2).IsRequired();

                item.Ignore(i => i.Subtotal);
            });

            versao.Ignore(v => v.FoiRespondida);
            versao.Ignore(v => v.EstaVigenteParaResposta);
        });

        builder.Ignore(o => o.EventosDeDominio);
    }
}
