namespace OficinaDigital.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string ChaveSecreta { get; set; } = "D>Ç;:euxn7r?q/vWp}RPM?XU6Hz=VZ2$hQnsq.T8*]?HK~!gvA";
    public string Emissor { get; set; } = "OficinaDigital";
    public string Audiencia { get; set; } = "OficinaDigital.Api";
    public int ExpiracaoMinutos { get; set; } = 60;
}
