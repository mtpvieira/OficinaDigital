using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Atendimento.Repositories;
using OficinaDigital.Domain.Cadastro;
using OficinaDigital.Domain.Cadastro.Repositories;
using OficinaDigital.Domain.Cadastro.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Application.Cadastro;

public class ClienteService(
    IClienteRepository clientes,
    IOrdemDeServicoRepository ordens,
    IUnitOfWork uow)
{
    public async Task<ClienteResponse?> IdentificarPorDocumentoAsync(string documento, CancellationToken ct = default)
    {
        var cpfCnpj = CpfCnpj.Criar(documento);
        var cliente = await clientes.ObterPorDocumentoAsync(cpfCnpj, ct);

        return cliente is null ? null : ClienteResponse.De(cliente);
    }

    public async Task<ClienteResponse> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await clientes.ObterPorIdAsync(new ClienteId(id), ct)
                      ?? throw new RecursoNaoEncontradoException("Cliente", id);

        return ClienteResponse.De(cliente);
    }

    public async Task<ResultadoPaginado<ClienteResponse>> ListarAsync(string? nome, bool? ativo,
        Paginacao paginacao, CancellationToken ct = default)
    {
        var p = paginacao.Normalizar();

        var itens = await clientes.ListarAsync(nome, ativo, p.Pagina, p.TamanhoPagina, ct);
        var total = await clientes.ContarAsync(nome, ativo, ct);

        return new ResultadoPaginado<ClienteResponse>(
            itens.Select(ClienteResponse.De).ToList(), p.Pagina, p.TamanhoPagina, total);
    }

    public async Task<ClienteResponse> CadastrarAsync(CadastrarClienteRequest request, CancellationToken ct = default)
    {
        var documento = CpfCnpj.Criar(request.Documento);

        if (await clientes.ExisteComDocumentoAsync(documento, ct))
            throw new ConflitoDeNegocioException($"Já existe cliente cadastrado com o documento {documento.Formatado()}.");

        var cliente = Cliente.Cadastrar(
            request.Nome,
            request.Documento,
            request.Contatos.Select(c => Contato.Criar(c.Canal, c.Valor)),
            MapearEndereco(request.Endereco));

        await clientes.AdicionarAsync(cliente, ct);
        await uow.CommitAsync(ct);

        return ClienteResponse.De(cliente);
    }

    public async Task<ClienteResponse> AlterarAsync(Guid id, AlterarClienteRequest request,
        CancellationToken ct = default)
    {
        var cliente = await clientes.ObterPorIdAsync(new ClienteId(id), ct)
                      ?? throw new RecursoNaoEncontradoException("Cliente", id);

        cliente.AlterarDados(
            request.Nome,
            request.Contatos.Select(c => Contato.Criar(c.Canal, c.Valor)),
            MapearEndereco(request.Endereco));

        await uow.CommitAsync(ct);
        return ClienteResponse.De(cliente);
    }

    public async Task InativarAsync(Guid id, CancellationToken ct = default)
    {
        var clienteId = new ClienteId(id);

        var cliente = await clientes.ObterPorIdAsync(clienteId, ct)
                      ?? throw new RecursoNaoEncontradoException("Cliente", id);

        if (await ordens.ClientePossuiOsAtivaAsync(clienteId, ct))
            throw new ConflitoDeNegocioException(
                "Cliente com ordem de serviço ativa não pode ser inativado. Conclua ou cancele a OS antes.");

        cliente.Inativar();
        await uow.CommitAsync(ct);
    }

    private static Endereco? MapearEndereco(EnderecoDto? dto) =>
        dto is null ? null : Endereco.Criar(dto.Logradouro, dto.Numero, dto.Complemento, dto.Cidade, dto.Uf, dto.Cep);
}
