using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OficinaDigital.Domain.Catalogo.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Infrastructure.Persistencia;

public static class Conversores
{
    public sealed class ClienteIdConverter() : ValueConverter<ClienteId, Guid>(id => id.Valor, v => new ClienteId(v));

    public sealed class VeiculoIdConverter() : ValueConverter<VeiculoId, Guid>(id => id.Valor, v => new VeiculoId(v));

    public sealed class ServicoIdConverter() : ValueConverter<ServicoId, Guid>(id => id.Valor, v => new ServicoId(v));

    public sealed class PecaIdConverter() : ValueConverter<PecaId, Guid>(id => id.Valor, v => new PecaId(v));

    public sealed class ItemEstoqueIdConverter()
        : ValueConverter<ItemEstoqueId, Guid>(id => id.Valor, v => new ItemEstoqueId(v));

    public sealed class OrdemDeServicoIdConverter()
        : ValueConverter<OrdemDeServicoId, Guid>(id => id.Valor, v => new OrdemDeServicoId(v));

    public sealed class OrcamentoIdConverter()
        : ValueConverter<OrcamentoId, Guid>(id => id.Valor, v => new OrcamentoId(v));

    public sealed class UsuarioIdConverter() : ValueConverter<UsuarioId, Guid>(id => id.Valor, v => new UsuarioId(v));

    public sealed class OrdemDeServicoIdNulavelConverter()
        : ValueConverter<OrdemDeServicoId?, Guid?>(
            id => id == null ? null : id.Value.Valor,
            v => v == null ? null : new OrdemDeServicoId(v.Value));

    public sealed class OrcamentoIdNulavelConverter()
        : ValueConverter<OrcamentoId?, Guid?>(
            id => id == null ? null : id.Value.Valor,
            v => v == null ? null : new OrcamentoId(v.Value));

    public sealed class DinheiroConverter() : ValueConverter<Dinheiro, decimal>(d => d.Valor, v => Dinheiro.De(v));

    public sealed class DuracaoConverter() : ValueConverter<Duracao, int>(d => d.Minutos, v => Duracao.DeMinutos(v));

    public sealed class SkuConverter() : ValueConverter<Sku, string>(s => s.Codigo, v => Sku.Criar(v));
}
