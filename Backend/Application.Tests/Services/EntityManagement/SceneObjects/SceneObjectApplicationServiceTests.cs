using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.SceneObjects;

/// <summary>
///     Tests for <see cref="SceneObjectApplicationService" /> verifying CRUD operations,
///     reactive notifications, and error handling.
/// </summary>
public class SceneObjectApplicationServiceTests
{
    private readonly Mock<ILogger<SceneObjectApplicationService>> _mockLogger;
    private readonly Mock<IRepository<SceneObject>> _mockRepository;
    private readonly SceneObjectApplicationService _service;

    public SceneObjectApplicationServiceTests()
    {
        _mockRepository = new Mock<IRepository<SceneObject>>();
        _mockLogger = new Mock<ILogger<SceneObjectApplicationService>>();
        _service = new SceneObjectApplicationService(_mockRepository.Object, _mockLogger.Object);
    }

    private static SceneObject CreateSceneObject(string name = "TestObject")
    {
        return new SceneObject
        {
            Id = Guid.NewGuid(),
            Name = name,
            Position = new Position { X = 1, Y = 2, Z = 3 }
        };
    }

    #region Notification Error Handling

    [Fact]
    public async Task CreateSceneObjectAsync_NotificationFails_PropagatesErrorToSubscribers()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.CreateAsync(sceneObject)).ReturnsAsync(sceneObject);
        _mockRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException("DB error"));

        Exception? receivedError = null;
        _service.OnSceneObjectsChanged().Subscribe(_ => { }, ex => receivedError = ex);

        // Act
        await _service.CreateSceneObjectAsync(sceneObject);

        // Assert
        Assert.NotNull(receivedError);
        Assert.IsType<InvalidOperationException>(receivedError);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SceneObjectApplicationService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SceneObjectApplicationService(_mockRepository.Object, null!));
    }

    #endregion

    #region Create

    [Fact]
    public async Task CreateSceneObjectAsync_ValidObject_ReturnsCreatedObject()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.CreateAsync(sceneObject)).ReturnsAsync(sceneObject);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([sceneObject]);

        // Act
        var result = await _service.CreateSceneObjectAsync(sceneObject);

        // Assert
        Assert.Equal(sceneObject.Id, result.Id);
        _mockRepository.Verify(r => r.CreateAsync(sceneObject), Times.Once);
    }

    [Fact]
    public async Task CreateSceneObjectAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateSceneObjectAsync(null!));
    }

    [Fact]
    public async Task CreateSceneObjectAsync_NotifiesSubscribers()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.CreateAsync(sceneObject)).ReturnsAsync(sceneObject);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([sceneObject]);

        IReadOnlyList<SceneObject>? notified = null;
        _service.OnSceneObjectsChanged().Subscribe(list => notified = list);

        // Act
        await _service.CreateSceneObjectAsync(sceneObject);

        // Assert
        Assert.NotNull(notified);
        Assert.Single(notified);
        Assert.Equal(sceneObject.Id, notified[0].Id);
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateSceneObjectAsync_Success_ReturnsTrue()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.UpdateAsync(sceneObject)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([sceneObject]);

        // Act
        var result = await _service.UpdateSceneObjectAsync(sceneObject);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.UpdateAsync(sceneObject), Times.Once);
    }

    [Fact]
    public async Task UpdateSceneObjectAsync_Failure_ReturnsFalse()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.UpdateAsync(sceneObject)).ReturnsAsync(false);

        // Act
        var result = await _service.UpdateSceneObjectAsync(sceneObject);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateSceneObjectAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateSceneObjectAsync(null!));
    }

    [Fact]
    public async Task UpdateSceneObjectAsync_Success_NotifiesSubscribers()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.UpdateAsync(sceneObject)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([sceneObject]);

        IReadOnlyList<SceneObject>? notified = null;
        _service.OnSceneObjectsChanged().Subscribe(list => notified = list);

        // Act
        await _service.UpdateSceneObjectAsync(sceneObject);

        // Assert
        Assert.NotNull(notified);
    }

    [Fact]
    public async Task UpdateSceneObjectAsync_Failure_DoesNotNotifySubscribers()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.UpdateAsync(sceneObject)).ReturnsAsync(false);

        IReadOnlyList<SceneObject>? notified = null;
        _service.OnSceneObjectsChanged().Subscribe(list => notified = list);

        // Act
        await _service.UpdateSceneObjectAsync(sceneObject);

        // Assert
        Assert.Null(notified);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteSceneObjectAsync_Success_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.DeleteSceneObjectAsync(id);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteSceneObjectAsync_Failure_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteSceneObjectAsync(id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSceneObjectAsync_Success_NotifiesSubscribers()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        IReadOnlyList<SceneObject>? notified = null;
        _service.OnSceneObjectsChanged().Subscribe(list => notified = list);

        // Act
        await _service.DeleteSceneObjectAsync(id);

        // Assert
        Assert.NotNull(notified);
        Assert.Empty(notified);
    }

    [Fact]
    public async Task DeleteSceneObjectAsync_Failure_DoesNotNotifySubscribers()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        IReadOnlyList<SceneObject>? notified = null;
        _service.OnSceneObjectsChanged().Subscribe(list => notified = list);

        // Act
        await _service.DeleteSceneObjectAsync(id);

        // Assert
        Assert.Null(notified);
    }

    #endregion

    #region GetAll

    [Fact]
    public async Task GetAllSceneObjectsAsync_ReturnsAllObjects()
    {
        // Arrange
        var objects = new List<SceneObject> { CreateSceneObject("A"), CreateSceneObject("B") };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(objects);

        // Act
        var result = await _service.GetAllSceneObjectsAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllSceneObjectsAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.GetAllSceneObjectsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetSceneObjectByIdAsync_ExistingId_ReturnsObject()
    {
        // Arrange
        var sceneObject = CreateSceneObject();
        _mockRepository.Setup(r => r.GetByIdAsync(sceneObject.Id)).ReturnsAsync(sceneObject);

        // Act
        var result = await _service.GetSceneObjectByIdAsync(sceneObject.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sceneObject.Id, result.Id);
    }

    [Fact]
    public async Task GetSceneObjectByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((SceneObject?)null);

        // Act
        var result = await _service.GetSceneObjectByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    #endregion
}