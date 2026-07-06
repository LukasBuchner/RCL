using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for agent provider
///     and skill property extraction operations. Covers <see cref="Services.Providers.RuntimeAgentProvider" />
///     (agent lookups and provider lifecycle) and <see cref="Utilities.SkillPropertyExtractor" />
///     (6DOF pose extraction from skill properties).
/// </summary>
public static partial class AgentProviderLogger
{
    // ──────────────────────────────────────────────────
    //  RuntimeAgentProvider — Lifecycle
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the creation of a <see cref="Services.Providers.RuntimeAgentProvider" /> instance,
    ///     including identity hashes for the provider and its backing agent manager.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="providerHash">The hash code of the newly created provider instance.</param>
    /// <param name="managerHash">The hash code of the underlying agent manager instance.</param>
    /// <param name="managerType">The concrete type name of the underlying agent manager.</param>
    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Debug,
        Message =
            "RuntimeAgentProvider created: providerInstance={ProviderHash}, agentManagerInstance={ManagerHash}, managerType={ManagerType}")]
    public static partial void LogProviderCreated(
        this ILogger logger,
        int providerHash,
        int managerHash,
        string managerType);

    // ──────────────────────────────────────────────────
    //  RuntimeAgentProvider — Agent lookup
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the result of an agent lookup by ID, including whether the agent was found
    ///     and identity hashes for the provider and manager instances for diagnostics.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The unique identifier of the agent being looked up.</param>
    /// <param name="found">Whether the agent was found in the manager.</param>
    /// <param name="providerHash">The hash code of the provider instance performing the lookup.</param>
    /// <param name="managerHash">The hash code of the underlying agent manager instance.</param>
    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Debug,
        Message =
            "GetRuntimeAgent({AgentId}): result={Found}, providerInstance={ProviderHash}, managerInstance={ManagerHash}")]
    public static partial void LogRuntimeAgentLookup(
        this ILogger logger,
        Guid agentId,
        bool found,
        int providerHash,
        int managerHash);

    // ──────────────────────────────────────────────────
    //  SkillPropertyExtractor — Pose extraction
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the successful extraction of a 6DOF pose from a <c>PositionType</c> property
    ///     on a skill, including all six pose components.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="propertyName">The name of the position-typed property that was read.</param>
    /// <param name="skillName">The name of the skill the property belongs to.</param>
    /// <param name="x">The X translation component of the extracted pose.</param>
    /// <param name="y">The Y translation component of the extracted pose.</param>
    /// <param name="z">The Z translation component of the extracted pose.</param>
    /// <param name="alpha">The Alpha (rotation around X) component of the extracted pose.</param>
    /// <param name="beta">The Beta (rotation around Y) component of the extracted pose.</param>
    /// <param name="gamma">The Gamma (rotation around Z) component of the extracted pose.</param>
    [LoggerMessage(
        EventId = 1110,
        Level = LogLevel.Debug,
        Message =
            "Extracted pose from PositionType '{PropertyName}' for skill '{SkillName}': X={X}, Y={Y}, Z={Z}, Alpha={Alpha}, Beta={Beta}, Gamma={Gamma}")]
    public static partial void LogPoseExtracted(
        this ILogger logger,
        string propertyName,
        string skillName,
        double x,
        double y,
        double z,
        double alpha,
        double beta,
        double gamma);

    /// <summary>
    ///     Logs a warning when a named property is missing or not of <c>NumberType</c>
    ///     on a skill, causing a default value to be used instead.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="propertyName">The name of the missing or incorrectly typed property.</param>
    /// <param name="skillName">The name of the skill the property was expected on.</param>
    /// <param name="defaultValue">The default value that will be used in place of the missing property.</param>
    [LoggerMessage(
        EventId = 1111,
        Level = LogLevel.Warning,
        Message =
            "TypedProperty '{PropertyName}' not found or not a number for skill '{SkillName}', using default: {DefaultValue}")]
    public static partial void LogPropertyNotFoundOrNotNumber(
        this ILogger logger,
        string propertyName,
        string skillName,
        double defaultValue);
}