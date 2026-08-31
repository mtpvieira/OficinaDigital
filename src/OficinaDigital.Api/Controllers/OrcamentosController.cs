using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Orcamentos;

namespace OficinaDigital.Api.Controllers;

/// <summary>Orçamentos da OS: envio, alteração versionada, resposta do cliente e expiração.</summary>
[ApiController]
[Route("api/orcamentos")]
[Produces("application/json")]
public class OrcamentosController(OrcamentoService orcamentos, IUsuarioAtual usuarioAtual) : ControllerBase
{
    /// <summary>Detalha o orçamento, com todas as versões e a vigente marcada.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Politicas.Interno)]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrcamentoResponse>> ObterPorId(Guid id, CancellationToken ct) =>
        Ok(await orcamentos.ObterPorIdAsync(id, ct));

    /// <summary>Lista os orçamentos de uma OS: o principal e os complementares.</summary>
    [HttpGet("da-os/{ordemDeServicoId:guid}")]
    [Authorize(Policy = Politicas.Interno)]
    [ProducesResponseType<IReadOnlyList<OrcamentoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrcamentoResponse>>> ListarPorOs(Guid ordemDeServicoId,
        CancellationToken ct) =>
        Ok(await orcamentos.ListarPorOsAsync(ordemDeServicoId, ct));

    /// <summary>Envia o orçamento ao cliente.</summary>
    [HttpPost("{id:guid}/envio")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrcamentoResponse>> Enviar(Guid id, CancellationToken ct) =>
        Ok(await orcamentos.EnviarAsync(id, ct));

    /// <summary>Altera o orçamento recalculando os itens com os preços do dia.</summary>
    [HttpPost("{id:guid}/alteracao")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrcamentoResponse>> Alterar(Guid id, CancellationToken ct) =>
        Ok(await orcamentos.AlterarAsync(id, ct));

    /// <summary>Resposta do cliente ao orçamento — aprovar ou reprovar.</summary>
    [HttpPost("{id:guid}/resposta")]
    [Authorize]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrcamentoResponse>> Responder(Guid id, ResponderOrcamentoRequest request,
        CancellationToken ct) =>
        Ok(await orcamentos.ResponderAsync(id, request, usuarioAtual.ClienteId, ct));

}
