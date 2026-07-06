using System.Reactive.Linq;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Nodes;

/// <summary>
///     Application service for procedure-scoped node operations with validation, reactive notifications, and scheduling
///     integration.
/// </summary>
/// <remarks>
///     <para>
///         This service enforces procedure-based isolation, ensuring all operations are scoped to the currently loaded
///         procedure.
///         All CRUD methods validate procedure ownership before executing, preventing cross-procedure data access.
///     </para>
///     <para>
///         CRUD operations are delegated to the scheduling orchestrator for coordinated scheduling updates.
///         The service provides reactive notifications through an observable that automatically filters nodes
///         to match the currently loaded procedure.
///     </para>
///     <para>
///         Key features:
///         <list type="bullet">
///             <item>Procedure ownership validation on all mutations (Create, Update, Delete)</item>
///             <item>Automatic filtering by loaded procedure for queries</item>
///             <item>Real-time reactive updates filtered to current procedure</item>
///             <item>Integration with scheduling system for coordinated updates</item>
///         </list>
///     </para>
/// </remarks>
public sealed class NodeApplicationService : INodeApplicationService
{
    private readonly ICrudSchedulingOrchestrator _crudOrchestrator;
    private readonly double _defaultTaskDuration;
    private readonly ILogger<NodeApplicationService> _logger;
    private readonly INodeChangeTracker _nodeChangeTracker;
    private readonly IProcedureContext _procedureContext;
    private readonly IProcedureRepository _procedureRepository;
    private readonly IProcedureVariableService _procedureVariableService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NodeApplicationService" /> class.
    /// </summary>
    /// <param name="procedureRepository">The repository for procedure aggregate persistence operations.</param>
    /// <param name="crudOrchestrator">The CRUD orchestrator for coordinated operations with scheduling.</param>
    /// <param name="nodeChangeTracker">The change tracker for node entities.</param>
    /// <param name="procedureContext">The procedure context for validating procedure ownership.</param>
    /// <param name="procedureVariableService">The service for managing procedure variables.</param>
    /// <param name="schedulingOptions">The scheduling configuration options.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
    public NodeApplicationService(
        IProcedureRepository procedureRepository,
        ICrudSchedulingOrchestrator crudOrchestrator,
        INodeChangeTracker nodeChangeTracker,
        IProcedureContext procedureContext,
        IProcedureVariableService procedureVariableService,
        IOptions<SchedulingConfiguration> schedulingOptions,
        ILogger<NodeApplicationService> logger)
    {
        _procedureRepository = procedureRepository ?? throw new ArgumentNullException(nameof(procedureRepository));
        _crudOrchestrator = crudOrchestrator ?? throw new ArgumentNullException(nameof(crudOrchestrator));
        _nodeChangeTracker = nodeChangeTracker ?? throw new ArgumentNullException(nameof(nodeChangeTracker));
        _procedureContext = procedureContext ?? throw new ArgumentNullException(nameof(procedureContext));
        _procedureVariableService = procedureVariableService ??
                                    throw new ArgumentNullException(nameof(procedureVariableService));
        _defaultTaskDuration = schedulingOptions?.Value.Defaults.DefaultTaskDuration ?? 200.0;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Node> CreateNodeAsync(Node node)
    {
        _procedureContext.ValidateProcedureOwnership(node.ProcedureId);
        await ThrowIfParentIsRouterAsync(node);
        var createdNode = await _crudOrchestrator.CreateNodeAsync(node);

        // Automatically create procedure variables for skill output properties
        await CreateVariablesForSkillOutputsAsync(createdNode);

        // Automatically create branch TaskNodes for RouterNodes
        await CreateBranchTaskNodesForRouterAsync(createdNode);

        return createdNode;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateNodeAsync(Node node)
    {
        _procedureContext.ValidateProcedureOwnership(node.ProcedureId);

        // For RouterNodes, preserve existing TargetNodeId values when not provided in the update
        var nodeToUpdate = await PreserveBranchTargetNodeIdsAsync(node);

        return await _crudOrchestrator.UpdateNodeAsync(nodeToUpdate);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteNodeAsync(Guid nodeId)
    {
        // Fetch node first to validate ownership
        var node = await _procedureRepository.GetNodeByIdAsync(nodeId);
        if (node != null) _procedureContext.ValidateProcedureOwnership(node.ProcedureId);

        // Clean up procedure variables for skill output properties before deletion
        if (node != null)
            await RemoveVariablesForNodeTreeAsync(nodeId, node.ProcedureId);

        return await _crudOrchestrator.DeleteNodeTreeAsync(nodeId);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Node>> GetAllNodesAsync()
    {
        var procedureId = _procedureContext.CurrentProcedureId;

        // If no procedure is loaded, return an empty list (valid at startup)
        if (!procedureId.HasValue)
        {
            _logger.LogGetAll("Node", 0);
            return Task.FromResult<IReadOnlyList<Node>>(Array.Empty<Node>());
        }

        var nodes = _nodeChangeTracker.GetCurrentNodes()
            .Where(n => n.ProcedureId == procedureId.Value)
            .ToList();
        _logger.LogGetAll("Node", nodes.Count);
        return Task.FromResult<IReadOnlyList<Node>>(nodes.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<Node?> GetNodeByIdAsync(Guid nodeId)
    {
        _logger.LogGetById("Node", nodeId);
        var node = _nodeChangeTracker.GetCurrentNodes().FirstOrDefault(n => n.Id == nodeId);

        if (node != null) _procedureContext.ValidateProcedureOwnership(node.ProcedureId);

        return Task.FromResult(node);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Node>> GetNodesByParentIdAsync(Guid parentId)
    {
        _logger.LogGetById("Node", parentId);
        var procedureId = _procedureContext.CurrentProcedureId;

        // If no procedure is loaded, return an empty list (valid at startup)
        if (!procedureId.HasValue) return Task.FromResult<IReadOnlyList<Node>>(Array.Empty<Node>());

        var childNodes = _nodeChangeTracker.GetCurrentNodes()
            .Where(n => n.ProcedureId == procedureId.Value && n.ParentId == parentId)
            .ToList();
        return Task.FromResult<IReadOnlyList<Node>>(childNodes.AsReadOnly());
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<Node>> Nodes =>
        _procedureContext.ProcedureChanges.StartWith(_procedureContext.CurrentProcedureId).CombineLatest(
            _nodeChangeTracker.Nodes,
            (procedureId, allNodes) => FilterByProcedureId(allNodes, procedureId));

    /// <summary>
    ///     Preserves existing TargetNodeId values from the database when updating a RouterNode.
    ///     This prevents loss of TargetNodeId mappings when the frontend sends updates without them.
    /// </summary>
    /// <param name="node">The node being updated.</param>
    /// <returns>The node with preserved TargetNodeId values if it's a RouterNode, otherwise the original node.</returns>
    private async Task<Node> PreserveBranchTargetNodeIdsAsync(Node node)
    {
        if (node is not RouterNode routerNode)
            return node;

        var existingNode = await _procedureRepository.GetNodeByIdAsync(node.Id);
        if (existingNode is not RouterNode existingRouterNode)
            return node;

        var existingBranchTargets = existingRouterNode.RouterTask.Branches
            .Where(b => b.TargetNodeId.HasValue)
            .ToDictionary(b => b.Name, b => b.TargetNodeId!.Value);

        if (existingBranchTargets.Count == 0)
            return node;

        var updatedBranches = routerNode.RouterTask.Branches
            .Select(branch =>
            {
                if (branch.TargetNodeId.HasValue)
                    return branch;

                if (existingBranchTargets.TryGetValue(branch.Name, out var existingTargetId))
                {
                    _logger.LogPreservingBranchTargetNodeId(existingTargetId, branch.Name, node.Id);
                    return branch with { TargetNodeId = existingTargetId };
                }

                return branch;
            })
            .ToList();

        var updatedRouterTask = routerNode.RouterTask with { Branches = updatedBranches };
        return routerNode with { RouterTask = updatedRouterTask };
    }

    /// <summary>
    ///     Filters a list of nodes to only include those belonging to the specified procedure.
    /// </summary>
    /// <param name="nodes">The complete list of nodes.</param>
    /// <param name="procedureId">The procedure ID to filter by, or null to return no nodes.</param>
    /// <returns>A filtered list containing only nodes from the specified procedure.</returns>
    /// <remarks>
    ///     <para>
    ///         This method is called reactively whenever either the procedure context changes
    ///         or the node collection changes, ensuring the filtered view stays synchronized.
    ///     </para>
    ///     <para>
    ///         The observable uses StartWith to ensure ProcedureChanges has an immediate value,
    ///         preventing CombineLatest from waiting for a procedure change event before emitting.
    ///         This ensures new nodes are immediately visible to subscribers even when the procedure
    ///         context hasn't changed.
    ///     </para>
    /// </remarks>
    private static IReadOnlyList<Node> FilterByProcedureId(IReadOnlyList<Node> nodes, Guid? procedureId)
    {
        return !procedureId.HasValue
            ? Array.Empty<Node>()
            : nodes.Where(n => n.ProcedureId == procedureId.Value).ToList();
    }

    /// <summary>
    ///     Automatically creates procedure variables for output properties of a skill execution node.
    /// </summary>
    /// <param name="node">The node that was just created.</param>
    /// <remarks>
    ///     <para>
    ///         When a SkillExecutionNode is created, this method inspects the skill's properties
    ///         and creates procedure variables for any properties with Output or InputOutput direction.
    ///         This allows router nodes to reference these variables in their conditional expressions.
    ///     </para>
    ///     <para>
    ///         The created variables have:
    ///         <list type="bullet">
    ///             <item>Name matching the property name</item>
    ///             <item>Type matching the property's value type</item>
    ///             <item>Default value matching the property's current value</item>
    ///             <item>Source set to SkillOutput</item>
    ///             <item>Scope set to Procedure</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         If a variable with the same name already exists in the procedure, it is silently skipped
    ///         to avoid conflicts with user-defined variables.
    ///     </para>
    /// </remarks>
    private async Task CreateVariablesForSkillOutputsAsync(Node node)
    {
        // Only process SkillExecutionNode
        if (node is not SkillExecutionNode skillExecutionNode)
            return;

        var skill = skillExecutionNode.SkillExecutionTask.Skill;
        var procedureId = node.ProcedureId;

        // Find all output properties (Output or InputOutput direction)
        var outputProperties = skill.Properties
            .Where(p => p.Direction is PropertyDirection.Output or PropertyDirection.InputOutput)
            .ToList();

        if (outputProperties.Count == 0)
        {
            _logger.LogNoOutputProperties(skill.Name, node.Id);
            return;
        }

        _logger.LogCreatingOutputVariables(outputProperties.Count, skill.Name, node.Id);

        // Create a variable for each output property
        foreach (var property in outputProperties)
            try
            {
                var variableDefinition = new VariableDefinition
                {
                    Name = property.Name,
                    Type = property.Value.Type,
                    DefaultValue = property.Value.Value,
                    Source = VariableSource.SkillOutput,
                    Scope = VariableScope.Procedure,
                    Description = $"Output from skill '{skill.Name}' property '{property.Name}'",
                    IsReadOnly = false
                };

                await _procedureVariableService.AddVariableAsync(procedureId, variableDefinition);

                _logger.LogOutputVariableCreated(property.Name, property.Value.Type.TypeName);
            }
            catch (VariableAlreadyExistsException)
            {
                // Variable already exists - this is fine, just log it
                _logger.LogOutputVariableAlreadyExists(property.Name, procedureId);
            }
            catch (Exception ex)
            {
                _logger.LogOutputVariableCreationFailed(property.Name, procedureId, ex);
                // Continue with other properties even if one fails
            }
    }

    /// <summary>
    ///     Automatically creates TaskNodes for each branch in a RouterNode and links them.
    /// </summary>
    /// <param name="node">The node that was just created.</param>
    /// <remarks>
    ///     <para>
    ///         When a RouterNode is created, this method automatically creates a TaskNode for each
    ///         branch in the RouterTask.Branches collection. Each created TaskNode is configured as follows:
    ///         <list type="bullet">
    ///             <item>ParentId is set to the RouterNode's ID</item>
    ///             <item>ProcedureId matches the RouterNode's ProcedureId</item>
    ///             <item>Task.Name is set to "{BranchName} Branch"</item>
    ///             <item>Initial position is (0, 0)</item>
    ///             <item>StartTime and Duration are both 0</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         After creating the TaskNodes, the method updates the RouterNode's branches to set
    ///         each branch's TargetNodeId to point to the corresponding created TaskNode.
    ///         This establishes the routing relationships that the execution engine will follow.
    ///     </para>
    ///     <para>
    ///         This automatic creation ensures that RouterNodes always have valid branch targets,
    ///         simplifying the user experience and preventing invalid routing configurations.
    ///     </para>
    /// </remarks>
    private async Task CreateBranchTaskNodesForRouterAsync(Node node)
    {
        // Only process RouterNode
        if (node is not RouterNode routerNode)
            return;

        var branches = routerNode.RouterTask.Branches;
        if (branches == null || branches.Count == 0)
        {
            _logger.LogNoBranches(node.Id);
            return;
        }

        _logger.LogCreatingBranchTaskNodes(branches.Count, node.Id);

        var updatedBranches = new List<ConditionalBranch>();

        // Create a TaskNode for each branch
        foreach (var branch in branches)
            try
            {
                var branchTaskNode = new TaskNode
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = node.ProcedureId,
                    ParentId = node.Id,
                    Position = new NodePosition { X = 0, Y = 0 },
                    Extent = "parent", // Constrain child node within parent bounds
                    Task = new Domain.Entities.Procedure.Task
                    {
                        Name = $"{branch.Name} Branch",
                        Description = $"Branch target for '{branch.Name}' from router '{routerNode.RouterTask.Name}'",
                        StartTime = 0,
                        Duration = _defaultTaskDuration
                    }
                };

                // Create the TaskNode through the orchestrator
                var createdTaskNode = await _crudOrchestrator.CreateNodeAsync(branchTaskNode);

                _logger.LogBranchTaskNodeCreated(createdTaskNode.Id, branch.Name, node.Id);

                // Update the branch to point to the created TaskNode
                var updatedBranch = branch with { TargetNodeId = createdTaskNode.Id };
                updatedBranches.Add(updatedBranch);
            }
            catch (Exception ex)
            {
                _logger.LogBranchTaskNodeCreationFailed(branch.Name, node.Id, ex);
                throw;
            }

        // Update the RouterNode with the new branch references
        // Set first branch as default selection for timeline filtering
        var firstBranchName = updatedBranches.FirstOrDefault()?.Name;
        var updatedRouterTask = routerNode.RouterTask with
        {
            Branches = updatedBranches,
            ManuallySelectedBranch = firstBranchName
        };
        var updatedRouterNode = routerNode with { RouterTask = updatedRouterTask };

        _logger.LogAutoSelectedBranch(firstBranchName, node.Id);

        await _crudOrchestrator.UpdateNodeAsync(updatedRouterNode);

        _logger.LogBranchTaskNodesLinked(updatedBranches.Count, node.Id);
    }

    /// <summary>
    ///     Removes procedure variables for all <see cref="SkillExecutionNode" /> instances
    ///     in the node tree rooted at <paramref name="rootNodeId" />.
    ///     Discovers the root node and all descendants via BFS over the change tracker,
    ///     then delegates per-node cleanup to <see cref="RemoveVariablesForSkillNodeAsync" />.
    /// </summary>
    /// <param name="rootNodeId">The ID of the root node whose tree is being deleted.</param>
    /// <param name="procedureId">The procedure that owns the variables to remove.</param>
    /// <returns>A task that completes when all variable cleanup attempts have finished.</returns>
    private async Task RemoveVariablesForNodeTreeAsync(Guid rootNodeId, Guid procedureId)
    {
        var allNodes = _nodeChangeTracker.GetCurrentNodes();
        if (allNodes == null || allNodes.Count == 0)
            return;

        // Collect root + all descendants
        var nodesToProcess = new List<Node>();

        var rootNode = allNodes.FirstOrDefault(n => n.Id == rootNodeId);
        if (rootNode != null)
            nodesToProcess.Add(rootNode);

        nodesToProcess.AddRange(HierarchyTraversal.CollectDescendants(rootNodeId, allNodes));

        foreach (var node in nodesToProcess)
            await RemoveVariablesForSkillNodeAsync(node, procedureId);
    }

    /// <summary>
    ///     Removes procedure variables that were auto-created for a single <see cref="SkillExecutionNode" />'s
    ///     output properties. Non-skill nodes are silently skipped.
    ///     This is the deletion-side counterpart to <see cref="CreateVariablesForSkillOutputsAsync" />.
    /// </summary>
    /// <param name="node">The node to inspect. Only <see cref="SkillExecutionNode" /> instances are processed.</param>
    /// <param name="procedureId">The procedure that owns the variables to remove.</param>
    /// <returns>A task that completes when all variable removal attempts have finished.</returns>
    /// <remarks>
    ///     Removal is best-effort: if a variable was already manually removed by the user,
    ///     the resulting <see cref="InvalidOperationException" /> is caught and logged at debug level.
    ///     Other failures are logged as warnings. Neither case prevents deletion from proceeding.
    /// </remarks>
    private async Task RemoveVariablesForSkillNodeAsync(Node node, Guid procedureId)
    {
        if (node is not SkillExecutionNode skillNode)
            return;

        var outputProperties = skillNode.SkillExecutionTask.Skill.Properties
            .Where(p => p.Direction is PropertyDirection.Output or PropertyDirection.InputOutput)
            .ToList();

        if (outputProperties.Count == 0)
            return;

        _logger.LogRemovingOutputVariables(outputProperties.Count, skillNode.SkillExecutionTask.Skill.Name, node.Id);

        foreach (var property in outputProperties)
            try
            {
                await _procedureVariableService.RemoveVariableAsync(procedureId, property.Name);
                _logger.LogOutputVariableRemoved(property.Name, node.Id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogOutputVariableAlreadyRemoved(property.Name, node.Id, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogOutputVariableRemovalFailed(property.Name, node.Id, ex);
            }
    }

    /// <summary>
    ///     Validates that the node's parent is not a <see cref="RouterNode" />.
    ///     RouterNode children are created exclusively by the internal branch auto-creation path
    ///     (<see cref="CreateBranchTaskNodesForRouterAsync" />), so external callers must not
    ///     manually add children under a RouterNode.
    /// </summary>
    /// <param name="node">The node being created whose parent relationship is validated.</param>
    /// <returns>A task that completes when validation succeeds.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <paramref name="node" /> has a <see cref="Node.ParentId" /> that references
    ///     a <see cref="RouterNode" />. The exception message identifies the parent router node ID.
    /// </exception>
    private async Task ThrowIfParentIsRouterAsync(Node node)
    {
        if (node.ParentId is null)
            return;

        var parentNode = await _procedureRepository.GetNodeByIdAsync(node.ParentId.Value);

        if (parentNode is RouterNode)
            throw new InvalidOperationException(
                $"Cannot manually create a child node under RouterNode '{node.ParentId.Value}'. " +
                "RouterNode children are created automatically when the router is created.");
    }
}