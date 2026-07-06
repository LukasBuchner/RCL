namespace FHOOE.Freydis.Application.Services.Common;

/// <summary>
///     Generic Tarjan's SCC detector. Finds all strongly connected components
///     in a directed graph represented as an adjacency dictionary.
/// </summary>
/// <remarks>
///     Extracted from <c>ExecutionGraphExtensions</c> to be reusable across both
///     the scheduling layer (IPlannedSkillExecution) and the execution layer (Guid node IDs).
///     Complexity: O(V+E).
/// </remarks>
public static class SccDetector
{
    /// <summary>
    ///     Finds all strongly connected components in the given adjacency graph.
    ///     Returns SCCs in reverse topological order (sinks first).
    /// </summary>
    /// <typeparam name="T">The vertex type (must be usable as dictionary key).</typeparam>
    /// <param name="adjacency">
    ///     For each vertex, the list of vertices it has edges to.
    ///     Vertices that appear only as targets (not keys) are treated as having no outgoing edges.
    /// </param>
    /// <returns>A list of SCCs, each SCC being a list of vertices.</returns>
    public static IReadOnlyList<IReadOnlyList<T>> FindSccs<T>(
        IReadOnlyDictionary<T, IReadOnlyList<T>> adjacency) where T : notnull
    {
        var index = 0;
        var indices = new Dictionary<T, int>();
        var lowLinks = new Dictionary<T, int>();
        var onStack = new HashSet<T>();
        var stack = new Stack<T>();
        var result = new List<IReadOnlyList<T>>();

        // Collect all vertices: keys + any targets not already keys
        var allVertices = new HashSet<T>(adjacency.Keys);
        foreach (var targets in adjacency.Values)
            foreach (var t in targets)
                allVertices.Add(t);

        foreach (var v in allVertices.Where(v => !indices.ContainsKey(v)))
            StrongConnect(v, adjacency, indices, lowLinks, onStack, stack, ref index, result);

        return result;
    }

    private static void StrongConnect<T>(
        T v,
        IReadOnlyDictionary<T, IReadOnlyList<T>> adjacency,
        Dictionary<T, int> indices,
        Dictionary<T, int> lowLinks,
        HashSet<T> onStack,
        Stack<T> stack,
        ref int index,
        List<IReadOnlyList<T>> result) where T : notnull
    {
        indices[v] = index;
        lowLinks[v] = index;
        index++;
        stack.Push(v);
        onStack.Add(v);

        if (adjacency.TryGetValue(v, out var targets))
            foreach (var w in targets)
                if (!indices.TryGetValue(w, out _))
                {
                    StrongConnect(w, adjacency, indices, lowLinks, onStack, stack, ref index, result);
                    lowLinks[v] = Math.Min(lowLinks[v], lowLinks[w]);
                }
                else if (onStack.Contains(w))
                {
                    lowLinks[v] = Math.Min(lowLinks[v], indices[w]);
                }

        if (lowLinks[v] != indices[v]) return;

        // v is the root of an SCC
        var scc = new List<T>();
        T popped;
        do
        {
            popped = stack.Pop();
            onStack.Remove(popped);
            scc.Add(popped);
        } while (!EqualityComparer<T>.Default.Equals(popped, v));

        result.Add(scc);
    }
}