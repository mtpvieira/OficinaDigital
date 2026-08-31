using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Atendimento;
using OficinaDigital.Application.Common;

namespace OficinaDigital.Api.Controllers;

/// <summary>API do app do cliente: acompanhamento em tempo real das OS dos próprios veículos.</summary>
[ApiController]
[Route("api/minhas-ordens")]
[Produces("application/json")]
[Authorize(Policy = Politicas.Cliente)]
public class AcompanhamentoController(AcompanhamentoService acompanhamento, IUsuarioAtual usuarioAtual)
    : ControllerBase
{
    /// <summary>Lista as ordens de serviço dos veículos do cliente autenticado.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AcompanhamentoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AcompanhamentoResponse>>> Listar(CancellationToken ct) =>
        Ok(await acompanhamento.ListarDoClienteAsync(ClienteDoToken(), ct));

    /// <summary>Acompanha o andamento de uma OS: status, itens, prazo e se há orçamento esperando resposta.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AcompanhamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AcompanhamentoResponse>> Obter(Guid id, CancellationToken ct) =>
        Ok(await acompanhamento.ObterAsync(id, ClienteDoToken(), ct));

    private Domain.Common.ClienteId ClienteDoToken() =>
        usuarioAtual.ClienteId
        ?? throw new AcessoNegadoException("Este endpoint exige um token de cliente.");
}
