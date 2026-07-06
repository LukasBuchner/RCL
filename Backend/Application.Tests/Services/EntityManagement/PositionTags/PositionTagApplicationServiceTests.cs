using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.PositionTags;

/// <summary>
///     Tests for <see cref="PositionTagApplicationService" /> verifying CRUD operations,
///     reactive notifications, and error handling.
/// </summary>
public class PositionTagApplicationServiceTests
{
    private readonly Mock<ILogger<PositionTagApplicationService>> _mockLogger;
    private readonly Mock<IRepository<PositionTag>> _mockRepository;
    private readonly PositionTagApplicationService _service;

    public PositionTagApplicationServiceTests()
    {
        _mockRepository = new Mock<IRepository<PositionTag>>();
        _mockLogger = new Mock<ILogger<PositionTagApplicationService>>();
        _service = new PositionTagApplicationService(_mockRepository.Object, _mockLogger.Object);
    }

    private static PositionTag CreatePositionTag(string tag = "TestTag")
    {
        return new PositionTag
        {
            Id = Guid.NewGuid(),
            Tag = tag,
            Position = new Position { X = 1, Y = 2, Z = 3 }
        };
    }

    #region Notification Error Handling

    [Fact]
    public async Task CreatePositionTagAsync_NotificationFails_PropagatesErrorToSubscribers()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.CreateAsync(tag)).ReturnsAsync(tag);
        _mockRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException("DB error"));

        Exception? receivedError = null;
        _service.OnPositionTagsChanged().Subscribe(_ => { }, ex => receivedError = ex);

        // Act
        await _service.CreatePositionTagAsync(tag);

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
            new PositionTagApplicationService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PositionTagApplicationService(_mockRepository.Object, null!));
    }

    #endregion

    #region Create

    [Fact]
    public async Task CreatePositionTagAsync_ValidTag_ReturnsCreatedTag()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.CreateAsync(tag)).ReturnsAsync(tag);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([tag]);

        // Act
        var result = await _service.CreatePositionTagAsync(tag);

        // Assert
        Assert.Equal(tag.Id, result.Id);
        _mockRepository.Verify(r => r.CreateAsync(tag), Times.Once);
    }

    [Fact]
    public async Task CreatePositionTagAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreatePositionTagAsync(null!));
    }

    [Fact]
    public async Task CreatePositionTagAsync_NotifiesSubscribers()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.CreateAsync(tag)).ReturnsAsync(tag);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([tag]);

        IReadOnlyList<PositionTag>? notified = null;
        _service.OnPositionTagsChanged().Subscribe(list => notified = list);

        // Act
        await _service.CreatePositionTagAsync(tag);

        // Assert
        Assert.NotNull(notified);
        Assert.Single(notified);
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdatePositionTagAsync_Success_ReturnsTrue()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.UpdateAsync(tag)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([tag]);

        // Act
        var result = await _service.UpdatePositionTagAsync(tag);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdatePositionTagAsync_Failure_ReturnsFalse()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.UpdateAsync(tag)).ReturnsAsync(false);

        // Act
        var result = await _service.UpdatePositionTagAsync(tag);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdatePositionTagAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdatePositionTagAsync(null!));
    }

    [Fact]
    public async Task UpdatePositionTagAsync_Success_NotifiesSubscribers()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.UpdateAsync(tag)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([tag]);

        IReadOnlyList<PositionTag>? notified = null;
        _service.OnPositionTagsChanged().Subscribe(list => notified = list);

        // Act
        await _service.UpdatePositionTagAsync(tag);

        // Assert
        Assert.NotNull(notified);
    }

    [Fact]
    public async Task UpdatePositionTagAsync_Failure_DoesNotNotifySubscribers()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.UpdateAsync(tag)).ReturnsAsync(false);

        IReadOnlyList<PositionTag>? notified = null;
        _service.OnPositionTagsChanged().Subscribe(list => notified = list);

        // Act
        await _service.UpdatePositionTagAsync(tag);

        // Assert
        Assert.Null(notified);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeletePositionTagAsync_Success_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.DeletePositionTagAsync(id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeletePositionTagAsync_Failure_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _service.DeletePositionTagAsync(id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeletePositionTagAsync_Success_NotifiesSubscribers()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        IReadOnlyList<PositionTag>? notified = null;
        _service.OnPositionTagsChanged().Subscribe(list => notified = list);

        // Act
        await _service.DeletePositionTagAsync(id);

        // Assert
        Assert.NotNull(notified);
        Assert.Empty(notified);
    }

    [Fact]
    public async Task DeletePositionTagAsync_Failure_DoesNotNotifySubscribers()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        IReadOnlyList<PositionTag>? notified = null;
        _service.OnPositionTagsChanged().Subscribe(list => notified = list);

        // Act
        await _service.DeletePositionTagAsync(id);

        // Assert
        Assert.Null(notified);
    }

    #endregion

    #region GetAll

    [Fact]
    public async Task GetAllPositionTagsAsync_ReturnsAllTags()
    {
        // Arrange
        var tags = new List<PositionTag> { CreatePositionTag("A"), CreatePositionTag("B") };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(tags);

        // Act
        var result = await _service.GetAllPositionTagsAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllPositionTagsAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.GetAllPositionTagsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetPositionTagByIdAsync_ExistingId_ReturnsTag()
    {
        // Arrange
        var tag = CreatePositionTag();
        _mockRepository.Setup(r => r.GetByIdAsync(tag.Id)).ReturnsAsync(tag);

        // Act
        var result = await _service.GetPositionTagByIdAsync(tag.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tag.Id, result.Id);
    }

    [Fact]
    public async Task GetPositionTagByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((PositionTag?)null);

        // Act
        var result = await _service.GetPositionTagByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    #endregion
}