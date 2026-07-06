using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillSynchronization;
using FHOOE.Freydis.Application.Services.AgentCoordination.Support.Logging;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.Registration;

/// <summary>
///     Service responsible for registering runtime agents in the domain model
///     and managing agent state transitions. Provides idempotent activation
///     via <see cref="EnsureAgentActiveAsync" /> for both initial startup and reconnection.
/// </summary>
public class AgentRegistrationService : IAgentRegistrationService
{
    private readonly IAgentApplicationService _agentApplicationService;
    private readonly IRepository<Agent> _agentRepository;
    private readonly ILogger<AgentRegistrationService> _logger;
    private readonly ISkillSynchronizationService _skillSyncService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentRegistrationService" /> class.
    /// </summary>
    /// <param name="agentRepository">Repository for querying existing agent records.</param>
    /// <param name="agentApplicationService">Application service for creating and updating agent entities.</param>
    /// <param name="skillSyncService">Service for synchronizing runtime agent skills with the domain model.</param>
    /// <param name="logger">Logger instance for diagnostic and operational events.</param>
    public AgentRegistrationService(
        IRepository<Agent> agentRepository,
        IAgentApplicationService agentApplicationService,
        ISkillSynchronizationService skillSyncService,
        ILogger<AgentRegistrationService> logger)
    {
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
        _agentApplicationService =
            agentApplicationService ?? throw new ArgumentNullException(nameof(agentApplicationService));
        _skillSyncService = skillSyncService ?? throw new ArgumentNullException(nameof(skillSyncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Agent> EnsureAgentActiveAsync(IRuntimeAgent runtimeAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runtimeAgent);

        var existingAgent = await _agentRepository.GetByIdAsync(runtimeAgent.Id);

        if (existingAgent != null)
        {
            // Agent already exists — reactivate: sync skills, set Active, update timestamp
            _logger.LogAgentReactivating(runtimeAgent.Name, runtimeAgent.Id);

            await _skillSyncService.SyncAgentSkillsAsync(runtimeAgent, cancellationToken);

            // Refresh skill IDs from the runtime agent so newly added skills are picked up
            var currentSkills = await runtimeAgent.GetAvailableSkillsAsync(cancellationToken);
            var currentSkillIds = currentSkills.Select(s => s.Id).ToList();

            var reactivated = existingAgent with
            {
                SkillIds = currentSkillIds,
                State = AgentState.Active,
                LastSeenUtc = DateTime.UtcNow
            };

            var updated = await _agentApplicationService.UpdateAgentAsync(reactivated);
            if (updated is null)
            {
                _logger.LogAgentReactivationFailed(reactivated.Name, reactivated.Id);
                return reactivated;
            }

            _logger.LogAgentReactivated(updated.Name, updated.Id);
            return updated;
        }

        // Agent does not exist — full registration (create + skill sync)
        return await RegisterNewAgentAsync(runtimeAgent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Agent?> UpdateAgentStateAsync(Guid agentId, AgentState state,
        CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent == null)
        {
            _logger.LogAgentStateUpdateNonExistent(agentId);
            return null;
        }

        // Only log state changes at INFO level if the state actually changes
        if (agent.State != state)
            _logger.LogAgentStateChanged(agent.Name, agent.State, state);
        else
            _logger.LogAgentStateUnchanged(agent.Name, agent.State);

        var updatedAgent = agent with
        {
            State = state,
            LastSeenUtc = DateTime.UtcNow
        };

        var result = await _agentApplicationService.UpdateAgentAsync(updatedAgent);
        return result;
    }

    /// <summary>
    ///     Creates a new agent in the domain model, synchronizing its skills first
    ///     to ensure all referenced skill definitions exist in the domain.
    /// </summary>
    /// <param name="runtimeAgent">The runtime agent to register.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created domain agent entity.</returns>
    private async Task<Agent> RegisterNewAgentAsync(IRuntimeAgent runtimeAgent,
        CancellationToken cancellationToken)
    {
        _logger.LogAgentRegistrationStart(runtimeAgent.Id, runtimeAgent.Name);

        // Synchronize skills first to ensure they exist in the domain
        var skillSyncResult = await _skillSyncService.SyncAgentSkillsAsync(runtimeAgent, cancellationToken);

        // Get agent skills after synchronization
        var skills = await runtimeAgent.GetAvailableSkillsAsync(cancellationToken);
        var skillIds = skills.Select(s => s.Id).ToList();

        // Create domain agent
        var agent = new Agent
        {
            Id = runtimeAgent.Id,
            Name = runtimeAgent.Name,
            RepresentativeColor = GenerateRepresentativeColor(runtimeAgent.Name),
            SkillIds = skillIds,
            State = AgentState.Active,
            LastSeenUtc = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["RuntimeType"] = runtimeAgent.GetType().Name,
                ["RegisteredAt"] = DateTime.UtcNow.ToString("O"),
                ["SkillsSynced"] = skillSyncResult.SkillsCreated + skillSyncResult.SkillsUpdated
            }
        };

        var createdAgent = await _agentApplicationService.CreateAgentAsync(agent);

        var skillsChanged = skillSyncResult.SkillsCreated + skillSyncResult.SkillsUpdated;
        _logger.LogAgentRegistrationSuccess(createdAgent.Name, skillIds.Count, skillsChanged);
        _logger.LogAgentRegistrationSuccessDetailed(createdAgent.Id, createdAgent.Name, skillIds.Count,
            skillSyncResult.SkillsCreated, skillSyncResult.SkillsUpdated);

        return createdAgent;
    }

    /// <summary>
    ///     Generates a representative color for an agent based on its name.
    /// </summary>
    /// <param name="agentName">The agent name.</param>
    /// <returns>A hex color code.</returns>
    private static string GenerateRepresentativeColor(string agentName)
    {
        // Simple color generation based on name hash
        var hash = agentName.GetHashCode();
        var r = (hash & 0xFF0000) >> 16;
        var g = (hash & 0x00FF00) >> 8;
        var b = hash & 0x0000FF;

        // Ensure the color is not too dark
        if (r < 64) r += 64;
        if (g < 64) g += 64;
        if (b < 64) b += 64;

        return $"#{r:X2}{g:X2}{b:X2}";
    }
}