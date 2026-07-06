using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Types.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services.Mappers;

/// <summary>
///     Minimal tests to verify GraphQLMapperService correctly uses IProcedureContext
///     instead of IProcedureOrchestrator.
/// </summary>
public class GraphQlMapperServiceContextTests
{
    [Fact]
    public async Task MapToDependencyEdge_UsesIProcedureContext_RequireCurrentProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var mockSkillService = new Mock<ISkillApplicationService>();
        var mockAgentService = new Mock<IAgentApplicationService>();
        var mockProcedureContext = new Mock<IProcedureContext>();

        mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var sut = new GraphQlMapperService(
            mockSkillService.Object,
            mockAgentService.Object,
            mockProcedureContext.Object,
            NullLogger<GraphQlMapperService>.Instance);

        var dto = new DependencyEdgeDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "output",
            "input"
        );

        // Act
        var result = await sut.MapToDependencyEdgeAsync(dto);

        // Assert
        Assert.Equal(procedureId, result.ProcedureId);
        mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    [Fact]
    public async Task MapToDependencyEdge_WhenNoProcedureLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockSkillService = new Mock<ISkillApplicationService>();
        var mockAgentService = new Mock<IAgentApplicationService>();
        var mockProcedureContext = new Mock<IProcedureContext>();

        mockProcedureContext
            .Setup(x => x.RequireCurrentProcedureId())
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        var sut = new GraphQlMapperService(
            mockSkillService.Object,
            mockAgentService.Object,
            mockProcedureContext.Object,
            NullLogger<GraphQlMapperService>.Instance);

        var dto = new DependencyEdgeDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null
        );

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.MapToDependencyEdgeAsync(dto));

        Assert.Contains("No procedure is currently loaded", ex.Message);
        mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }
}