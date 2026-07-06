using System.Text.Json;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.GraphQLServer.Configuration;
using FHOOE.Freydis.GraphQLServer.Support.Logging;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.GraphQLServer.Services.Initialization;

/// <summary>
///     Implementation of scene initialization service.
/// </summary>
public class SceneInitializationService : ISceneInitializationService
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<SceneInitializationService> _logger;
    private readonly IPositionTagApplicationService _positionTagService;
    private readonly ISceneObjectApplicationService _sceneObjectService;
    private readonly SceneOptions _sceneOptions;

    /// <summary>
    ///     Initializes a new instance of the SceneInitializationService.
    /// </summary>
    /// <param name="sceneOptions">Scene configuration options.</param>
    /// <param name="positionTagService">Position tag application service.</param>
    /// <param name="sceneObjectService">Scene object application service.</param>
    /// <param name="logger">Logger instance.</param>
    public SceneInitializationService(
        IOptions<SceneOptions> sceneOptions,
        IPositionTagApplicationService positionTagService,
        ISceneObjectApplicationService sceneObjectService,
        ILogger<SceneInitializationService> logger)
    {
        _sceneOptions = sceneOptions.Value;
        _positionTagService = positionTagService ?? throw new ArgumentNullException(nameof(positionTagService));
        _sceneObjectService = sceneObjectService ?? throw new ArgumentNullException(nameof(sceneObjectService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeSceneAsync(CancellationToken cancellationToken = default)
    {
        if (!_sceneOptions.AutoLoad)
        {
            _logger.LogSceneAutoLoadDisabled();
            return;
        }

        if (string.IsNullOrEmpty(_sceneOptions.ConfigurationFile))
        {
            _logger.LogNoSceneConfigFile();
            return;
        }

        _logger.LogLoadingSceneConfig(_sceneOptions.ConfigurationFile);

        try
        {
            // Load configuration
            var configJson = await File.ReadAllTextAsync(_sceneOptions.ConfigurationFile, cancellationToken);
            var configuration = JsonSerializer.Deserialize<SceneConfiguration>(configJson,
                CaseInsensitiveJsonOptions);

            if (configuration == null) throw new InvalidOperationException("Failed to deserialize scene configuration");

            var positionTagsCreated = 0;
            var positionTagsUpdated = 0;

            // Sync PositionTags to database
            if (configuration.PositionTags is { Count: > 0 })
            {
                _logger.LogSyncingPositionTags(configuration.PositionTags.Count);
                foreach (var positionTag in configuration.PositionTags)
                    try
                    {
                        var existing = await _positionTagService.GetPositionTagByIdAsync(positionTag.Id);
                        if (existing == null)
                        {
                            await _positionTagService.CreatePositionTagAsync(positionTag);
                            positionTagsCreated++;
                            _logger.LogCreatedPositionTag(positionTag.Tag, positionTag.Id);
                        }
                        else
                        {
                            await _positionTagService.UpdatePositionTagAsync(positionTag);
                            positionTagsUpdated++;
                            _logger.LogUpdatedPositionTag(positionTag.Tag, positionTag.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogPositionTagSyncFailed(ex, positionTag.Tag, positionTag.Id);
                        throw;
                    }
            }

            var sceneObjectsCreated = 0;
            var sceneObjectsUpdated = 0;

            // Sync SceneObjects to database
            if (configuration.SceneObjects is { Count: > 0 })
            {
                _logger.LogSyncingSceneObjects(configuration.SceneObjects.Count);
                foreach (var sceneObject in configuration.SceneObjects)
                    try
                    {
                        var existing = await _sceneObjectService.GetSceneObjectByIdAsync(sceneObject.Id);
                        if (existing == null)
                        {
                            await _sceneObjectService.CreateSceneObjectAsync(sceneObject);
                            sceneObjectsCreated++;
                            _logger.LogCreatedSceneObject(sceneObject.Name, sceneObject.Id);
                        }
                        else
                        {
                            await _sceneObjectService.UpdateSceneObjectAsync(sceneObject);
                            sceneObjectsUpdated++;
                            _logger.LogUpdatedSceneObject(sceneObject.Name, sceneObject.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogSceneObjectSyncFailed(ex, sceneObject.Name, sceneObject.Id);
                        throw;
                    }
            }

            _logger.LogSceneLoaded(
                configuration.PositionTags?.Count ?? 0, positionTagsCreated, positionTagsUpdated,
                configuration.SceneObjects?.Count ?? 0, sceneObjectsCreated, sceneObjectsUpdated);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogSceneConfigFileNotFound(ex, _sceneOptions.ConfigurationFile);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogSceneInitFailed(ex);
            throw;
        }
    }
}