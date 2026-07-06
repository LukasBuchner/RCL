using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FHOOE.Freydis.GraphQLServer.Services;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Services.Validation;
using FHOOE.Freydis.GraphQLServer.Support.Logging;
using FHOOE.Freydis.GraphQLServer.Types;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;

namespace FHOOE.Freydis.GraphQLServer.Operations;

/// <summary>
///     GraphQL mutation operations for modifying data in the system.
///     Uses application services with direct repository access.
/// </summary>
public class Mutation
{
    public async Task<StartLoadedProcedurePayload> StartLoadedProcedureAsync(
        [Service] IExecutionOrchestrator executionOrchestrator,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledSimple("StartLoadedProcedureAsync");

        try
        {
            var result = await executionOrchestrator.StartLoadedProcedureAsync();
            var resultStr = result.ToString();
            logger.LogMutationCompletedWithResult("StartLoadedProcedureAsync", resultStr);
            return new StartLoadedProcedurePayload { Boolean = result };
        }
        catch (ExecutionPreConditionException ex)
        {
            logger.LogMutationRejected("StartLoadedProcedureAsync", ex.Message);
            var builder = ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode(ex.ErrorCode);
            if (ex.StructuredData is not null)
                builder.SetExtension("data", ex.StructuredData);
            throw new GraphQLException(builder.Build());
        }
    }

    public async Task<CreateSceneObjectPayload> CreateSceneObjectAsync(CreateSceneObjectInput input,
        [Service] ISceneObjectApplicationService sceneObjectService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("CreateSceneObjectAsync", input.SceneObject.Id);
        var sceneObject = new SceneObject
        {
            Id = input.SceneObject.Id,
            Name = input.SceneObject.Name,
            Position = input.SceneObject.Position
        };
        var result = await sceneObjectService.CreateSceneObjectAsync(sceneObject);
        logger.LogMutationCompleted("CreateSceneObjectAsync", result.Id);
        return new CreateSceneObjectPayload { SceneObject = result };
    }

    public async Task<UpdateSceneObjectPayload> UpdateSceneObjectAsync(UpdateSceneObjectInput input,
        [Service] ISceneObjectApplicationService sceneObjectService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("UpdateSceneObjectAsync", input.SceneObject.Id);
        var sceneObject = new SceneObject
        {
            Id = input.SceneObject.Id,
            Name = input.SceneObject.Name,
            Position = input.SceneObject.Position
        };
        var result = await sceneObjectService.UpdateSceneObjectAsync(sceneObject);
        logger.LogMutationCompletedWithSuccess("UpdateSceneObjectAsync", input.SceneObject.Id, result);
        return new UpdateSceneObjectPayload { Boolean = result };
    }

    public async Task<DeleteSceneObjectPayload> DeleteSceneObjectAsync(DeleteSceneObjectInput input,
        [Service] ISceneObjectApplicationService sceneObjectService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("DeleteSceneObjectAsync", input.Id);
        var result = await sceneObjectService.DeleteSceneObjectAsync(input.Id);
        logger.LogMutationCompletedWithSuccess("DeleteSceneObjectAsync", input.Id, result);
        return new DeleteSceneObjectPayload { Boolean = result };
    }

    // PositionTag operations - using application service
    public async Task<CreatePositionTagPayload> CreatePositionTagAsync(CreatePositionTagInput input,
        [Service] IPositionTagApplicationService positionTagService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("CreatePositionTagAsync", input.PositionTag.Id);
        var positionTag = new PositionTag
        {
            Id = input.PositionTag.Id,
            Tag = input.PositionTag.Tag,
            Position = input.PositionTag.Position
        };
        var result = await positionTagService.CreatePositionTagAsync(positionTag);
        logger.LogMutationCompleted("CreatePositionTagAsync", result.Id);
        return new CreatePositionTagPayload { PositionTag = result };
    }

    public async Task<UpdatePositionTagPayload> UpdatePositionTagAsync(UpdatePositionTagInput input,
        [Service] IPositionTagApplicationService positionTagService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("UpdatePositionTagAsync", input.PositionTag.Id);
        var positionTag = new PositionTag
        {
            Id = input.PositionTag.Id,
            Tag = input.PositionTag.Tag,
            Position = input.PositionTag.Position
        };
        var result = await positionTagService.UpdatePositionTagAsync(positionTag);
        logger.LogMutationCompletedWithSuccess("UpdatePositionTagAsync", input.PositionTag.Id, result);
        return new UpdatePositionTagPayload { Boolean = result };
    }

