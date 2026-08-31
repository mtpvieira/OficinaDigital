using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Cadastro;
using OficinaDigital.Application.Common;

namespace OficinaDigital.Api.Controllers;

/// <summary>CRUD de veículos, identificação por placa e transferência de proprietário.</summary>
[ApiController]
[Route("api/veiculos")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Interno)]
public class VeiculosController(VeiculoService veiculos) : ControllerBase
{
    /// <summary>Lista veículos.</summary>
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<VeiculoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<VeiculoResponse>>> Listar(
        [FromQuery] Paginacao paginacao, CancellationToken ct) =>
        Ok(await veiculos.ListarAsync(paginacao, ct));

    /// <summary>Detalha um veículo.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeiculoResponse>> ObterPorId(Guid id, CancellationToken ct) =>
        Ok(await veiculos.ObterPorIdAsync(id, ct));

    /// <summary>Lista os veículos de um cliente.</summary>
    [HttpGet("do-cliente/{clienteId:guid}")]
    [ProducesResponseType<IReadOnlyList<VeiculoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VeiculoResponse>>> ListarPorCliente(Guid clienteId,
        CancellationToken ct) =>
        Ok(await veiculos.ListarPorClienteAsync(clienteId, ct));

    /// <summary>Identifica o veículo pela placa — aceita o padrão antigo (AAA0000) e o Mercosul (AAA0A00).</summary>
    [HttpGet("por-placa/{placa}")]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeiculoResponse>> IdentificarPorPlaca(string placa, CancellationToken ct)
    {
        var veiculo = await veiculos.IdentificarPorPlacaAsync(placa, ct);

        return veiculo is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Veículo não encontrado",
                Detail = "Nenhum veículo cadastrado com esta placa. Cadastre-o para abrir a OS."
            })
            : Ok(veiculo);
    }

    /// <summary>Cadastra um veículo. A placa é única e imutável.</summary>
    [HttpPost]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VeiculoResponse>> Cadastrar(CadastrarVeiculoRequest request,
        CancellationToken ct)
    {
        var veiculo = await veiculos.CadastrarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = veiculo.Id }, veiculo);
    }

    /// <summary>Altera os dados do veículo. A placa não muda.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeiculoResponse>> Alterar(Guid id, AlterarVeiculoRequest request,
        CancellationToken ct) =>
        Ok(await veiculos.AlterarAsync(id, request, ct));

}
