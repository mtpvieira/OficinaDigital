namespace OficinaDigital.Infrastructure.Seguranca;

public class JwtOptions
{
    public const string Secao = "Jwt";

    public const int TamanhoMinimoDaChave = 32;

    public string Emissor { get; set; } = "OficinaDigital";
    public string Audiencia { get; set; } = "OficinaDigital.Api";
    public string ChaveSecreta { get; set; } = string.Empty;
    public int ExpiracaoEmMinutos { get; set; } = 60;

    public int ExpiracaoClienteEmMinutos { get; set; } = 30;

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(ChaveSecreta))
            throw new InvalidOperationException(
                "Jwt:ChaveSecreta não configurada. Defina em appsettings ou na variável de ambiente Jwt__ChaveSecreta.");

        if (ChaveSecreta.Length < TamanhoMinimoDaChave)
            throw new InvalidOperationException(
                $"Jwt:ChaveSecreta precisa ter ao menos {TamanhoMinimoDaChave} caracteres.");
    }
}
