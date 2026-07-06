namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Event arguments for entity change notifications.
/// </summary>
/// <typeparam name="TEntity">The type of entity that changed.</typeparam>
public class EntityChangedEventArgs<TEntity> : EventArgs where TEntity : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityChangedEventArgs{TEntity}" /> class.
    /// </summary>
    /// <param name="entities">The entities after the change.</param>
    public EntityChangedEventArgs(IReadOnlyList<TEntity> entities)
    {
        Entities = entities;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Gets the entities after the change operation.
    /// </summary>
    public IReadOnlyList<TEntity> Entities { get; }

    /// <summary>
    ///     Gets the timestamp when the change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}