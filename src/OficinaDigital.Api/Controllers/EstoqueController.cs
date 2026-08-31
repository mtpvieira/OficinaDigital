using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Estoque;

namespace OficinaDigital.Api.Controllers;

/// <summary>Controle de estoque de peças e insumos: entrada, disponibilidade e alertas de estoque mínimo.</summary>
[ApiController]
[Route("api/estoque")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Interno)]
public class EstoqueController(EstoqueService estoque) : ControllerBase
{
    /// <summary>Listagem de estoque: saldo, reservado e disponível por peça.</summary>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<ItemEstoqueResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<ItemEstoqueResponse>>> Listar(
        [FromQuery] Paginacao paginacao, CancellationToken ct) =>
        Ok(await estoque.ListarAsync(paginacao, ct));

    /// <summary>Alertas de estoque mínimo: peças com disponível abaixo do ponto de reposição.</summary>
    [HttpGet("alertas")]
    [ProducesResponseType<IReadOnlyList<ItemEstoqueResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItemEstoqueResponse>>> Alertas(CancellationToken ct) =>
        Ok(await estoque.ListarAlertasDeEstoqueMinimoAsync(ct));

    /// <summary>Detalha a posição de estoque de uma peça, com reservas e movimentos.</summary>
    [HttpGet("pecas/{pecaId:guid}")]
    [ProducesResponseType<ItemEstoqueDetalhadoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemEstoqueDetalhadoResponse>> ObterPorPeca(Guid pecaId,
        CancellationToken ct) =>
        Ok(await estoque.ObterPorPecaAsync(pecaId, ct));

    /// <summary>Consulta de disponibilidade: informe as quantidades desejadas por peça e receba o que o estoque consegue atender.</summary>
    [HttpPost("disponibilidade")]
    [ProducesResponseType<IReadOnlyList<DisponibilidadeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DisponibilidadeDto>>> ConsultarDisponibilidade(
        Dictionary<Guid, decimal> quantidadesPorPeca, CancellationToken ct) =>
        Ok(await estoque.ConsultarDisponibilidadeAsync(quantidadesPorPeca, ct));

    /// <summary>Registra a entrada (recebimento) de peças.</summary>
    [HttpPost("pecas/{pecaId:guid}/entradas")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<ItemEstoqueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemEstoqueResponse>> RegistrarEntrada(Guid pecaId,
        RegistrarEntradaRequest request, CancellationToken ct) =>
        Ok(await estoque.RegistrarEntradaAsync(pecaId, request, ct));

    /// <summary>Define o ponto de estoque mínimo (reposição) da peça.</summary>
    [HttpPut("pecas/{pecaId:guid}/estoque-minimo")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<ItemEstoqueResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemEstoqueResponse>> DefinirEstoqueMinimo(Guid pecaId,
        DefinirEstoqueMinimoRequest request, CancellationToken ct) =>
        Ok(await estoque.DefinirEstoqueMinimoAsync(pecaId, request, ct));

}
