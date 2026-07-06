using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Skills;

/// <summary>
///     Application service for skill management operations with integrated reactive notifications.
///     Provides a simple, focused interface for all skill-related operations.
///     This service directly interacts with the repository layer and provides real-time updates through reactive streams.
/// </summary>
/// <remarks>
///     This interface follows the same pattern as INodeApplicationService with direct repository access.
///     It integrates reactive notifications directly, eliminating the need for separate event dispatchers.
///     All operations that modify skills automatically trigger notifications to subscribers.
/// </remarks>
public interface ISkillApplicationService : IDisposable
{
    /// <summary>
    ///     Creates a new skill in the system.
    /// </summary>
    /// <param name="skill">The skill entity to create. Must not be null and should have valid data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created skill.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the skill parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnSkillsChanged observable.
    /// </remarks>
    Task<Skill> CreateSkillAsync(Skill skill);

    /// <summary>
    ///     Updates an existing skill with new data.
    /// </summary>
    /// <param name="skill">The skill entity containing updated data. Must not be null and must have an existing ID.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the updated skill if successful,
    ///     null otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the skill parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnSkillsChanged observable when
    ///     successful.
    /// </remarks>
    Task<Skill?> UpdateSkillAsync(Skill skill);

    /// <summary>
    ///     Deletes a skill from the system.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill to delete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the deletion was
    ///     successful, false if the skill was not found.
    /// </returns>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnSkillsChanged observable when
    ///     successful.
    /// </remarks>
    Task<bool> DeleteSkillAsync(Guid skillId);

    /// <summary>
    ///     Retrieves all skills from the system.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all skills.</returns>
    Task<IReadOnlyList<Skill>> GetAllSkillsAsync();

    /// <summary>
    ///     Retrieves a specific skill by its unique identifier.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill to retrieve.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the skill if found, null
    ///     otherwise.
    /// </returns>
    Task<Skill?> GetSkillByIdAsync(Guid skillId);

    /// <summary>
    ///     Gets an observable sequence that notifies subscribers when skills have changed in the system.
    /// </summary>
    /// <returns>
    ///     An observable sequence that emits the complete list of skills whenever any skill is created, updated, or
    ///     deleted.
    /// </returns>
    /// <remarks>
    ///     This observable provides real-time notifications for all skill changes.
    ///     The observable uses Rx.NET and is suitable for implementing GraphQL subscriptions.
    /// </remarks>
    IObservable<IReadOnlyList<Skill>> OnSkillsChanged();
}