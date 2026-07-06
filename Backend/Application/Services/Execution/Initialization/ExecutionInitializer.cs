using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Initialization;

/// <summary>
///     Initializes procedure execution by loading, assigning execution IDs, calculating initial schedule, preparing agent
///     assignments, and initializing variable contexts.
/// </summary>
public class ExecutionInitializer : IExecutionInitializer
{
    private readonly IAgentManager _agentManager;
    private readonly IExecutionIdAssigner _executionIdAssigner;
    private readonly ILogger<ExecutionInitializer> _logger;
    private readonly IProcedureContext _procedureContext;
    private readonly IProcedureRepository _procedureRepository;
    private readonly ITimingCalculationOrchestrator _timingCalculationOrchestrator;
    private readonly IVariableResolver _variableResolver;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionInitializer" /> class.
    /// </summary>
    /// <param name="procedureRepository">Repository for procedure aggregate operations.</param>
    /// <param name="timingCalculationOrchestrator">Orchestrator for timing calculations.</param>
    /// <param name="agentManager">Manager for agent operations.</param>
    /// <param name="executionIdAssigner">Service for assigning execution IDs.</param>
    /// <param name="variableResolver">Service for resolving variables.</param>
    /// <param name="procedureContext">Context for the currently loaded procedure.</param>
    /// <param name="logger">Logger instance.</param>
    public ExecutionInitializer(
        IProcedureRepository procedureRepository,
        ITimingCalculationOrchestrator timingCalculationOrchestrator,
        IAgentManager agentManager,
        IExecutionIdAssigner executionIdAssigner,
        IVariableResolver variableResolver,
        IProcedureContext procedureContext,
        ILogger<ExecutionInitializer> logger)
    {
        _procedureRepository = procedureRepository ?? throw new ArgumentNullException(nameof(procedureRepository));
        _timingCalculationOrchestrator = timingCalculationOrchestrator ??
                                         throw new ArgumentNullException(nameof(timingCalculationOrchestrator));
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        _executionIdAssigner = executionIdAssigner ?? throw new ArgumentNullException(nameof(executionIdAssigner));
        _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
        _procedureContext = procedureContext ?? throw new ArgumentNullException(nameof(procedureContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ExecutionInitializationResult> InitializeAsync(
        DateTimeOffset executionStartTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInitializationStarted();

            // Get the current procedure ID from context
            var procedureId = _procedureContext.RequireCurrentProcedureId();

            // Log initialization phase with orchestrator logger
            var loadingInfo = $"Loading nodes and edges for procedure {procedureId}";
            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                0,
                additionalInfo: loadingInfo);

            // Load nodes and edges from repositories for the current procedure only

            var nodesList = await _procedureRepository.GetNodesByProcedureIdAsync(procedureId);
            var edgesList = await _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);

            var skillCount = nodesList.OfType<SkillExecutionNode>().Count();
            _logger.LogNodesAndEdgesLoaded(nodesList.Count, edgesList.Count, procedureId);

            var loadedInfo = $"Loaded {nodesList.Count} nodes, {edgesList.Count} edges";
            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                skillCount,
                skillCount,
                additionalInfo: loadedInfo);

            // Load procedure to initialize variable context
            var procedure = await _procedureRepository.GetByIdAsync(procedureId);
            if (procedure == null)
            {
                _logger.LogProcedureNotFound(procedureId);
                return new ExecutionInitializationResult
                {
                    Success = false,
                    ErrorMessage = $"Procedure {procedureId} not found",
                    ExecutionStartTime = executionStartTime
                };
            }

            // Initialize variable context for the execution
            var executionId = Guid.NewGuid();
            var variableContext = await _variableResolver.InitializeContextAsync(
                executionId,
                procedure);

            var variableCount = variableContext.GetAllValues().Count;
            _logger.LogVariableContextInitialized(variableCount, executionId);

            // Assign execution IDs to all skill execution nodes
            var nodes = _executionIdAssigner.AssignExecutionIds(nodesList.AsReadOnly());
            var edges = edgesList.AsReadOnly();

            // Calculate initial schedule using timing calculation orchestrator
            var schedulingRequest = new SchedulingRequest
            {
                ProcedureId = procedureId,
                Nodes = nodes,
                Edges = edges,
                StrictMode = false,
                IncludeDetailedTiming = true,
                PreserveOriginalTaskDurations = true
            };

            var schedule = await _timingCalculationOrchestrator.CalculateAsync(schedulingRequest, cancellationToken);

            if (schedule is not { Success: true })
            {
                _logger.LogScheduleCalculationFailed(procedureId);
                _logger.LogOrchestratorPhase(
                    "INIT",
                    procedureId,
                    skillCount,
                    success: false,
                    errorMessage: "Failed to calculate initial schedule");

                return new ExecutionInitializationResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to calculate initial schedule for procedure {procedureId}",
                    Nodes = nodes,
                    Edges = edges,
                    Schedule = schedule,
                    ExecutionStartTime = executionStartTime,
                    VariableContext = variableContext
                };
            }

