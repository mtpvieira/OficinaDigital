namespace OficinaDigital.Domain.Common;

public readonly record struct ClienteId(Guid Valor)
{
    public static ClienteId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}

public readonly record struct VeiculoId(Guid Valor)
{
    public static VeiculoId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}

public readonly record struct ServicoId(Guid Valor)
{
    public static ServicoId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}

public readonly record struct PecaId(Guid Valor)
{
    public static PecaId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}

public readonly record struct ItemEstoqueId(Guid Valor)
{
    public static ItemEstoqueId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}

public readonly record struct OrdemDeServicoId(Guid Valor)
{
    public static OrdemDeServicoId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}

public readonly record struct OrcamentoId(Guid Valor)
{
    public static OrcamentoId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}

public readonly record struct UsuarioId(Guid Valor)
{
    public static UsuarioId Novo() => new(Guid.NewGuid());
    public override string ToString() => Valor.ToString();
}
