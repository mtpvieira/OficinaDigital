using Microsoft.EntityFrameworkCore;
using OficinaDigital.Domain.Clientes;

namespace OficinaDigital.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository(OficinaDigitalDbContext context) : IClienteRepository
{
    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Clientes.FirstOrDefaultAsync(cliente => cliente.Id == id, cancellationToken);

    public Task<Cliente?> ObterPorDocumentoAsync(string documento, CancellationToken cancellationToken = default)
    {
        var digitos = new string(documento.Where(char.IsDigit).ToArray());
        return context.Clientes.FirstOrDefaultAsync(cliente => cliente.Documento.Numero == digitos, cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.Clientes.ToListAsync(cancellationToken);

    public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default) =>
        await context.Clientes.AddAsync(cliente, cancellationToken);

    public void Remover(Cliente cliente) => context.Clientes.Remove(cliente);
}