    public async Task<DeletePositionTagPayload> DeletePositionTagAsync(DeletePositionTagInput input,
        [Service] IPositionTagApplicationService positionTagService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("DeletePositionTagAsync", input.Id);
        var result = await positionTagService.DeletePositionTagAsync(input.Id);
        logger.LogMutationCompletedWithSuccess("DeletePositionTagAsync", input.Id, result);
        return new DeletePositionTagPayload { Boolean = result };
    }

    // DependencyEdge operations - using simplified service
    public async Task<CreateDependencyEdgePayload> CreateDependencyEdgeAsync(CreateDependencyEdgeInput input,
        [Service] IDependencyEdgeApplicationService edgeService,
        [Service] IDependencyEdgeValidator edgeValidator,
        [Service] IProcedureContext procedureContext,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("CreateDependencyEdgeAsync", input.DependencyEdge.Id);

        await edgeValidator.ValidateAsync(
            input.DependencyEdge.SourceId, input.DependencyEdge.TargetId,
            input.DependencyEdge.SourceHandle, input.DependencyEdge.TargetHandle);

        var procedureId = procedureContext.RequireCurrentProcedureId();

        var dependencyEdge = new DependencyEdge
        {
            Id = input.DependencyEdge.Id,
            ProcedureId = procedureId,
            SourceId = input.DependencyEdge.SourceId,
            TargetId = input.DependencyEdge.TargetId,
            SourceHandle = input.DependencyEdge.SourceHandle,
            TargetHandle = input.DependencyEdge.TargetHandle
        };
        var result = await edgeService.CreateDependencyEdgeAsync(dependencyEdge);
        logger.LogMutationCompleted("CreateDependencyEdgeAsync", result.Id);
        return new CreateDependencyEdgePayload { DependencyEdge = result };
    }

    public async Task<UpdateDependencyEdgePayload> UpdateDependencyEdgeAsync(UpdateDependencyEdgeInput input,
        [Service] IDependencyEdgeApplicationService edgeService,
        [Service] IDependencyEdgeValidator edgeValidator,
        [Service] IProcedureContext procedureContext,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("UpdateDependencyEdgeAsync", input.DependencyEdge.Id);

        await edgeValidator.ValidateAsync(
            input.DependencyEdge.SourceId, input.DependencyEdge.TargetId,
            input.DependencyEdge.SourceHandle, input.DependencyEdge.TargetHandle,
            input.DependencyEdge.Id);

        var procedureId = procedureContext.RequireCurrentProcedureId();

        var dependencyEdge = new DependencyEdge
        {
            Id = input.DependencyEdge.Id,
            ProcedureId = procedureId,
            SourceId = input.DependencyEdge.SourceId,
            TargetId = input.DependencyEdge.TargetId,
            SourceHandle = input.DependencyEdge.SourceHandle,
            TargetHandle = input.DependencyEdge.TargetHandle
        };
        var result = await edgeService.UpdateDependencyEdgeAsync(dependencyEdge);
        logger.LogMutationCompletedWithSuccess("UpdateDependencyEdgeAsync", input.DependencyEdge.Id, result);
        return new UpdateDependencyEdgePayload { Boolean = result };
    }

    public async Task<DeleteDependencyEdgePayload> DeleteDependencyEdgeAsync(DeleteDependencyEdgeInput input,
        [Service] IDependencyEdgeApplicationService edgeService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("DeleteDependencyEdgeAsync", input.Id);
        var result = await edgeService.DeleteDependencyEdgeAsync(input.Id);
        logger.LogMutationCompletedWithSuccess("DeleteDependencyEdgeAsync", input.Id, result);
        return new DeleteDependencyEdgePayload { Boolean = result };
    }

    // Node operations - using simplified service
    public async Task<CreateNodePayload> CreateNodeAsync(CreateNodeInput input,
        [Service] INodeApplicationService nodeService,
        [Service] IGraphQlMapperService mapper,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledSimple("CreateNodeAsync");
        var node = await mapper.MapToNodeAsync(input.NodeInput);
        logger.LogMutationCalled("CreateNodeAsync", node.Id);

        // Log detailed node information at Debug level
        logger.LogNodeOperationDetails("CREATE_NODE", node);

        var result = await nodeService.CreateNodeAsync(node);
        logger.LogMutationCompleted("CreateNodeAsync", result.Id);
        return new CreateNodePayload { Node = result };
    }

