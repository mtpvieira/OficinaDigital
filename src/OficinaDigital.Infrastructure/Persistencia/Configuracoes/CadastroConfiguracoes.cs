using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaDigital.Domain.Cadastro;

namespace OficinaDigital.Infrastructure.Persistencia.Configuracoes;

public class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes", "cadastro");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasConversion<Conversores.ClienteIdConverter>().ValueGeneratedNever();

        builder.Property(c => c.Nome).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Ativo).IsRequired();
        builder.Property(c => c.CriadoEm).IsRequired();

        builder.OwnsOne(c => c.Documento, doc =>
        {
            doc.Property(d => d.Numero).HasColumnName("Documento").HasMaxLength(14).IsRequired();
            doc.Property(d => d.Tipo).HasColumnName("TipoPessoa").HasConversion<int>().IsRequired();

            doc.HasIndex(d => d.Numero).IsUnique().HasDatabaseName("IX_Clientes_Documento");
        });
        builder.Navigation(c => c.Documento).IsRequired();

        builder.OwnsMany(c => c.Contatos, con =>
        {
            con.ToTable("ClienteContatos", "cadastro");
            con.WithOwner().HasForeignKey("ClienteId");
            con.Property<int>("Id").ValueGeneratedOnAdd();
            con.HasKey("Id");

            con.Property(x => x.Canal).HasConversion<int>().IsRequired();
            con.Property(x => x.Valor).HasMaxLength(120).IsRequired();
        });

        builder.OwnsOne(c => c.Endereco, end =>
        {
            end.Property(e => e.Logradouro).HasColumnName("Logradouro").HasMaxLength(150);
            end.Property(e => e.Numero).HasColumnName("NumeroEndereco").HasMaxLength(15);
            end.Property(e => e.Complemento).HasColumnName("Complemento").HasMaxLength(60);
            end.Property(e => e.Cidade).HasColumnName("Cidade").HasMaxLength(80);
            end.Property(e => e.Uf).HasColumnName("Uf").HasMaxLength(2);
            end.Property(e => e.Cep).HasColumnName("Cep").HasMaxLength(8);
        });

        builder.Ignore(c => c.EventosDeDominio);
    }
}

public class VeiculoConfiguracao : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("Veiculos", "cadastro");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasConversion<Conversores.VeiculoIdConverter>().ValueGeneratedNever();

        builder.Property(v => v.ClienteId).HasConversion<Conversores.ClienteIdConverter>().IsRequired();
        builder.HasIndex(v => v.ClienteId);

        builder.Property(v => v.Marca).HasMaxLength(40).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(60).IsRequired();
        builder.Property(v => v.AnoFabricacao).IsRequired();
        builder.Property(v => v.Cor).HasMaxLength(30);
        builder.Property(v => v.CriadoEm).IsRequired();

        builder.OwnsOne(v => v.Placa, placa =>
        {
            placa.Property(p => p.Valor).HasColumnName("Placa").HasMaxLength(7).IsRequired();
            placa.Property(p => p.Padrao).HasColumnName("PadraoPlaca").HasConversion<int>().IsRequired();

            placa.HasIndex(p => p.Valor).IsUnique().HasDatabaseName("IX_Veiculos_Placa");
        });
        builder.Navigation(v => v.Placa).IsRequired();

        builder.Ignore(v => v.EventosDeDominio);
    }
}
