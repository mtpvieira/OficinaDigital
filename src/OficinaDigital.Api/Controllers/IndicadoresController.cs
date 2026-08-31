using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Atendimento;

namespace OficinaDigital.Api.Controllers;

/// <summary>Indicadores de gestão da oficina.</summary>
[ApiController]
[Route("api/indicadores")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Interno)]
public class IndicadoresController(IndicadorService indicadores) : ControllerBase
{
    /// <summary>Tempo médio de execução por tipo de serviço, comparado ao tempo padrão do catálogo.</summary>
    /// <param name="inicio">Início do período (opcional), pela data de conclusão da OS.</param>
    /// <param name="fim">Fim do período (opcional), pela data de conclusão da OS.</param>
    /// <param name="ct">Token de cancelamento da requisição.</param>
    [HttpGet("tempo-medio-execucao")]
    [ProducesResponseType<PainelDeTempoMedioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PainelDeTempoMedioResponse>> TempoMedioDeExecucao(
        [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken ct) =>
        Ok(await indicadores.ObterTempoMedioDeExecucaoAsync(inicio, fim, ct));
}
