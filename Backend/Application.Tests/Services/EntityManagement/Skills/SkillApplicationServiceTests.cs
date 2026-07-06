using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.Skills;

/// <summary>
///     Tests for <see cref="SkillApplicationService" /> verifying CRUD operations,
///     reactive notifications, and error handling.
/// </summary>
public class SkillApplicationServiceTests
{
    private readonly Mock<ILogger<SkillApplicationService>> _mockLogger;
    private readonly Mock<IRepository<Skill>> _mockRepository;
    private readonly SkillApplicationService _service;

    public SkillApplicationServiceTests()
    {
        _mockRepository = new Mock<IRepository<Skill>>();
        _mockLogger = new Mock<ILogger<SkillApplicationService>>();
        _service = new SkillApplicationService(_mockRepository.Object, _mockLogger.Object);
    }

    private static Skill CreateSkill(string name = "TestSkill")
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test skill description",
            Properties = []
        };
    }

    #region Notification Error Handling

    [Fact]
    public async Task CreateSkillAsync_NotificationFails_PropagatesErrorToSubscribers()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.CreateAsync(skill)).ReturnsAsync(skill);
        _mockRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException("DB error"));

        Exception? receivedError = null;
        _service.OnSkillsChanged().Subscribe(_ => { }, ex => receivedError = ex);

        // Act
        await _service.CreateSkillAsync(skill);

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
            new SkillApplicationService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SkillApplicationService(_mockRepository.Object, null!));
    }

    #endregion

    #region Create

    [Fact]
    public async Task CreateSkillAsync_ValidSkill_ReturnsCreatedSkill()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.CreateAsync(skill)).ReturnsAsync(skill);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([skill]);

        // Act
        var result = await _service.CreateSkillAsync(skill);

        // Assert
        Assert.Equal(skill.Id, result.Id);
        _mockRepository.Verify(r => r.CreateAsync(skill), Times.Once);
    }

    [Fact]
    public async Task CreateSkillAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateSkillAsync(null!));
    }

    [Fact]
    public async Task CreateSkillAsync_NotifiesSubscribers()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.CreateAsync(skill)).ReturnsAsync(skill);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([skill]);

        IReadOnlyList<Skill>? notified = null;
        _service.OnSkillsChanged().Subscribe(list => notified = list);

        // Act
        await _service.CreateSkillAsync(skill);

        // Assert
        Assert.NotNull(notified);
        Assert.Single(notified);
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateSkillAsync_Success_ReturnsSkill()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.UpdateAsync(skill)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([skill]);

        // Act
        var result = await _service.UpdateSkillAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(skill.Id, result.Id);
    }

    [Fact]
    public async Task UpdateSkillAsync_Failure_ReturnsNull()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.UpdateAsync(skill)).ReturnsAsync(false);

        // Act
        var result = await _service.UpdateSkillAsync(skill);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateSkillAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateSkillAsync(null!));
    }

    [Fact]
    public async Task UpdateSkillAsync_Success_NotifiesSubscribers()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.UpdateAsync(skill)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([skill]);

        IReadOnlyList<Skill>? notified = null;
        _service.OnSkillsChanged().Subscribe(list => notified = list);

        // Act
        await _service.UpdateSkillAsync(skill);

        // Assert
        Assert.NotNull(notified);
    }

    [Fact]
    public async Task UpdateSkillAsync_Failure_DoesNotNotifySubscribers()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.UpdateAsync(skill)).ReturnsAsync(false);

        IReadOnlyList<Skill>? notified = null;
        _service.OnSkillsChanged().Subscribe(list => notified = list);

        // Act
        await _service.UpdateSkillAsync(skill);

        // Assert
        Assert.Null(notified);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteSkillAsync_Success_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.DeleteSkillAsync(id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteSkillAsync_Failure_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteSkillAsync(id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSkillAsync_Success_NotifiesSubscribers()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        IReadOnlyList<Skill>? notified = null;
        _service.OnSkillsChanged().Subscribe(list => notified = list);

        // Act
        await _service.DeleteSkillAsync(id);

        // Assert
        Assert.NotNull(notified);
        Assert.Empty(notified);
    }

    [Fact]
    public async Task DeleteSkillAsync_Failure_DoesNotNotifySubscribers()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        IReadOnlyList<Skill>? notified = null;
        _service.OnSkillsChanged().Subscribe(list => notified = list);

        // Act
        await _service.DeleteSkillAsync(id);

        // Assert
        Assert.Null(notified);
    }

    #endregion

    #region GetAll

    [Fact]
    public async Task GetAllSkillsAsync_ReturnsAllSkills()
    {
        // Arrange
        var skills = new List<Skill> { CreateSkill("A"), CreateSkill("B") };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(skills);

        // Act
        var result = await _service.GetAllSkillsAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllSkillsAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _service.GetAllSkillsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetSkillByIdAsync_ExistingId_ReturnsSkill()
    {
        // Arrange
        var skill = CreateSkill();
        _mockRepository.Setup(r => r.GetByIdAsync(skill.Id)).ReturnsAsync(skill);

        // Act
        var result = await _service.GetSkillByIdAsync(skill.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(skill.Id, result.Id);
    }

    [Fact]
    public async Task GetSkillByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Skill?)null);

        // Act
        var result = await _service.GetSkillByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    #endregion
}