            // Build agent assignments from schedule
            var agentAssignments = BuildAgentAssignments(schedule.UpdatedNodes ?? nodes);

            _logger.LogAgentsAssigned(agentAssignments.Count, procedureId);

            var initCompleteInfo =
                $"Assigned {agentAssignments.Count} agents, calculated initial schedule, initialized {variableContext.GetAllValues().Count} variables";
            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                skillCount,
                skillCount,
                success: true,
                additionalInfo: initCompleteInfo);

            return new ExecutionInitializationResult
            {
                Success = true,
                ProcedureId = procedureId,
                Nodes = schedule.UpdatedNodes ?? nodes,
                Edges = edges,
                Schedule = schedule,
                AgentAssignments = agentAssignments,
                ExecutionStartTime = executionStartTime,
                VariableContext = variableContext
            };
        }
        catch (Exception ex)
        {
            _logger.LogInitializationFailed(ex);

            var procedureId = _procedureContext.CurrentProcedureId ?? Guid.Empty;

            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                0,
                success: false,
                errorMessage: ex.Message);

            return new ExecutionInitializationResult
            {
                Success = false,
                ErrorMessage = $"Failed to initialize execution: {ex.Message}",
                ExecutionStartTime = executionStartTime
            };
        }
    }

    /// <inheritdoc />
    public async Task<ExecutionInitializationResult> InitializeAsync(
        Guid procedureId,
        Guid executionId,
        DateTimeOffset executionStartTime,
        Dictionary<string, object>? userProvidedValues = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInitializationStartedForProcedure(procedureId, executionId);

            // Load procedure from repository
            var procedure = await _procedureRepository.GetByIdAsync(procedureId);
            if (procedure == null)
            {
                _logger.LogProcedureNotFound(procedureId);
                return new ExecutionInitializationResult
                {
                    Success = false,
                    ErrorMessage = $"Procedure {procedureId} not found",
                    ExecutionStartTime = executionStartTime
                };
            }

            var loadingInfo2 = $"Loading nodes and edges for procedure {procedureId}";
            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                0,
                additionalInfo: loadingInfo2);

            // Load nodes and edges from repositories for the specified procedure only
            List<Node> nodesList;
            List<DependencyEdge> edgesList;

            nodesList = await _procedureRepository.GetNodesByProcedureIdAsync(procedureId);
            edgesList = await _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);

            var skillCount = nodesList.OfType<SkillExecutionNode>().Count();
            _logger.LogNodesAndEdgesLoadedSimple(nodesList.Count, edgesList.Count);

            var loadedInfo2 = $"Loaded {nodesList.Count} nodes, {edgesList.Count} edges";
            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                skillCount,
                skillCount,
                additionalInfo: loadedInfo2);

            // Initialize variable context for the execution
            // Note: Validation exceptions (InvalidOperationException, InvalidCastException, ArgumentException)
            // are allowed to propagate to the caller as they represent invalid user input
            var variableContext = await _variableResolver.InitializeContextAsync(
                executionId,
                procedure,
                userProvidedValues);

            var variableCount2 = variableContext.GetAllValues().Count;
            _logger.LogVariableContextInitialized(variableCount2, executionId);

            // Assign execution IDs to all skill execution nodes
            var nodes = _executionIdAssigner.AssignExecutionIds(nodesList.AsReadOnly());
            var edges = edgesList.AsReadOnly();

            // Calculate initial schedule using timing calculation orchestrator
            var schedulingRequest = new SchedulingRequest
            {
                ProcedureId = procedureId,
                Nodes = nodes,
                Edges = edges,
                StrictMode = false,
                IncludeDetailedTiming = true,
                PreserveOriginalTaskDurations = true
            };

            var schedule = await _timingCalculationOrchestrator.CalculateAsync(schedulingRequest, cancellationToken);

            if (schedule is not { Success: true })
            {
                _logger.LogScheduleCalculationFailed(procedureId);
                _logger.LogOrchestratorPhase(
                    "INIT",
                    procedureId,
                    skillCount,
                    success: false,
                    errorMessage: "Failed to calculate initial schedule");

                return new ExecutionInitializationResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to calculate initial schedule for procedure {procedureId}",
                    Nodes = nodes,
                    Edges = edges,
                    Schedule = schedule,
                    ExecutionStartTime = executionStartTime,
                    VariableContext = variableContext
                };
            }

            // Build agent assignments from schedule
            var agentAssignments = BuildAgentAssignments(schedule.UpdatedNodes ?? nodes);

            _logger.LogAgentsAssigned(agentAssignments.Count, procedureId);

            var initCompleteInfo2 =
                $"Assigned {agentAssignments.Count} agents, calculated initial schedule, initialized {variableContext.GetAllValues().Count} variables";
            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                skillCount,
                skillCount,
                success: true,
                additionalInfo: initCompleteInfo2);

            return new ExecutionInitializationResult
            {
                Success = true,
                ProcedureId = procedureId,
                Nodes = schedule.UpdatedNodes ?? nodes,
                Edges = edges,
                Schedule = schedule,
                AgentAssignments = agentAssignments,
                ExecutionStartTime = executionStartTime,
                VariableContext = variableContext
            };
        }
        catch (InvalidOperationException)
        {
            // Validation exceptions propagate to caller (e.g., read-only variable override, undefined variable)
            throw;
        }
        catch (InvalidCastException)
        {
            // Type validation exceptions propagate to caller
            throw;
        }
        catch (ArgumentException)
        {
            // Argument validation exceptions propagate to caller
            throw;
        }
        catch (Exception ex)
        {
            // Operational exceptions (database, network, etc.) are caught and returned as failure result
            _logger.LogInitializationFailedForProcedure(ex, procedureId, executionId);
            _logger.LogOrchestratorPhase(
                "INIT",
                procedureId,
                0,
                success: false,
                errorMessage: ex.Message);

            return new ExecutionInitializationResult
            {
                Success = false,
                ErrorMessage = $"Failed to initialize execution: {ex.Message}",
                ExecutionStartTime = executionStartTime
            };
        }
    }

    /// <summary>
    ///     Builds agent assignments from scheduled nodes.
    /// </summary>
    private Dictionary<Guid, IRuntimeAgent> BuildAgentAssignments(IReadOnlyList<Node> nodes)
    {
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        var skillExecutionNodes = nodes.OfType<SkillExecutionNode>().ToList();

        foreach (var skillNode in skillExecutionNodes)
        {
            var agentId = skillNode.SkillExecutionTask.AgentId;
            var runtimeAgent = _agentManager.GetAgent(agentId);

            if (runtimeAgent != null)
            {
                agentAssignments[skillNode.Id] = runtimeAgent;
                _logger.LogAgentAssignedToSkill(agentId, runtimeAgent.Name, skillNode.Id);
            }
            else
            {
                _logger.LogAgentNotFoundForSkill(agentId, skillNode.Id);
            }
        }

        return agentAssignments;
    }
}