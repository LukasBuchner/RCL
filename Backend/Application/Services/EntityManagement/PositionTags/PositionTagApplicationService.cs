using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;

/// <summary>
///     Application service for position tag operations with direct repository access and integrated reactive
///     notifications.
/// </summary>
/// <remarks>
///     This service integrates reactive notifications using Rx.NET's Subject pattern
///     to provide real-time updates to subscribers whenever position tags are modified.
/// </remarks>
public sealed class PositionTagApplicationService : IPositionTagApplicationService
{
    private readonly ILogger<PositionTagApplicationService> _logger;
    private readonly IRepository<PositionTag> _positionTagRepository;
    private readonly Subject<IReadOnlyList<PositionTag>> _positionTagsChangedSubject;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PositionTagApplicationService" /> class.
    /// </summary>
    /// <param name="positionTagRepository">The repository for position tag data persistence operations.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
    public PositionTagApplicationService(
        IRepository<PositionTag> positionTagRepository,
        ILogger<PositionTagApplicationService> logger)
    {
        _positionTagRepository =
            positionTagRepository ?? throw new ArgumentNullException(nameof(positionTagRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _positionTagsChangedSubject = new Subject<IReadOnlyList<PositionTag>>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _positionTagsChangedSubject.Dispose();
    }

    /// <inheritdoc />
    public async Task<PositionTag> CreatePositionTagAsync(PositionTag positionTag)
    {
        ArgumentNullException.ThrowIfNull(positionTag);

        _logger.LogCreateStart("PositionTag", positionTag.Id, positionTag.Tag);

        var createdPositionTag = await _positionTagRepository.CreateAsync(positionTag);

        // Notify subscribers with all position tags
        await NotifyPositionTagsChangedAsync();

        return createdPositionTag;
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePositionTagAsync(PositionTag positionTag)
    {
        ArgumentNullException.ThrowIfNull(positionTag);

        _logger.LogUpdateStart("PositionTag", positionTag.Id, positionTag.Tag);

        var result = await _positionTagRepository.UpdateAsync(positionTag);

        if (result)
            await NotifyPositionTagsChangedAsync();
        else
            _logger.LogUpdateFailed("PositionTag", positionTag.Id, positionTag.Tag);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePositionTagAsync(Guid positionTagId)
    {
        _logger.LogDeleteStart("PositionTag", positionTagId);

        var result = await _positionTagRepository.DeleteAsync(positionTagId);

        if (result)
            await NotifyPositionTagsChangedAsync();
        else
            _logger.LogDeleteFailed("PositionTag", positionTagId);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PositionTag>> GetAllPositionTagsAsync()
    {
        var positionTags = await _positionTagRepository.GetAllAsync();
        _logger.LogGetAll("PositionTag", positionTags.Count);
        return positionTags.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<PositionTag?> GetPositionTagByIdAsync(Guid positionTagId)
    {
        _logger.LogGetById("PositionTag", positionTagId);
        return await _positionTagRepository.GetByIdAsync(positionTagId);
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<PositionTag>> OnPositionTagsChanged()
    {
        return _positionTagsChangedSubject.AsObservable();
    }

    /// <summary>
    ///     Notifies all subscribers about position tag changes by emitting the current state of all position tags.
    /// </summary>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    /// <remarks>
    ///     This method retrieves all position tags from the repository and emits them through the reactive subject.
    ///     If an error occurs during notification, it logs the error and propagates it to subscribers via OnError.
    ///     This ensures that subscribers are aware of any issues in the notification pipeline.
    /// </remarks>
    private async Task NotifyPositionTagsChangedAsync()
    {
        try
        {
            var allPositionTags = await _positionTagRepository.GetAllAsync();
            _positionTagsChangedSubject.OnNext(allPositionTags.AsReadOnly());
            _logger.LogNotificationSent("PositionTag", allPositionTags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogNotificationFailed("PositionTag", ex);
            _positionTagsChangedSubject.OnError(ex);
        }
    }
}