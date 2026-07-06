using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services;
using FHOOE.Freydis.GraphQLServer.Types;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Operations;

/// <summary>
///     GraphQL query operations for retrieving data from the system.
///     Uses application services with direct repository access.
/// </summary>
public class Query
{
    public async Task<List<Agent>> GetAgentsAsync(
        [Service] IAgentApplicationService agentService)
    {
        var agents = await agentService.GetAllAgentsAsync();
        return agents.ToList();
    }

    public async Task<Agent?> GetAgentByIdAsync(Guid id,
        [Service] IAgentApplicationService agentService)
    {
        return await agentService.GetAgentByIdAsync(id);
    }

    // Skill queries - using simplified service
    public async Task<List<Skill>> GetSkillsAsync(
        [Service] ISkillApplicationService skillService)
    {
        var skills = await skillService.GetAllSkillsAsync();
        return skills.ToList();
    }

    public async Task<Skill?> GetSkillByIdAsync(Guid id,
        [Service] ISkillApplicationService skillService)
    {
        return await skillService.GetSkillByIdAsync(id);
    }

    // SceneObject queries - using simplified service
    public async Task<List<SceneObject>> GetSceneObjectsAsync(
        [Service] ISceneObjectApplicationService sceneObjectService)
    {
        var sceneObjects = await sceneObjectService.GetAllSceneObjectsAsync();
        return sceneObjects.ToList();
    }

    public async Task<SceneObject?> GetSceneObjectByIdAsync(Guid id,
        [Service] ISceneObjectApplicationService sceneObjectService)
    {
        return await sceneObjectService.GetSceneObjectByIdAsync(id);
    }

    // PositionTag queries - using simplified service
    public async Task<List<PositionTag>> GetPositionTagsAsync(
        [Service] IPositionTagApplicationService positionTagService)
    {
        var positionTags = await positionTagService.GetAllPositionTagsAsync();
        return positionTags.ToList();
    }

    public async Task<PositionTag?> GetPositionTagByIdAsync(Guid id,
        [Service] IPositionTagApplicationService positionTagService)
    {
        return await positionTagService.GetPositionTagByIdAsync(id);
    }

    // DependencyEdge queries - using simplified service
    public async Task<List<DependencyEdge>> GetDependencyEdgesAsync(
        [Service] IDependencyEdgeApplicationService edgeService)
    {
        var edges = await edgeService.GetAllDependencyEdgesAsync();
        return edges.ToList();
    }

    public async Task<DependencyEdge?> GetDependencyEdgeByIdAsync(Guid id,
        [Service] IDependencyEdgeApplicationService edgeService)
    {
        return await edgeService.GetDependencyEdgeByIdAsync(id);
    }

    // Node queries - using simplified service
    public async Task<List<Node>> GetNodesAsync(
        [Service] INodeApplicationService nodeService)
    {
        var nodes = await nodeService.GetAllNodesAsync();
        return nodes.ToList();
    }

    public async Task<Node?> GetNodeByIdAsync(Guid id,
        [Service] INodeApplicationService nodeService)
    {
        return await nodeService.GetNodeByIdAsync(id);
    }

    // Runtime agent queries - these were missing and causing the schema validation to fail
    public async Task<List<RuntimeAgentInfo>> GetRuntimeAgentsAsync(
        [Service] RuntimeAgentService runtimeAgentService)
    {
        return await runtimeAgentService.GetAllRuntimeAgentsAsync();
    }

    public async Task<RuntimeAgentInfo?> GetRuntimeAgentByIdAsync(Guid agentId,
        [Service] RuntimeAgentService runtimeAgentService)
    {
        return await runtimeAgentService.GetRuntimeAgentByIdAsync(agentId);
    }

    public async Task<RuntimeAgentInfo?> GetRuntimeAgentByNameAsync(string agentName,
        [Service] RuntimeAgentService runtimeAgentService)
    {
        return await runtimeAgentService.GetRuntimeAgentByNameAsync(agentName);
    }

    // Legacy runtime agent queries - keeping for backward compatibility
    public Task<List<IRuntimeAgent>> GetAvailableRuntimeAgentsAsync(
        [Service] IRuntimeAgentProvider runtimeAgentProvider)
    {
        return Task.FromResult(runtimeAgentProvider.GetAvailableRuntimeAgents().ToList());
    }

    public Task<IRuntimeAgent?> GetRuntimeAgentByIdAsync(Guid id,
        [Service] IRuntimeAgentProvider runtimeAgentProvider)
    {
        return Task.FromResult(runtimeAgentProvider.GetRuntimeAgent(id));
    }

    // Execution state queries
    /// <summary>
    ///     Gets the current execution state of all skills.
    /// </summary>
    public Task<ExecutionStateDto> GetExecutionStateAsync(
        [Service] ISkillExecutionStateManager stateManager)
    {
        var states = stateManager.GetAllStates();
        return Task.FromResult(ExecutionStateDto.FromSkillExecutionStates(states));
    }

    /// <summary>
    ///     Gets the execution state for a specific skill.
    /// </summary>
    public Task<SkillExecutionStateDto?> GetSkillExecutionStateAsync(Guid skillId,
        [Service] ISkillExecutionStateManager stateManager)
    {
        var state = stateManager.GetState(skillId);
        return Task.FromResult(state != null
            ? SkillExecutionStateDto.FromSkillExecutionState(state)
            : null);
    }

    /// <summary>
    ///     Gets all skill execution states with a specific status.
    /// </summary>
    public Task<List<SkillExecutionStateDto>> GetSkillExecutionStatesByStatusAsync(ExecutionStatus status,
        [Service] ISkillExecutionStateManager stateManager)
    {
        var states = stateManager.GetStatesByStatus(status);
        return Task.FromResult(states.Select(SkillExecutionStateDto.FromSkillExecutionState)
            .ToList());
    }

    // Procedure queries
    /// <summary>
    ///     Gets all procedures from the system.
    /// </summary>
    public async Task<List<Procedure>> GetProceduresAsync(
        [Service] IRepository<Procedure> procedureRepository)
    {
        return await procedureRepository.GetAllAsync();
    }

    /// <summary>
    ///     Gets a specific procedure by its unique identifier.
    /// </summary>
    public async Task<Procedure?> GetProcedureByIdAsync(Guid id,
        [Service] IRepository<Procedure> procedureRepository)
    {
        return await procedureRepository.GetByIdAsync(id);
    }

    /// <summary>
    ///     Gets the currently loaded/active procedure, if any.
    /// </summary>
    public async Task<Procedure?> GetLoadedProcedureAsync(
        [Service] IProcedureOrchestrator procedureOrchestrator)
    {
        return await procedureOrchestrator.GetLoadedProcedureAsync();
    }

    /// <summary>
    ///     Gets the scheduling configuration used by the backend for positioning and sizing calculations.
    ///     This allows the frontend to synchronize its rendering with backend calculations.
    /// </summary>
    public Task<SchedulingConfigurationDto> GetSchedulingConfigurationAsync(
        [Service] IOptions<SchedulingConfiguration> schedulingOptions)
    {
        var config = schedulingOptions.Value.Positioning;
        var dto = new SchedulingConfigurationDto
        {
            TimeToPixelScale = config.TimeToPixelScale,
            BaseYOffset = config.BaseYOffset,
            SiblingSpacing = config.SiblingSpacing,
            ContainerTopPadding = config.ContainerTopPadding,
            ContainerBottomPadding = config.ContainerBottomPadding,
            BaseHeight = config.BaseHeight,
            RouterDropdownHeight = config.RouterDropdownHeight
        };
        return Task.FromResult(dto);
    }
}