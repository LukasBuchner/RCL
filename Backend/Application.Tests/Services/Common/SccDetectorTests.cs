using FHOOE.Freydis.Application.Services.Common;

namespace FHOOE.Freydis.Application.Tests.Services.Common;

/// <summary>
///     Tests for <see cref="SccDetector.FindSccs{T}" /> which implements Tarjan's algorithm
///     to find strongly connected components in a directed graph.
/// </summary>
public sealed class SccDetectorTests
{
    /// <summary>
    ///     Normalises SCC output so assertions are deterministic regardless of internal ordering.
    ///     Each SCC is sorted alphabetically, then SCCs are sorted by descending size
    ///     (ties broken by first member alphabetically).
    /// </summary>
    private static List<List<string>> Normalise(IReadOnlyList<IReadOnlyList<string>> sccs)
    {
        var sorted = sccs
            .Select(scc => scc.OrderBy(v => v, StringComparer.Ordinal).ToList())
            .OrderByDescending(scc => scc.Count)
            .ThenBy(scc => scc[0], StringComparer.Ordinal)
            .ToList();

        return sorted;
    }

    [Fact]
    public void SingleNode_NoEdges_ReturnsTrivialScc()
    {
        // Arrange
        var adjacency = new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = []
        };

        // Act
        var sccs = Normalise(SccDetector.FindSccs<string>(adjacency));

        // Assert
        Assert.Single(sccs);
        Assert.Equal(new[] { "A" }, sccs[0]);
    }

    [Fact]
    public void TwoNodes_BidirectionalEdge_ReturnsSingleScc()
    {
        // Arrange
        var adjacency = new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = ["B"],
            ["B"] = ["A"]
        };

        // Act
        var sccs = Normalise(SccDetector.FindSccs<string>(adjacency));

        // Assert
        Assert.Single(sccs);
        Assert.Equal(2, sccs[0].Count);
        Assert.Contains("A", sccs[0]);
        Assert.Contains("B", sccs[0]);
    }

    [Fact]
    public void TwoNodes_UnidirectionalEdge_ReturnsTwoTrivialSccs()
    {
        // Arrange
        var adjacency = new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = ["B"],
            ["B"] = []
        };

        // Act
        var sccs = Normalise(SccDetector.FindSccs<string>(adjacency));

        // Assert
        Assert.Equal(2, sccs.Count);
        Assert.All(sccs, scc => Assert.Single(scc));
        Assert.Contains(sccs, scc => scc.Contains("A"));
        Assert.Contains(sccs, scc => scc.Contains("B"));
    }

    [Fact]
    public void ThreeNodes_ChainNoCycle_ReturnsThreeTrivialSccs()
    {
        // Arrange — A → B → C
        var adjacency = new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = ["B"],
            ["B"] = ["C"],
            ["C"] = []
        };

        // Act
        var sccs = Normalise(SccDetector.FindSccs<string>(adjacency));

        // Assert
        Assert.Equal(3, sccs.Count);
        Assert.All(sccs, scc => Assert.Single(scc));
        Assert.Contains(sccs, scc => scc.Contains("A"));
        Assert.Contains(sccs, scc => scc.Contains("B"));
        Assert.Contains(sccs, scc => scc.Contains("C"));
    }

    [Fact]
    public void ThreeNodes_FullCycle_ReturnsSingleScc()
    {
        // Arrange — A → B → C → A
        var adjacency = new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = ["B"],
            ["B"] = ["C"],
            ["C"] = ["A"]
        };

        // Act
        var sccs = Normalise(SccDetector.FindSccs<string>(adjacency));

        // Assert
        Assert.Single(sccs);
        Assert.Equal(3, sccs[0].Count);
        Assert.Equal(new[] { "A", "B", "C" }, sccs[0]);
    }

    [Fact]
    public void MixedGraph_TwoSccsConnectedByUnidirectional_ReturnsBothSccs()
    {
        // Arrange — A ↔ B (SCC {A,B}) and A → C (trivial SCC {C})
        var adjacency = new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = ["B", "C"],
            ["B"] = ["A"],
            ["C"] = []
        };

        // Act
        var sccs = Normalise(SccDetector.FindSccs<string>(adjacency));

        // Assert
        Assert.Equal(2, sccs.Count);

        // Larger SCC first (size-descending sort)
        Assert.Equal(2, sccs[0].Count);
        Assert.Equal(new[] { "A", "B" }, sccs[0]);

        Assert.Single(sccs[1]);
        Assert.Equal(new[] { "C" }, sccs[1]);
    }
}