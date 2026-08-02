using FluentValidation;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Common.Exceptions;
using OficinaDigital.Domain.Clientes;

namespace OficinaDigital.Application.Clientes;

public sealed class ClienteService(
    IClienteRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<CriarClienteRequest> criarValidator,
    IValidator<AtualizarClienteRequest> atualizarValidator) : IClienteService
{
    public async Task<ClienteDto> CriarAsync(CriarClienteRequest request, CancellationToken cancellationToken = default)
    {
        await criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var cliente = Cliente.Criar(request.Nome, request.Documento, request.Email, request.Telefone);

        await repository.AdicionarAsync(cliente, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(cliente);
    }

    public async Task<ClienteDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ParaDto(await ObterOuFalharAsync(id, cancellationToken));

    public async Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var clientes = await repository.ListarAsync(cancellationToken);
        return clientes.Select(ParaDto).ToList();
    }

    public async Task<ClienteDto> AtualizarAsync(Guid id, AtualizarClienteRequest request, CancellationToken cancellationToken = default)
    {
        await atualizarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var cliente = await ObterOuFalharAsync(id, cancellationToken);
        cliente.AtualizarDados(request.Nome, request.Email, request.Telefone);

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ParaDto(cliente);
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cliente = await ObterOuFalharAsync(id, cancellationToken);

        repository.Remover(cliente);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Cliente> ObterOuFalharAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.ObterPorIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Cliente '{id}' não encontrado.");

    private static ClienteDto ParaDto(Cliente cliente) =>
        new(cliente.Id, cliente.Nome, cliente.Documento.Formatado, cliente.Email, cliente.Telefone);
}
