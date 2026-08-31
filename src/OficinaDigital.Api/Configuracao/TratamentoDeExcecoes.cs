using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OficinaDigital.Application.Common;
using OficinaDigital.Domain.Common;

namespace OficinaDigital.Api.Configuracao;

public class TratamentoDeExcecoesMiddleware(RequestDelegate proximo, ILogger<TratamentoDeExcecoesMiddleware> logger,
    IHostEnvironment ambiente)
{
    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await proximo(contexto);
        }
        catch (Exception ex)
        {
            await EscreverProblemaAsync(contexto, ex);
        }
    }

    private async Task EscreverProblemaAsync(HttpContext contexto, Exception excecao)
    {
        var correlacao = contexto.TraceIdentifier;

        var (status, titulo, detalhe) = excecao switch
        {
            DomainException dom =>
                (StatusCodes.Status400BadRequest, "Regra de negócio violada", dom.Message),

            RecursoNaoEncontradoException nf =>
                (StatusCodes.Status404NotFound, "Recurso não encontrado", nf.Message),

            ConflitoDeNegocioException cf =>
                (StatusCodes.Status409Conflict, "Conflito com o estado atual", cf.Message),

            CredencialInvalidaException ci =>
                (StatusCodes.Status401Unauthorized, "Não autenticado", ci.Message),

            AcessoNegadoException ac =>
                (StatusCodes.Status403Forbidden, "Acesso negado", ac.Message),

            _ => (StatusCodes.Status500InternalServerError, "Erro interno",
                ambiente.IsDevelopment()
                    ? excecao.ToString()
                    : "Ocorreu um erro inesperado. Informe o identificador de correlação ao suporte.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(excecao, "Erro não tratado. Correlação: {Correlacao}", correlacao);
        else
            logger.LogInformation("Requisição rejeitada ({Status}): {Mensagem}", status, excecao.Message);

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = contexto.Request.Path
        };
        problema.Extensions["correlacao"] = correlacao;

        contexto.Response.Clear();
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/problem+json";

        await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcoesJson));
    }
}

public static class TratamentoDeExcecoesExtensions
{
    public static IApplicationBuilder UseTratamentoDeExcecoes(this IApplicationBuilder app) =>
        app.UseMiddleware<TratamentoDeExcecoesMiddleware>();
}
