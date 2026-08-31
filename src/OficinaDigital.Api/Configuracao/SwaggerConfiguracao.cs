using System.Reflection;
using Microsoft.OpenApi;

namespace OficinaDigital.Api.Configuracao;

public static class SwaggerConfiguracao
{
    private const string EsquemaJwt = "Bearer";

    public static IServiceCollection AddSwaggerDaOficina(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(opcoes =>
        {
            opcoes.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Oficina Digital — Sistema Integrado de Atendimento e Execução de Serviços",
                Version = "v1",
                Description =
                    """
                    MVP do back-end da oficina mecânica: gestão de ordens de serviço, clientes,
                    veículos, catálogo e estoque de peças, com orçamento versionado e aprovação
                    pelo cliente.

                    **Como usar**
                    1. Autentique em `POST /api/auth/login` com um usuário interno.
                    2. Clique em *Authorize* e informe o token.
                    3. O cliente final autentica em `POST /api/auth/login-cliente` (CPF/CNPJ + placa)
                       e enxerga apenas as OS dos seus próprios veículos.

                    **Perfis**
                    - `ATENDENTE`: abertura da OS, orçamento, pagamento, entrega e cadastros.
                    - `MECANICO`: diagnóstico e execução.
                    - `ADMINISTRADOR`: tudo, mais a gestão de usuários.
                    - `CLIENTE`: acompanhamento e resposta de orçamento das próprias OS.
                    """
            });

            opcoes.AddSecurityDefinition(EsquemaJwt, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe apenas o token JWT (o prefixo Bearer é adicionado automaticamente)."
            });

            opcoes.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(EsquemaJwt, documento)] = []
            });

            var xml = Path.Combine(AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

            if (File.Exists(xml)) opcoes.IncludeXmlComments(xml);
        });

        return services;
    }
}
