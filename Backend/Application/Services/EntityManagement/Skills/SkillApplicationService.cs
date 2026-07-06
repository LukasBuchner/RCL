using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Skills;

/// <summary>
///     Application service for skill operations with direct repository access and integrated reactive notifications.
///     Follows the same simplified pattern as NodeApplicationService.
/// </summary>
/// <remarks>
///     This service implementation provides a simplified approach to skill management by directly using the repository
///     pattern with integrated reactive notifications using Rx.NET's Subject pattern.
/// </remarks>
public sealed class SkillApplicationService : ISkillApplicationService
{
    private readonly ILogger<SkillApplicationService> _logger;
    private readonly IRepository<Skill> _skillRepository;
    private readonly Subject<IReadOnlyList<Skill>> _skillsChangedSubject;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SkillApplicationService" /> class.
    /// </summary>
    /// <param name="skillRepository">The repository for skill data persistence operations.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
    public SkillApplicationService(
        IRepository<Skill> skillRepository,
        ILogger<SkillApplicationService> logger)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _skillsChangedSubject = new Subject<IReadOnlyList<Skill>>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _skillsChangedSubject.Dispose();
    }

    /// <inheritdoc />
    public async Task<Skill> CreateSkillAsync(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        _logger.LogCreateStart("Skill", skill.Id, skill.Name);

        var createdSkill = await _skillRepository.CreateAsync(skill);

        // Notify subscribers with all skills
        await NotifySkillsChangedAsync();

        return createdSkill;
    }

    /// <inheritdoc />
    public async Task<Skill?> UpdateSkillAsync(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        _logger.LogUpdateStart("Skill", skill.Id, skill.Name);

        var result = await _skillRepository.UpdateAsync(skill);

        if (result)
        {
            await NotifySkillsChangedAsync();
            return skill;
        }

        _logger.LogUpdateFailed("Skill", skill.Id, skill.Name);
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSkillAsync(Guid skillId)
    {
        _logger.LogDeleteStart("Skill", skillId);

        var result = await _skillRepository.DeleteAsync(skillId);

        if (result)
            await NotifySkillsChangedAsync();
        else
            _logger.LogDeleteFailed("Skill", skillId);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Skill>> GetAllSkillsAsync()
    {
        var skills = await _skillRepository.GetAllAsync();
        _logger.LogGetAll("Skill", skills.Count);
        return skills.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Skill?> GetSkillByIdAsync(Guid skillId)
    {
        _logger.LogGetById("Skill", skillId);
        return await _skillRepository.GetByIdAsync(skillId);
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<Skill>> OnSkillsChanged()
    {
        return _skillsChangedSubject.AsObservable();
    }

    /// <summary>
    ///     Notifies all subscribers about skill changes by emitting the current state of all skills.
    /// </summary>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    private async Task NotifySkillsChangedAsync()
    {
        try
        {
            var allSkills = await _skillRepository.GetAllAsync();
            _skillsChangedSubject.OnNext(allSkills.AsReadOnly());
            _logger.LogNotificationSent("Skill", allSkills.Count);
        }
        catch (Exception ex)
        {
            _logger.LogNotificationFailed("Skill", ex);
            _skillsChangedSubject.OnError(ex);
        }
    }
}