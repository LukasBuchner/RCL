using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.Execution.Coordination;

/// <summary>
///     Resolves PositionTag-typed and SceneObject-typed skill properties to their
///     current values at execution time. Subscribes to change notifications from
///     both <see cref="EntityManagement.PositionTags.IPositionTagApplicationService" />
///     and <see cref="EntityManagement.SceneObjects.ISceneObjectApplicationService" />
///     to maintain always-current in-memory caches, avoiding database queries at
///     execution time.
///     <para>
///         Skill properties with entity references (PositionTag, SceneObject) embed a
///         snapshot from when the procedure node was saved. This resolver replaces those
///         stale snapshots with current data from the reactive caches before the skill
///         is dispatched to an agent.
///     </para>
/// </summary>
public interface ISceneEntityResolver
{
    /// <summary>
    ///     Returns a copy of the skill with all PositionTag-typed and SceneObject-typed
    ///     property values refreshed from the current in-memory caches. Properties whose
    ///     entity ID is not found in the cache are left unchanged.
    /// </summary>
    /// <param name="skill">The skill whose entity-reference properties should be refreshed.</param>
    /// <returns>
    ///     A skill with refreshed entity properties. Returns the same instance if no
    ///     entity-typed properties exist or all values are already current.
    /// </returns>
    Skill RefreshSceneEntityProperties(Skill skill);
}