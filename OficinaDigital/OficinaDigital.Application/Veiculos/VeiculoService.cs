using FluentValidation;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Common.Exceptions;
using OficinaDigital.Domain.Clientes;
using OficinaDigital.Domain.Veiculos;

namespace OficinaDigital.Application.Veiculos;

public sealed class VeiculoService(
    IVeiculoRepository repository,
    IClienteRepository clienteRepository,
    IUnitOfWork unitOfWork,
    IValidator<CriarVeiculoRequest> criarValidator,
    IValidator<AtualizarVeiculoRequest> atualizarValidator) : IVeiculoService
{
    public async Task<VeiculoDto> CriarAsync(CriarVeiculoRequest request, CancellationToken cancellationToken = default)
    {
        await criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken)
            ?? throw new NotFoundException($"Cliente '{request.ClienteId}' não encontrado.");

        var veiculo = Veiculo.Criar(request.ClienteId, request.Placa, request.Marca, request.Modelo, request.Ano);

        await repository.AdicionarAsync(veiculo, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(veiculo);
    }

    public async Task<VeiculoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ParaDto(await ObterOuFalharAsync(id, cancellationToken));

    public async Task<IReadOnlyList<VeiculoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var veiculos = await repository.ListarAsync(cancellationToken);
        return veiculos.Select(ParaDto).ToList();
    }

    public async Task<VeiculoDto> AtualizarAsync(Guid id, AtualizarVeiculoRequest request, CancellationToken cancellationToken = default)
    {
        await atualizarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var veiculo = await ObterOuFalharAsync(id, cancellationToken);
        veiculo.AtualizarDados(request.Marca, request.Modelo, request.Ano);

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(veiculo);
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var veiculo = await ObterOuFalharAsync(id, cancellationToken);

        repository.Remover(veiculo);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Veiculo> ObterOuFalharAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.ObterPorIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Veículo '{id}' não encontrado.");

    private static VeiculoDto ParaDto(Veiculo veiculo) =>
        new(veiculo.Id, veiculo.ClienteId, veiculo.Placa, veiculo.Marca, veiculo.Modelo, veiculo.Ano);
}
