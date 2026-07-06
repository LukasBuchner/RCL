using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.SkillSynchronization;

/// <summary>
///     Service responsible for synchronizing skills between runtime agents and the domain model.
///     Ensures that skills reported by runtime agents are properly represented as persistent
///     skill definitions, maintaining consistency between runtime capabilities and domain records.
/// </summary>
public interface ISkillSynchronizationService
{
    /// <summary>
    ///     Synchronizes all skills from a specific runtime agent with the domain model.
    ///     For each skill the agent reports, ensures a corresponding domain skill definition
    ///     exists, creating or updating it as necessary.
    /// </summary>
    /// <param name="agent">The runtime agent whose skills should be synchronized.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing synchronization statistics and any errors encountered.</returns>
    Task<SkillSynchronizationResult> SyncAgentSkillsAsync(IRuntimeAgent agent,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Ensures that a specific skill exists in the domain skill repository.
    ///     Creates the skill if it doesn't exist, updates it if it has changed.
    /// </summary>
    /// <param name="skill">The skill to ensure exists in the domain.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the skill was created or updated, false if no changes were needed.</returns>
    Task<bool> EnsureSkillExistsAsync(Skill skill, CancellationToken cancellationToken = default);
}

/// <summary>
///     Contains the results of a skill synchronization operation.
/// </summary>
public sealed class SkillSynchronizationResult
{
    /// <summary>
    ///     Gets or sets when the synchronization operation started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    ///     Gets or sets when the synchronization operation completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    ///     Gets or sets the total number of skills processed.
    /// </summary>
    public int TotalSkillsProcessed { get; set; }

    /// <summary>
    ///     Gets or sets the number of skills that were created in the domain.
    /// </summary>
    public int SkillsCreated { get; set; }

    /// <summary>
    ///     Gets or sets the number of skills that were updated in the domain.
    /// </summary>
    public int SkillsUpdated { get; set; }

    /// <summary>
    ///     Gets or sets the number of skills that were already up-to-date.
    /// </summary>
    public int SkillsUnchanged { get; set; }

    /// <summary>
    ///     Gets or sets the number of agents whose skill relationships were updated.
    /// </summary>
    public int AgentRelationshipsUpdated { get; set; }

    /// <summary>
    ///     Gets or sets any errors that occurred during synchronization.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    ///     Gets a value indicating whether the synchronization completed successfully without errors.
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;

    /// <summary>
    ///     Gets the duration of the synchronization operation.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
}