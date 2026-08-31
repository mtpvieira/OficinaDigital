using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Atendimento;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Atendimento.ValueObjects;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Api.Controllers;

/// <summary>Ciclo completo da Ordem de Serviço, do recebimento do veículo à entrega.</summary>
[ApiController]
[Route("api/ordens-servico")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Interno)]
public class OrdensDeServicoController(OrdemDeServicoService ordens) : ControllerBase
{
    /// <summary>Listagem de OS, com filtro por status, cliente e veículo.</summary>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<OrdemDeServicoResumoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<OrdemDeServicoResumoResponse>>> Listar(
        [FromQuery] StatusOS? status, [FromQuery] Guid? clienteId, [FromQuery] Guid? veiculoId,
        [FromQuery] Paginacao paginacao, CancellationToken ct) =>
        Ok(await ordens.ListarAsync(status, clienteId, veiculoId, paginacao, ct));

    /// <summary>Painel de OS por status.</summary>
    [HttpGet("painel")]
    [ProducesResponseType<IReadOnlyDictionary<string, int>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyDictionary<string, int>>> Painel(CancellationToken ct) =>
        Ok(await ordens.ContarPorStatusAsync(ct));

    /// <summary>Detalhamento da OS: itens, prazos, execução, orçamento e pagamento.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrdemDeServicoResponse>> ObterPorId(Guid id, CancellationToken ct) =>
        Ok(await ordens.ObterPorIdAsync(id, ct));

    /// <summary>Detalhamento pelo número sequencial da OS.</summary>
    [HttpGet("por-numero/{numero:long}")]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrdemDeServicoResponse>> ObterPorNumero(long numero, CancellationToken ct) =>
        Ok(await ordens.ObterPorNumeroAsync(numero, ct));

    /// <summary>Abre a OS para um cliente e um veículo já identificados.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrdemDeServicoResponse>> Abrir(AbrirOsRequest request, CancellationToken ct)
    {
        var os = await ordens.AbrirAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = os.Id }, os);
    }

    /// <summary>Inicia o diagnóstico. Status passa a Em diagnóstico.</summary>
    [HttpPost("{id:guid}/diagnostico/iniciar")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> IniciarDiagnostico(Guid id, CancellationToken ct) =>
        Ok(await ordens.IniciarDiagnosticoAsync(id, ct));

    /// <summary>Registra os serviços necessários.</summary>
    [HttpPost("{id:guid}/servicos")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> RegistrarServicos(Guid id,
        RegistrarServicosRequest request, CancellationToken ct) =>
        Ok(await ordens.RegistrarServicosAsync(id, request, ct));

    /// <summary>Remove um item de serviço durante o diagnóstico.</summary>
    [HttpDelete("{id:guid}/servicos/{itemId:guid}")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrdemDeServicoResponse>> RemoverServico(Guid id, Guid itemId,
        CancellationToken ct) =>
        Ok(await ordens.RemoverServicoAsync(id, itemId, ct));

    /// <summary>Registra as peças necessárias.</summary>
    [HttpPost("{id:guid}/pecas")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> RegistrarPecas(Guid id,
        RegistrarPecasRequest request, CancellationToken ct) =>
        Ok(await ordens.RegistrarPecasAsync(id, request, ct));

    /// <summary>Remove um item de peça durante o diagnóstico.</summary>
    [HttpDelete("{id:guid}/pecas/{itemId:guid}")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrdemDeServicoResponse>> RemoverPeca(Guid id, Guid itemId,
        CancellationToken ct) =>
        Ok(await ordens.RemoverPecaAsync(id, itemId, ct));

    /// <summary>Finaliza o diagnóstico.</summary>
    [HttpPost("{id:guid}/diagnostico/finalizar")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> FinalizarDiagnostico(Guid id, CancellationToken ct) =>
        Ok(await ordens.FinalizarDiagnosticoAsync(id, ct));

    /// <summary>Inicia o serviço.</summary>
    [HttpPost("{id:guid}/execucao/iniciar")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrdemDeServicoResponse>> IniciarServico(Guid id, CancellationToken ct) =>
        Ok(await ordens.IniciarServicoAsync(id, ct));

    /// <summary>Registra um problema adicional encontrado durante a execução.</summary>
    [HttpPost("{id:guid}/problemas-adicionais")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrdemDeServicoResponse>> RegistrarProblemaAdicional(Guid id,
        RegistrarProblemaAdicionalRequest request, CancellationToken ct) =>
        Ok(await ordens.RegistrarProblemaAdicionalAsync(id, request, ct));

    /// <summary>Registra serviços e peças adicionais.</summary>
    [HttpPost("{id:guid}/itens-adicionais")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> RegistrarItensAdicionais(Guid id,
        RegistrarItensAdicionaisRequest request, CancellationToken ct) =>
        Ok(await ordens.RegistrarItensAdicionaisAsync(id, request, ct));

    /// <summary>Conclui o serviço.</summary>
    [HttpPost("{id:guid}/execucao/finalizar")]
    [Authorize(Policy = Politicas.Mecanico)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> FinalizarServico(Guid id, CancellationToken ct) =>
        Ok(await ordens.FinalizarServicoAsync(id, ct));

    /// <summary>Registra a entrega do veículo. Status passa a Entregue.</summary>
    [HttpPost("{id:guid}/entrega")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> RegistrarEntrega(Guid id, CancellationToken ct) =>
        Ok(await ordens.RegistrarEntregaAsync(id, ct));

    /// <summary>Cancela a OS.</summary>
    [HttpPost("{id:guid}/cancelamento")]
    [Authorize(Policy = Politicas.Atendente)]
    [ProducesResponseType<OrdemDeServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrdemDeServicoResponse>> Cancelar(Guid id, CancelarOsRequest request,
        CancellationToken ct) =>
        Ok(await ordens.CancelarAsync(id, request, ct));
}
