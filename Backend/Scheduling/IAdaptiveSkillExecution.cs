using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Scheduling;

/// <summary>
///     Extends <see cref="IAdaptivePlannedSkillExecution" /> with run‑time information collected
///     while the skill is executing.
///     If the skill is not running yet, the Planned StartTime can still be changed.
///     If it is running, the Planned FinishTime (and Duration) can be changed.
///     Once completed, nothing can be changed any more.
/// </summary>
public interface IAdaptiveSkillExecution : ISkillExecution, IAdaptivePlannedSkillExecution;