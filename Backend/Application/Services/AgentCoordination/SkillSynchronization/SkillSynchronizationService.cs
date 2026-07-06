using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.Support.Logging;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.SkillSynchronization;

/// <summary>
///     Implementation of skill synchronization service that maintains consistency
///     between runtime agent capabilities and domain skill definitions.
///     For each skill a runtime agent reports, ensures a corresponding domain skill
///     definition exists, creating or updating it as necessary.
/// </summary>
public class SkillSynchronizationService : ISkillSynchronizationService
{
    private readonly ILogger<SkillSynchronizationService> _logger;
    private readonly ISkillApplicationService _skillApplicationService;
    private readonly IRepository<Skill> _skillRepository;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SkillSynchronizationService" /> class.
    /// </summary>
    /// <param name="skillRepository">Repository for querying existing skill definitions.</param>
    /// <param name="skillApplicationService">Application service for creating and updating skill entities.</param>
    /// <param name="logger">Logger instance for diagnostic and operational events.</param>
    public SkillSynchronizationService(
        IRepository<Skill> skillRepository,
        ISkillApplicationService skillApplicationService,
        ILogger<SkillSynchronizationService> logger)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _skillApplicationService =
            skillApplicationService ?? throw new ArgumentNullException(nameof(skillApplicationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SkillSynchronizationResult> SyncAgentSkillsAsync(IRuntimeAgent agent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var result = new SkillSynchronizationResult
        {
            StartedAt = DateTimeOffset.UtcNow
        };

        _logger.LogSkillSyncStart(agent.Id, agent.Name);

        try
        {
            // Get skills from runtime agent
            var agentSkills = await agent.GetAvailableSkillsAsync(cancellationToken);
            result.TotalSkillsProcessed = agentSkills.Count;

            _logger.LogAgentReportsSkills(agent.Name, agentSkills.Count);

            // Sync each skill
            foreach (var skill in agentSkills)
                try
                {
                    var skillChanged = await EnsureSkillExistsAsync(skill, cancellationToken);
                    if (skillChanged)
                    {
                        // We need to check if it was created or updated
                        var existingSkill = await _skillRepository.GetByIdAsync(skill.Id);
                        if (existingSkill == null)
                            result.SkillsCreated++;
                        else
                            result.SkillsUpdated++;
                    }
                    else
                    {
                        result.SkillsUnchanged++;
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Failed to sync skill {skill.Name} (ID: {skill.Id}): {ex.Message}";
                    _logger.LogSkillSyncFailed(skill.Name, agent.Name, ex);
                    result.Errors.Add(error);
                }
        }
        catch (Exception ex)
        {
            var error = $"Skill synchronization failed for agent {agent.Name}: {ex.Message}";
            _logger.LogSkillSyncProcessFailed(agent.Name, ex);
            result.Errors.Add(error);
        }
        finally
        {
            result.CompletedAt = DateTimeOffset.UtcNow;

            var changed = result.SkillsCreated + result.SkillsUpdated;
            _logger.LogSkillSyncComplete(agent.Name, result.TotalSkillsProcessed, changed);
            _logger.LogSkillSyncCompleteDetailed(agent.Id, agent.Name, result.TotalSkillsProcessed,
                result.SkillsCreated, result.SkillsUpdated, result.SkillsUnchanged, result.Errors.Count);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> EnsureSkillExistsAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skill);

        _logger.LogEnsureSkillExists(skill.Name, skill.Id);

        var existingSkill = await _skillRepository.GetByIdAsync(skill.Id);

        if (existingSkill == null)
        {
            // Skill doesn't exist, create it.
            // Handle race condition: another agent may create the same skill concurrently.
            // CreateAsync uses ON CONFLICT ... DO UPDATE (upsert), so concurrent
            // creation of the same skill is handled at the DB level without exceptions.
            _logger.LogCreatingNewSkill(skill.Name);
            await _skillApplicationService.CreateSkillAsync(skill);
            return true;
        }

        // Check if skill has changed
        if (HasSkillChanged(existingSkill, skill))
        {
            _logger.LogUpdatingExistingSkill(skill.Name);

            var updatedSkill = new Skill
            {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
                Properties = skill.Properties
            };

            await _skillApplicationService.UpdateSkillAsync(updatedSkill);
            return true;
        }

        // No changes needed
        return false;
    }

    /// <summary>
    ///     Determines if a skill has changed by comparing key properties.
    /// </summary>
    /// <param name="existingSkill">The existing skill from the repository.</param>
    /// <param name="newSkill">The new skill from the runtime agent.</param>
    /// <returns>True if the skill has changed, false otherwise.</returns>
    private static bool HasSkillChanged(Skill existingSkill, Skill newSkill)
    {
        if (existingSkill.Name != newSkill.Name)
            return true;

        if (existingSkill.Description != newSkill.Description)
            return true;

        if (existingSkill.Properties.Count != newSkill.Properties.Count)
            return true;

        // Detailed property comparison
        foreach (var existingProp in existingSkill.Properties)
        {
            var newProp = newSkill.Properties.FirstOrDefault(p => p.Name == existingProp.Name);

            if (newProp == null)
                return true; // TypedProperty removed or renamed

            // Compare property direction (critical for Input/Output detection)
            if (existingProp.Direction != newProp.Direction)
                return true;

            // Compare property type
            if (existingProp.Value.Type.TypeName != newProp.Value.Type.TypeName)
                return true;

            // Compare property value (basic comparison)
            if (!ArePropertyValuesEqual(existingProp.Value, newProp.Value))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Compares two TypedValues for equality.
    /// </summary>
    /// <param name="existing">The existing typed value.</param>
    /// <param name="newValue">The new typed value.</param>
    /// <returns>True if the values are equal, false otherwise.</returns>
    private static bool ArePropertyValuesEqual(TypedValue existing, TypedValue newValue)
    {
        // If types don't match, values aren't equal
        if (existing.Type.TypeName != newValue.Type.TypeName)
            return false;

        // Compare based on type
        return existing.Type.TypeName switch
        {
            "Number" => Equals(existing.Value, newValue.Value),
            "String" => Equals(existing.Value, newValue.Value),
            "Boolean" => Equals(existing.Value, newValue.Value),
            "Position" => ArePositionsEqual(existing.Value as Position, newValue.Value as Position),
            "PositionTag" => ArePositionTagsEqual(existing.Value as PositionTag, newValue.Value as PositionTag),
            "SceneObject" => AreSceneObjectsEqual(existing.Value as SceneObject, newValue.Value as SceneObject),
            _ => false // Unknown type, assume changed
        };
    }

    /// <summary>
    ///     Compares two Position objects for equality.
    /// </summary>
    private static bool ArePositionsEqual(Position? pos1, Position? pos2)
    {
        if (pos1 == null && pos2 == null) return true;
        if (pos1 == null || pos2 == null) return false;

        return pos1.X == pos2.X && pos1.Y == pos2.Y && pos1.Z == pos2.Z &&
               pos1.Alpha == pos2.Alpha && pos1.Beta == pos2.Beta && pos1.Gamma == pos2.Gamma;
    }

    /// <summary>
    ///     Compares two PositionTag objects for equality.
    /// </summary>
    private static bool ArePositionTagsEqual(PositionTag? tag1, PositionTag? tag2)
    {
        if (tag1 == null && tag2 == null) return true;
        if (tag1 == null || tag2 == null) return false;

        return tag1.Id == tag2.Id;
    }

    /// <summary>
    ///     Compares two SceneObject objects for equality.
    /// </summary>
    private static bool AreSceneObjectsEqual(SceneObject? obj1, SceneObject? obj2)
    {
        if (obj1 == null && obj2 == null) return true;
        if (obj1 == null || obj2 == null) return false;

        return obj1.Id == obj2.Id;
    }
}