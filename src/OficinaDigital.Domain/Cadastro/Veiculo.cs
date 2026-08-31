using OficinaDigital.Domain.Cadastro.Events;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Domain.Cadastro;

public class Veiculo : AggregateRoot<VeiculoId>
{
    public ClienteId ClienteId { get; private set; }
    public Placa Placa { get; private set; } = null!;
    public string Marca { get; private set; } = null!;
    public string Modelo { get; private set; } = null!;
    public int AnoFabricacao { get; private set; }
    public string? Cor { get; private set; }
    public DateTime CriadoEm { get; private set; }

    protected Veiculo() { }

    private Veiculo(VeiculoId id, ClienteId clienteId, Placa placa, string marca, string modelo,
        int anoFabricacao, string? cor)
    {
        Id = id;
        ClienteId = clienteId;
        Placa = placa;
        Marca = marca;
        Modelo = modelo;
        AnoFabricacao = anoFabricacao;
        Cor = cor;
        CriadoEm = DateTime.UtcNow;
    }

    public static Veiculo Cadastrar(ClienteId clienteId, string? placa, string? marca, string? modelo,
        int anoFabricacao, string? cor = null)
    {
        DomainException.Se(clienteId.Valor == Guid.Empty, "Veículo precisa pertencer a um cliente.");
        DomainException.Se(string.IsNullOrWhiteSpace(marca), "Marca do veículo é obrigatória.");
        DomainException.Se(string.IsNullOrWhiteSpace(modelo), "Modelo do veículo é obrigatório.");
        ValidarAno(anoFabricacao);

        var veiculo = new Veiculo(VeiculoId.Novo(), clienteId, Placa.Criar(placa),
            marca!.Trim(), modelo!.Trim(), anoFabricacao, cor?.Trim());

        veiculo.Publicar(new VeiculoCadastrado(veiculo.Id, clienteId, veiculo.Placa.Valor));
        return veiculo;
    }

    public void AlterarDados(string? marca, string? modelo, int anoFabricacao, string? cor)
    {
        DomainException.Se(string.IsNullOrWhiteSpace(marca), "Marca do veículo é obrigatória.");
        DomainException.Se(string.IsNullOrWhiteSpace(modelo), "Modelo do veículo é obrigatório.");
        ValidarAno(anoFabricacao);

        Marca = marca!.Trim();
        Modelo = modelo!.Trim();
        AnoFabricacao = anoFabricacao;
        Cor = cor?.Trim();

        Publicar(new DadosDoVeiculoAlterados(Id));
    }

    private static void ValidarAno(int anoFabricacao)
    {
        var anoCorrente = DateTime.UtcNow.Year;
        DomainException.Se(anoFabricacao < 1900 || anoFabricacao > anoCorrente,
            $"Ano de fabricação deve estar entre 1900 e {anoCorrente}.");
    }
}
