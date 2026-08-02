namespace OficinaDigital.Application.OrdensServico;

public interface IOrdemServicoService
{
    Task<OrdemServicoDto> CriarAsync(CriarOrdemServicoRequest request, CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrdemServicoDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> IniciarDiagnosticoAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> EnviarOrcamentoAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> AprovarAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> ConcluirExecucaoAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> EntregarAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TempoMedioExecucaoDto> ObterTempoMedioExecucaoAsync(CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> ConsultarPublicoAsync(string numero, string documento, CancellationToken cancellationToken = default);

    Task<OrdemServicoDto> AprovarPublicoAsync(string numero, AprovarPublicoRequest request, CancellationToken cancellationToken = default);
}
