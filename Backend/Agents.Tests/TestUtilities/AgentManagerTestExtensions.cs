using System.Collections.Concurrent;
using System.Reflection;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;

namespace FHOOE.Freydis.Agents.Tests.TestUtilities;

/// <summary>
///     Extension methods for agent managers that provide test-specific functionality.
///     These methods are only available in test projects and should not be used in production code.
/// </summary>
/// <remarks>
///     <para>
///         These extensions use reflection to access internal state of agent managers for testing purposes.
///         They are designed to work with implementations that use ConcurrentDictionary for agent storage.
///     </para>
///     <para>
///         Supported manager types:
///         <list type="bullet">
///             <item><see cref="UnifiedAgentManager" /> - Uses _agents field</item>
///         </list>
///     </para>
/// </remarks>
public static class AgentManagerTestExtensions
{
    /// <summary>
    ///     Removes all agents from the manager.
    /// </summary>
    /// <param name="manager">The agent manager to clear.</param>
    /// <remarks>
    ///     This method uses reflection to access the internal _agents ConcurrentDictionary field.
    ///     It is intended for test cleanup and setup scenarios.
    /// </remarks>
    /// <example>
    ///     <code>
    /// [TestCleanup]
    /// public void Cleanup()
    /// {
    ///     _agentManager.ClearAgents();
    /// }
    /// </code>
    /// </example>
    public static void ClearAgents(this IAgentManager manager)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        var agentsField = manager.GetType()
            .GetField("_agents", BindingFlags.NonPublic | BindingFlags.Instance);

        if (agentsField?.GetValue(manager) is ConcurrentDictionary<Guid, IRuntimeAgent> agents)
            agents.Clear();
        else
            throw new InvalidOperationException(
                $"Could not find _agents field on manager type {manager.GetType().Name}. " +
                "This extension method requires the manager to use a ConcurrentDictionary<Guid, IRuntimeAgent> field named '_agents'.");
    }

    /// <summary>
    ///     Removes a specific agent from the manager by ID.
    /// </summary>
    /// <param name="manager">The agent manager.</param>
    /// <param name="agentId">The ID of the agent to remove.</param>
    /// <returns>
    ///     <c>true</c> if the agent was found and removed; <c>false</c> if the agent was not found.
    /// </returns>
    /// <remarks>
    ///     This method uses reflection to access the internal _agents ConcurrentDictionary field.
    ///     It provides a way to remove agents in tests without calling StopAgentAsync.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var agentId = Guid.NewGuid();
    /// var agent = await _agentManager.StartAgentAsync(agentId, "TestAgent");
    /// 
    /// // Later in test...
    /// bool removed = _agentManager.RemoveAgent(agentId);
    /// Assert.IsTrue(removed);
    /// </code>
    /// </example>
    public static bool RemoveAgent(this IAgentManager manager, Guid agentId)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        var agentsField = manager.GetType()
            .GetField("_agents", BindingFlags.NonPublic | BindingFlags.Instance);

        if (agentsField?.GetValue(manager) is ConcurrentDictionary<Guid, IRuntimeAgent> agents)
            return agents.TryRemove(agentId, out _);

        throw new InvalidOperationException(
            $"Could not find _agents field on manager type {manager.GetType().Name}. " +
            "This extension method requires the manager to use a ConcurrentDictionary<Guid, IRuntimeAgent> field named '_agents'.");
    }

    /// <summary>
    ///     Gets the count of agents currently in the manager using reflection.
    /// </summary>
    /// <param name="manager">The agent manager.</param>
    /// <returns>The number of agents in the manager.</returns>
    /// <remarks>
    ///     This method is primarily useful for verifying internal state in tests
    ///     when you need to bypass the public ActiveAgentCount property.
    /// </remarks>
    public static int GetAgentCountDirect(this IAgentManager manager)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        var agentsField = manager.GetType()
            .GetField("_agents", BindingFlags.NonPublic | BindingFlags.Instance);

        if (agentsField?.GetValue(manager) is ConcurrentDictionary<Guid, IRuntimeAgent> agents) return agents.Count;

        throw new InvalidOperationException(
            $"Could not find _agents field on manager type {manager.GetType().Name}. " +
            "This extension method requires the manager to use a ConcurrentDictionary<Guid, IRuntimeAgent> field named '_agents'.");
    }

    /// <summary>
    ///     Determines whether an agent with the specified ID exists in the manager.
    /// </summary>
    /// <param name="manager">The agent manager.</param>
    /// <param name="agentId">The agent ID to check.</param>
    /// <returns>
    ///     <c>true</c> if an agent with the specified ID exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This is a convenience method that wraps GetAgent with a null check.
    /// </remarks>
    public static bool HasAgent(this IAgentManager manager, Guid agentId)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        return manager.GetAgent(agentId) != null;
    }

    /// <summary>
    ///     Determines whether an agent with the specified name exists in the manager.
    /// </summary>
    /// <param name="manager">The agent manager.</param>
    /// <param name="agentName">The agent name to check.</param>
    /// <returns>
    ///     <c>true</c> if an agent with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This is a convenience method that wraps GetAgent with a null check.
    /// </remarks>
    public static bool HasAgent(this IAgentManager manager, string agentName)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        return manager.GetAgent(agentName) != null;
    }
}