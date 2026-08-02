namespace OficinaDigital.Infrastructure.Auth;

public sealed class AdminCredentialsOptions
{
    public const string SectionName = "Admin";

    public string Usuario { get; set; } = "admin";
    public string Senha { get; set; } = "OficinaDigital@2026";
}
