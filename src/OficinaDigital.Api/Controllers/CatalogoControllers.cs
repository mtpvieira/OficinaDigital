using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Catalogo;
using OficinaDigital.Application.Common;

namespace OficinaDigital.Api.Controllers;

/// <summary>CRUD do catálogo de serviços: o que a oficina sabe fazer.</summary>
[ApiController]
[Route("api/servicos")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Interno)]
public class ServicosController(ServicoService servicos) : ControllerBase
{
    /// <summary>Lista os serviços do catálogo.</summary>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<ServicoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<ServicoResponse>>> Listar(
        [FromQuery] bool? ativo, [FromQuery] Paginacao paginacao, CancellationToken ct) =>
        Ok(await servicos.ListarAsync(ativo, paginacao, ct));

    /// <summary>Detalha um serviço, com o histórico de preços.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicoResponse>> ObterPorId(Guid id, CancellationToken ct) =>
        Ok(await servicos.ObterPorIdAsync(id, ct));

    /// <summary>Cadastra um serviço. Nome único; preço e tempo padrão são obrigatórios.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServicoResponse>> Cadastrar(CadastrarServicoRequest request,
        CancellationToken ct)
    {
        var servico = await servicos.CadastrarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = servico.Id }, servico);
    }

    /// <summary>Altera nome, descrição e tempo padrão do serviço.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServicoResponse>> Alterar(Guid id, AlterarServicoRequest request,
        CancellationToken ct) =>
        Ok(await servicos.AlterarAsync(id, request, ct));

    /// <summary>Altera o preço criando uma nova vigência.</summary>
    [HttpPatch("{id:guid}/preco")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ServicoResponse>> AlterarPreco(Guid id, AlterarPrecoRequest request,
        CancellationToken ct) =>
        Ok(await servicos.AlterarPrecoAsync(id, request, ct));

    /// <summary>Inativa o serviço. Serviço inativo não entra em novos diagnósticos.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct)
    {
        await servicos.InativarAsync(id, ct);
        return NoContent();
    }
}

/// <summary>CRUD do catálogo de peças e insumos.</summary>
[ApiController]
[Route("api/pecas")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Interno)]
public class PecasController(PecaService pecas) : ControllerBase
{
    /// <summary>Lista as peças do catálogo.</summary>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<PecaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<PecaResponse>>> Listar(
        [FromQuery] bool? ativa, [FromQuery] Paginacao paginacao, CancellationToken ct) =>
        Ok(await pecas.ListarAsync(ativa, paginacao, ct));

    /// <summary>Detalha uma peça, com o histórico de preços.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PecaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PecaResponse>> ObterPorId(Guid id, CancellationToken ct) =>
        Ok(await pecas.ObterPorIdAsync(id, ct));

    /// <summary>Cadastra uma peça.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<PecaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PecaResponse>> Cadastrar(CadastrarPecaRequest request, CancellationToken ct)
    {
        var peca = await pecas.CadastrarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = peca.Id }, peca);
    }

    /// <summary>Altera a descrição da peça.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<PecaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PecaResponse>> Alterar(Guid id, AlterarPecaRequest request,
        CancellationToken ct) =>
        Ok(await pecas.AlterarAsync(id, request, ct));

    /// <summary>Altera o preço criando uma nova vigência.</summary>
    [HttpPatch("{id:guid}/preco")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<PecaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PecaResponse>> AlterarPreco(Guid id, AlterarPrecoRequest request,
        CancellationToken ct) =>
        Ok(await pecas.AlterarPrecoAsync(id, request, ct));

    /// <summary>Inativa a peça.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct)
    {
        await pecas.InativarAsync(id, ct);
        return NoContent();
    }
}