    public async Task<UpdateNodePayload> UpdateNodeAsync(UpdateNodeInput input,
        [Service] INodeApplicationService nodeService,
        [Service] IGraphQlMapperService mapper,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledSimple("UpdateNodeAsync");
        var node = await mapper.MapToNodeAsync(input.NodeInput);
        logger.LogMutationCalled("UpdateNodeAsync", node.Id);

        // Log detailed node information at Debug level
        logger.LogNodeOperationDetails("UPDATE_NODE", node);

        var result = await nodeService.UpdateNodeAsync(node);
        logger.LogMutationCompletedWithSuccess("UpdateNodeAsync", node.Id, result);
        return new UpdateNodePayload { Boolean = result };
    }

    public async Task<DeleteNodePayload> DeleteNodeAsync(
        DeleteNodeInput input,
        [Service] INodeApplicationService nodeService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("DeleteNodeAsync", input.Id);
        var result = await nodeService.DeleteNodeAsync(input.Id);
        logger.LogMutationCompletedWithSuccess("DeleteNodeAsync", input.Id, result);
        return new DeleteNodePayload { Boolean = result };
    }

    // Agent operations - using simplified service
    public async Task<CreateAgentPayload> CreateAgentAsync(
        CreateAgentInput input,
        [Service] IAgentApplicationService agentService,
        [Service] IGraphQlMapperService mapper,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("CreateAgentAsync", input.AgentInput.Id);
        var agent = await mapper.MapToAgentAsync(input.AgentInput);
        var result = await agentService.CreateAgentAsync(agent);
        logger.LogMutationCompleted("CreateAgentAsync", result.Id);
        return new CreateAgentPayload { Agent = result };
    }

    public async Task<UpdateAgentPayload> UpdateAgentAsync(
        UpdateAgentInput input,
        [Service] IAgentApplicationService agentService,
        [Service] IGraphQlMapperService mapper,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("UpdateAgentAsync", input.Id);
        var agent = await mapper.MapToAgentAsync(input.AgentInput);
        agent.Id = input.Id; // Ensure we're updating the right agent
        var result = await agentService.UpdateAgentAsync(agent);
        logger.LogMutationCompletedWithSuccess("UpdateAgentAsync", input.Id, result != null);
        return new UpdateAgentPayload { Agent = result };
    }

    public async Task<DeleteAgentPayload> DeleteAgentAsync(
        DeleteAgentInput input,
        [Service] IAgentApplicationService agentService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("DeleteAgentAsync", input.Id);
        var result = await agentService.DeleteAgentAsync(input.Id);
        logger.LogMutationCompletedWithSuccess("DeleteAgentAsync", input.Id, result);
        return new DeleteAgentPayload { Boolean = result };
    }

    // Skill operations - using simplified service
    public async Task<CreateSkillPayload> CreateSkillAsync(
        CreateSkillInput input,
        [Service] ISkillApplicationService skillService,
        [Service] IGraphQlMapperService mapper,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("CreateSkillAsync", input.SkillInput.Id);
        var skill = await mapper.MapToSkillAsync(input.SkillInput);
        var result = await skillService.CreateSkillAsync(skill);
        logger.LogMutationCompleted("CreateSkillAsync", result.Id);
        return new CreateSkillPayload { Skill = result };
    }

    public async Task<UpdateSkillPayload> UpdateSkillAsync(
        UpdateSkillInput input,
        [Service] ISkillApplicationService skillService,
        [Service] IGraphQlMapperService mapper,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("UpdateSkillAsync", input.Id);
        var skill = await mapper.MapToSkillAsync(input.SkillInput);
        skill.Id = input.Id; // Ensure we're updating the right skill
        var result = await skillService.UpdateSkillAsync(skill);
        logger.LogMutationCompletedWithSuccess("UpdateSkillAsync", input.Id, result != null);
        return new UpdateSkillPayload { Skill = result };
    }

