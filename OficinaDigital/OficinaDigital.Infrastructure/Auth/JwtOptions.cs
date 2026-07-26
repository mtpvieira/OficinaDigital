namespace OficinaDigital.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string ChaveSecreta { get; set; } = "chave-de-desenvolvimento-oficina-digital-nao-usar-em-producao";
    public string Emissor { get; set; } = "OficinaDigital";
    public string Audiencia { get; set; } = "OficinaDigital.Api";
    public int ExpiracaoMinutos { get; set; } = 60;
}
