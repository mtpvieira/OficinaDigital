using Microsoft.Extensions.DependencyInjection;
using OficinaDigital.Application.Atendimento;
using OficinaDigital.Application.Cadastro;
using OficinaDigital.Application.Catalogo;
using OficinaDigital.Application.Common;
using OficinaDigital.Application.Estoque;
using OficinaDigital.Application.Orcamentos;
using OficinaDigital.Application.Politicas;
using OficinaDigital.Application.Seguranca;
using OficinaDigital.Domain.Atendimento.Events;
using OficinaDigital.Domain.Catalogo.Events;
using OficinaDigital.Domain.Estoque.Events;
using OficinaDigital.Domain.Orcamentos.Events;

namespace OficinaDigital.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClienteService>();
        services.AddScoped<VeiculoService>();
        services.AddScoped<ServicoService>();
        services.AddScoped<PecaService>();
        services.AddScoped<EstoqueService>();
        services.AddScoped<OrdemDeServicoService>();
        services.AddScoped<OrcamentoService>();
        services.AddScoped<AcompanhamentoService>();
        services.AddScoped<IndicadorService>();
        services.AddScoped<AuthService>();

        return services.AddPoliticasDeDominio();
    }

    private static IServiceCollection AddPoliticasDeDominio(this IServiceCollection services)
    {
        services.AddScoped<IPoliticaDeDominio<PecaCadastrada>, AbrirPosicaoDeEstoqueAoCadastrarPeca>();
        services.AddScoped<IPoliticaDeDominio<PecasDaOsRegistradas>, ReservarPecasAoRegistrarNaOs>();
        services.AddScoped<IPoliticaDeDominio<EntradaDePecasRegistrada>, ReservarPecasPendentesAoRegistrarEntrada>();
        services.AddScoped<IPoliticaDeDominio<ServicoIniciado>, SubtrairPecasAoIniciarServico>();
        services.AddScoped<IPoliticaDeDominio<OsCancelada>, LiberarReservasAoCancelarOs>();

        services.AddScoped<IPoliticaDeDominio<DiagnosticoFinalizado>, GerarOrcamentoAoFinalizarDiagnostico>();
        services.AddScoped<IPoliticaDeDominio<ServicosEPecasAdicionaisRegistrados>,
            GerarComplementarAoRegistrarItensAdicionais>();
        services.AddScoped<IPoliticaDeDominio<OrcamentoEnviadoAoCliente>, AtualizarOsAoEnviarOrcamento>();
        services.AddScoped<IPoliticaDeDominio<OrcamentoAprovado>, AoAprovarOrcamento>();
        services.AddScoped<IPoliticaDeDominio<OrcamentoReprovado>, AoReprovarOrcamento>();

        return services;
    }
}
