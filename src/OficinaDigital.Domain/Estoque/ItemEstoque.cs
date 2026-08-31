using OficinaDigital.Domain.Common;
using OficinaDigital.Domain.Estoque.Events;

namespace OficinaDigital.Domain.Estoque;

public class ItemEstoque : AggregateRoot<ItemEstoqueId>
{
    private readonly List<ReservaEstoque> _reservas = [];
    private readonly List<MovimentoEstoque> _movimentos = [];

    private decimal _saldo;
    private decimal _reservado;
    private decimal _estoqueMinimo;

    public PecaId PecaId { get; private set; }
    public UnidadeDeMedida Unidade { get; private set; }

    public IReadOnlyCollection<ReservaEstoque> Reservas => _reservas.AsReadOnly();
    public IReadOnlyCollection<MovimentoEstoque> Movimentos => _movimentos.AsReadOnly();

    // A unidade mora só aqui: as quantidades guardam o número e recebem a unidade do item.
    public Quantidade Saldo => Quantidade.De(_saldo, Unidade);
    public Quantidade Reservado => Quantidade.De(_reservado, Unidade);
    public Quantidade EstoqueMinimo => Quantidade.De(_estoqueMinimo, Unidade);
    public Quantidade Disponivel => Saldo.Subtrair(Reservado);

    public bool AbaixoDoEstoqueMinimo => EstoqueMinimo.MaiorQue(Disponivel);

    protected ItemEstoque() { }

    private ItemEstoque(ItemEstoqueId id, PecaId pecaId, UnidadeDeMedida unidade, decimal estoqueMinimo)
    {
        Id = id;
        PecaId = pecaId;
        Unidade = unidade;
        _estoqueMinimo = estoqueMinimo;
    }

    public static ItemEstoque Abrir(PecaId pecaId, UnidadeDeMedida unidade, decimal estoqueMinimo)
    {
        DomainException.Se(estoqueMinimo < 0, "Estoque mínimo não pode ser negativo.");
        return new ItemEstoque(ItemEstoqueId.Novo(), pecaId, unidade, estoqueMinimo);
    }

    public void DefinirEstoqueMinimo(decimal novoMinimo)
    {
        DomainException.Se(novoMinimo < 0, "Estoque mínimo não pode ser negativo.");

        _estoqueMinimo = novoMinimo;

        Publicar(new EstoqueMinimoDefinido(Id, PecaId, novoMinimo));
        AvaliarPontoDeReposicao();
    }

    public void RegistrarEntrada(decimal quantidade, Dinheiro custoUnitario)
    {
        DomainException.Se(quantidade <= 0, "A entrada exige quantidade maior que zero.");
        DomainException.Se(custoUnitario.Valor <= 0, "A entrada exige custo unitário maior que zero.");

        _saldo = Saldo.Somar(Medir(quantidade)).Valor;

        _movimentos.Add(new MovimentoEstoque(TipoMovimentoEstoque.ENTRADA, quantidade, _saldo));
        Publicar(new EntradaDePecasRegistrada(Id, PecaId, quantidade));
    }

    public bool PodeReservar(decimal quantidade) =>
        quantidade > 0 && !Medir(quantidade).MaiorQue(Disponivel);

    public void Reservar(OrdemDeServicoId ordemDeServicoId, decimal quantidade)
    {
        DomainException.Se(quantidade <= 0, "A reserva exige quantidade maior que zero.");

        var aReservar = Medir(quantidade);
        DomainException.Se(aReservar.MaiorQue(Disponivel),
            $"Quantidade indisponível para reserva. Solicitado {aReservar}, disponível {Disponivel}.");

        var reservaExistente = _reservas.FirstOrDefault(r => r.OrdemDeServicoId == ordemDeServicoId);
        if (reservaExistente is null)
            _reservas.Add(new ReservaEstoque(ordemDeServicoId, quantidade));
        else
            reservaExistente.Acrescentar(quantidade);

        _reservado = Reservado.Somar(aReservar).Valor;

        Publicar(new PecaReservada(Id, PecaId, ordemDeServicoId, quantidade));
        AvaliarPontoDeReposicao();
    }

    public void LiberarReserva(OrdemDeServicoId ordemDeServicoId)
    {
        var reserva = _reservas.FirstOrDefault(r => r.OrdemDeServicoId == ordemDeServicoId);
        if (reserva is null) return;

        _reservas.Remove(reserva);
        _reservado = Reservado.Subtrair(Medir(reserva.Quantidade)).Valor;

        Publicar(new ReservaDePecaLiberada(Id, PecaId, ordemDeServicoId, reserva.Quantidade));
    }

    public void Subtrair(OrdemDeServicoId ordemDeServicoId)
    {
        var reserva = _reservas.FirstOrDefault(r => r.OrdemDeServicoId == ordemDeServicoId);
        DomainException.Se(reserva is null,
            $"Não há reserva desta peça para a OS {ordemDeServicoId}. A subtração sempre consome uma reserva.");

        var quantidade = reserva!.Quantidade;

        _reservas.Remove(reserva);
        _reservado = Reservado.Subtrair(Medir(quantidade)).Valor;
        _saldo = Saldo.Subtrair(Medir(quantidade)).Valor;

        _movimentos.Add(new MovimentoEstoque(TipoMovimentoEstoque.SUBTRACAO, quantidade, _saldo,
            ordemDeServicoId));

        Publicar(new PecaSubtraidaDoEstoque(Id, PecaId, ordemDeServicoId, quantidade));
        AvaliarPontoDeReposicao();
    }

    public void SinalizarFalta(OrdemDeServicoId ordemDeServicoId, decimal quantidadeSolicitada)
    {
        var disponivel = Disponivel.Valor;

        Publicar(new FaltaDePecaIdentificada(PecaId, ordemDeServicoId, quantidadeSolicitada, disponivel));
        Publicar(new CompraDePecaSinalizada(PecaId, quantidadeSolicitada - disponivel));
    }

    public decimal QuantidadeReservadaPara(OrdemDeServicoId ordemDeServicoId) =>
        _reservas.FirstOrDefault(r => r.OrdemDeServicoId == ordemDeServicoId)?.Quantidade ?? 0m;

    private Quantidade Medir(decimal valor) => Quantidade.De(valor, Unidade);

    private void AvaliarPontoDeReposicao()
    {
        if (!AbaixoDoEstoqueMinimo) return;

        Publicar(new AlertaDeEstoqueMinimoEmitido(Id, PecaId, Disponivel.Valor, _estoqueMinimo));
    }
}
