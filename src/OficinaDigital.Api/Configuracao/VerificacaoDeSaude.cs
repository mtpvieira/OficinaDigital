namespace OficinaDigital.Api.Configuracao;

public static class VerificacaoDeSaude
{
    private const int Saudavel = 0;
    private const int NaoSaudavel = 1;

    public static async Task<int> ExecutarAsync()
    {
        var porta = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";
        var endereco = $"http://localhost:{porta}/health";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var resposta = await http.GetAsync(endereco);

            if (resposta.IsSuccessStatusCode) return Saudavel;

            await Console.Error.WriteLineAsync($"Health check falhou: HTTP {(int)resposta.StatusCode}.");
            return NaoSaudavel;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Health check falhou ao consultar {endereco}: {ex.Message}");
            return NaoSaudavel;
        }
    }
}
