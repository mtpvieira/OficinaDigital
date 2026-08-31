using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Catalogo;
using OficinaDigital.Domain.Catalogo.Repositories;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Application.Catalogo;

public class ServicoService(IServicoRepository servicos, IUnitOfWork uow)
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<ServicoResponse> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var servico = await servicos.ObterPorIdAsync(new ServicoId(id), ct)
                      ?? throw new RecursoNaoEncontradoException("Serviço", id);

        return ServicoResponse.De(servico, Hoje);
    }

    public async Task<ResultadoPaginado<ServicoResponse>> ListarAsync(bool? ativo, Paginacao paginacao,
        CancellationToken ct = default)
    {
        var p = paginacao.Normalizar();

        var itens = await servicos.ListarAsync(ativo, p.Pagina, p.TamanhoPagina, ct);
        var total = await servicos.ContarAsync(ativo, ct);

        return new ResultadoPaginado<ServicoResponse>(
            itens.Select(s => ServicoResponse.De(s, Hoje)).ToList(), p.Pagina, p.TamanhoPagina, total);
    }

    public async Task<ServicoResponse> CadastrarAsync(CadastrarServicoRequest request, CancellationToken ct = default)
    {
        if (await servicos.ExisteComNomeAsync(request.Nome.Trim(), null, ct))
            throw new ConflitoDeNegocioException($"Já existe um serviço chamado {request.Nome} no catálogo.");

        var servico = Servico.Cadastrar(request.Nome, request.Descricao, Dinheiro.De(request.Preco),
            request.TempoPadraoMinutos, Hoje);

        await servicos.AdicionarAsync(servico, ct);
        await uow.CommitAsync(ct);

        return ServicoResponse.De(servico, Hoje);
    }

    public async Task<ServicoResponse> AlterarAsync(Guid id, AlterarServicoRequest request,
        CancellationToken ct = default)
    {
        var servicoId = new ServicoId(id);

        var servico = await servicos.ObterPorIdAsync(servicoId, ct)
                      ?? throw new RecursoNaoEncontradoException("Serviço", id);

        if (await servicos.ExisteComNomeAsync(request.Nome.Trim(), servicoId, ct))
            throw new ConflitoDeNegocioException($"Já existe outro serviço chamado {request.Nome} no catálogo.");

        servico.AlterarDados(request.Nome, request.Descricao, request.TempoPadraoMinutos);

        await uow.CommitAsync(ct);
        return ServicoResponse.De(servico, Hoje);
    }

    public async Task<ServicoResponse> AlterarPrecoAsync(Guid id, AlterarPrecoRequest request,
        CancellationToken ct = default)
    {
        var servico = await servicos.ObterPorIdAsync(new ServicoId(id), ct)
                      ?? throw new RecursoNaoEncontradoException("Serviço", id);

        servico.AlterarPreco(Dinheiro.De(request.Preco), request.VigenteDe ?? Hoje);

        await uow.CommitAsync(ct);
        return ServicoResponse.De(servico, Hoje);
    }

    public async Task InativarAsync(Guid id, CancellationToken ct = default)
    {
        var servico = await servicos.ObterPorIdAsync(new ServicoId(id), ct)
                      ?? throw new RecursoNaoEncontradoException("Serviço", id);

        servico.Inativar();
        await uow.CommitAsync(ct);
    }
}