    public async Task<DeleteSkillPayload> DeleteSkillAsync(
        DeleteSkillInput input,
        [Service] ISkillApplicationService skillService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("DeleteSkillAsync", input.Id);
        var result = await skillService.DeleteSkillAsync(input.Id);
        logger.LogMutationCompletedWithSuccess("DeleteSkillAsync", input.Id, result);
        return new DeleteSkillPayload { Boolean = result };
    }

    // Variable management operations
    public async Task<Procedure> AddVariableToProcedure(
        Guid procedureId,
        VariableDefinitionInput variable,
        [Service] IProcedureVariableService procedureVariableService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledWithVariable("AddVariableToProcedure", procedureId, variable.Name);

        var variableDefinition = MapToVariableDefinition(variable);
        var result = await procedureVariableService.AddVariableAsync(procedureId, variableDefinition);

        logger.LogMutationCompletedForProcedure("AddVariableToProcedure", procedureId);

        return result;
    }

    public async Task<Procedure> UpdateProcedureVariable(
        Guid procedureId,
        string variableName,
        VariableDefinitionInput variable,
        [Service] IProcedureVariableService procedureVariableService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledWithVariable("UpdateProcedureVariable", procedureId, variableName);

        var variableDefinition = MapToVariableDefinition(variable);
        var result = await procedureVariableService.UpdateVariableAsync(procedureId, variableName, variableDefinition);

        logger.LogMutationCompletedForProcedure("UpdateProcedureVariable", procedureId);

        return result;
    }

    public async Task<Procedure> RemoveProcedureVariable(
        Guid procedureId,
        string variableName,
        [Service] IProcedureVariableService procedureVariableService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledWithVariable("RemoveProcedureVariable", procedureId, variableName);

        var result = await procedureVariableService.RemoveVariableAsync(procedureId, variableName);

        logger.LogMutationCompletedForProcedure("RemoveProcedureVariable", procedureId);

        return result;
    }

    // Procedure management mutations
    /// <summary>
    ///     Creates a new procedure.
    /// </summary>
    public async Task<CreateProcedurePayload> CreateProcedureAsync(
        CreateProcedureInput input,
        [Service] IProcedureOrchestrator procedureOrchestrator,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledWithName("CreateProcedureAsync", input.Name);

        var procedure = await procedureOrchestrator.CreateProcedureAsync(input.Name, input.Description);

        logger.LogMutationCompleted("CreateProcedureAsync", procedure.Id);

        return new CreateProcedurePayload { Procedure = procedure };
    }

    /// <summary>
    ///     Deletes a procedure and all associated entities.
    /// </summary>
    public async Task<DeleteProcedurePayload> DeleteProcedureAsync(
        DeleteProcedureInput input,
        [Service] IProcedureOrchestrator procedureOrchestrator,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("DeleteProcedureAsync", input.Id);

        var result = await procedureOrchestrator.DeleteProcedureAsync(input.Id);

        logger.LogMutationCompletedWithSuccess("DeleteProcedureAsync", input.Id, result);

        return new DeleteProcedurePayload { Boolean = result };
    }

    /// <summary>
    ///     Loads a procedure, making it the currently active procedure.
    /// </summary>
    public async Task<LoadProcedurePayload> LoadProcedureAsync(
        Guid id,
        [Service] IProcedureOrchestrator procedureOrchestrator,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalled("LoadProcedureAsync", id);

        var procedure = await procedureOrchestrator.LoadProcedureAsync(id);

        logger.LogMutationCompleted("LoadProcedureAsync", id);

        return new LoadProcedurePayload { Procedure = procedure };
    }

    /// <summary>
    ///     Unloads the currently active procedure.
    /// </summary>
    public async Task<UnloadProcedurePayload> UnloadProcedureAsync(
        [Service] IProcedureOrchestrator procedureOrchestrator,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogMutationCalledSimple("UnloadProcedureAsync");

        await procedureOrchestrator.UnloadCurrentProcedureAsync();

        logger.LogMutationCompletedWithResult("UnloadProcedureAsync", "Success");

        return new UnloadProcedurePayload { Success = true };
    }

    private static VariableDefinition MapToVariableDefinition(VariableDefinitionInput input)
    {
        return new VariableDefinition
        {
            Name = input.Name,
            Type = input.Type,
            DefaultValue = input.DefaultValue,
            Scope = input.Scope,
            Source = input.Source,
            Description = input.Description,
            IsReadOnly = input.IsReadOnly
        };
    }
}