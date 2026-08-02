using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using Moq;
using OficinaDigital.Domain.OrdensServico;
using OficinaDigital.Infrastructure.Persistence;

namespace OficinaDigital.Infrastructure.Tests.Persistence;

public class UnitOfWorkTests
{
    private static OficinaDigitalDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<OficinaDigitalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OficinaDigitalDbContext(options);
    }

    [Fact]
    public async Task SalvarAlteracoesAsync_ComAgregadoContendoEventoDeDominio_DeveLimparEventosAposSalvar()
    {
        await using var context = CriarContexto();
        var loggerMock = new Mock<ILogger<UnitOfWork>>();
        var sut = new UnitOfWork(context, loggerMock.Object);

        var ordemDeServico = OrdemDeServico.Criar("OS-TESTE-0001", Guid.NewGuid(), Guid.NewGuid());
        Assert.NotEmpty(ordemDeServico.DomainEvents);

        await context.OrdensServico.AddAsync(ordemDeServico);

        await sut.SalvarAlteracoesAsync();
        
        Assert.Empty(ordemDeServico.DomainEvents);
    }

    [Fact]
    public async Task SalvarAlteracoesAsync_ComAgregadoContendoEventoDeDominio_DeveRegistrarLogDeInformacao()
    {
        await using var context = CriarContexto();
        var loggerMock = new Mock<ILogger<UnitOfWork>>();
        var sut = new UnitOfWork(context, loggerMock.Object);

        var ordemDeServico = OrdemDeServico.Criar("OS-TESTE-0002", Guid.NewGuid(), Guid.NewGuid());
        await context.OrdensServico.AddAsync(ordemDeServico);

        await sut.SalvarAlteracoesAsync();

        loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(nameof(OSCriada))),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SalvarAlteracoesAsync_SemAlteracoesPendentes_NaoDeveRegistrarNenhumLog()
    {
        await using var context = CriarContexto();
        var loggerMock = new Mock<ILogger<UnitOfWork>>();
        var sut = new UnitOfWork(context, loggerMock.Object);

        await sut.SalvarAlteracoesAsync();

        loggerMock.Verify(
            logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
