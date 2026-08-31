using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Cadastro;
using OficinaDigital.Application.Common;

namespace OficinaDigital.Api.Controllers;

/// <summary>CRUD de clientes e identificação por CPF/CNPJ.</summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Interno)]
public class ClientesController(ClienteService clientes) : ControllerBase
{
    /// <summary>Lista clientes, com filtro por nome e por situação.</summary>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<ClienteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<ClienteResponse>>> Listar(
        [FromQuery] string? nome, [FromQuery] bool? ativo, [FromQuery] Paginacao paginacao,
        CancellationToken ct) =>
        Ok(await clientes.ListarAsync(nome, ativo, paginacao, ct));

    /// <summary>Detalha um cliente.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> ObterPorId(Guid id, CancellationToken ct) =>
        Ok(await clientes.ObterPorIdAsync(id, ct));

    /// <summary>Identifica o cliente por CPF/CNPJ — o primeiro passo da abertura da OS.</summary>
    [HttpGet("por-documento/{documento}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> IdentificarPorDocumento(string documento,
        CancellationToken ct)
    {
        var cliente = await clientes.IdentificarPorDocumentoAsync(documento, ct);

        return cliente is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Cliente não encontrado",
                Detail = "Nenhum cliente cadastrado com este CPF/CNPJ. Cadastre-o para abrir a OS."
            })
            : Ok(cliente);
    }

    /// <summary>Cadastra um cliente. O CPF/CNPJ é validado no dígito verificador e é único.</summary>
    [HttpPost]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteResponse>> Cadastrar(CadastrarClienteRequest request,
        CancellationToken ct)
    {
        var cliente = await clientes.CadastrarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, cliente);
    }

    /// <summary>Altera os dados cadastrais do cliente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> Alterar(Guid id, AlterarClienteRequest request,
        CancellationToken ct) =>
        Ok(await clientes.AlterarAsync(id, request, ct));

    /// <summary>Inativa o cliente.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct)
    {
        await clientes.InativarAsync(id, ct);
        return NoContent();
    }
}
