namespace OficinaDigital.Api.Configuracao;

public class CabecalhosDeSegurancaMiddleware(RequestDelegate proximo)
{
    public Task InvokeAsync(HttpContext contexto)
    {
        var cabecalhos = contexto.Response.Headers;

        cabecalhos["X-Content-Type-Options"] = "nosniff";

        cabecalhos["X-Frame-Options"] = "DENY";

        cabecalhos["Referrer-Policy"] = "no-referrer";

        cabecalhos["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        cabecalhos.Remove("Server");

        return proximo(contexto);
    }
}

public static class CabecalhosDeSegurancaExtensions
{
    public static IApplicationBuilder UseCabecalhosDeSeguranca(this IApplicationBuilder app) =>
        app.UseMiddleware<CabecalhosDeSegurancaMiddleware>();
}
