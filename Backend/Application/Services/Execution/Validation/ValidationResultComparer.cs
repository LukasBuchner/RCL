namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Structural equality comparer for <see cref="ProcedureValidationResult" /> used by the
///     <c>DistinctUntilChanged</c> operator inside <see cref="ProcedureValidationTracker" />.
/// </summary>
/// <remarks>
///     <para>
///         Record default equality compares list references, so two independently produced
///         results with identical violations would look different to the reactive pipeline and
///         trigger a spurious emission.  This comparer performs a deep structural check:
///         violation counts and per-violation collection sizes are compared as fast short-circuits,
///         followed by unordered-set equality on the actual skill node IDs and missing FS pair IDs.
///         This prevents <c>DistinctUntilChanged</c> from suppressing emissions when the same
///         counts appear with different underlying skill or pair identifiers.
///     </para>
///     <para>
///         Extend <see cref="Equals" /> when new validator fields are added to
///         <see cref="ProcedureValidationResult" />: add a parallel call to the appropriate
///         equality helper for the new field alongside <see cref="AgentSerializationViolationListEquals" />.
///     </para>
/// </remarks>
internal sealed class ValidationResultComparer : IEqualityComparer<ProcedureValidationResult>
{
    /// <summary>
    ///     Singleton instance.  Safe to use concurrently — the comparer holds no state.
    /// </summary>
    public static readonly ValidationResultComparer Instance = new();

    /// <inheritdoc />
    /// <summary>
    ///     Returns <see langword="true" /> when both results contain structurally identical agent
    ///     serialization violations, including deep equality of the actual skill node IDs and
    ///     missing FS pair IDs within each violation.  Reference equality is checked first as a
    ///     fast path.
    /// </summary>
    public bool Equals(ProcedureValidationResult? x, ProcedureValidationResult? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return AgentSerializationViolationListEquals(
            x.AgentSerializationViolations,
            y.AgentSerializationViolations);

        // When new validator result fields are added to ProcedureValidationResult,
        // add the corresponding structural equality check here using the same pattern.
    }

    /// <inheritdoc />
    /// <summary>
    ///     Returns a hash code based solely on the violation count.
    ///     Cheap and consistent with the structural equality defined in <see cref="Equals" />.
    /// </summary>
    public int GetHashCode(ProcedureValidationResult obj)
    {
        return obj.AgentSerializationViolations.Count;
    }

    /// <summary>
    ///     Performs a positional, deep structural comparison of two agent serialization violation
    ///     lists.  Count mismatches are checked first as O(1) short-circuits.  When counts match,
    ///     each positional element is compared by <see cref="AgentSerializationViolation.AgentId" />,
    ///     followed by unordered-set equality on the <see cref="UnserializedSkill.NodeId" /> values
    ///     and on the normalized <see cref="SkillPair" /> tuples.
    /// </summary>
    /// <param name="a">The first list to compare.</param>
    /// <param name="b">The second list to compare.</param>
    /// <returns>
    ///     <see langword="true" /> when the two lists are deeply structurally equal;
    ///     <see langword="false" /> otherwise.
    /// </returns>
    private static bool AgentSerializationViolationListEquals(
        IReadOnlyList<AgentSerializationViolation> a,
        IReadOnlyList<AgentSerializationViolation> b)
    {
        if (a.Count != b.Count) return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i].AgentId != b[i].AgentId) return false;
            if (a[i].MissingFsPairs.Count != b[i].MissingFsPairs.Count) return false;
            if (a[i].UnserializedSkills.Count != b[i].UnserializedSkills.Count) return false;
            if (!PairsEqual(a[i].MissingFsPairs, b[i].MissingFsPairs)) return false;
            if (!SkillsEqual(a[i].UnserializedSkills, b[i].UnserializedSkills)) return false;
        }

        return true;
    }

    /// <summary>
    ///     Compares two <see cref="SkillPair" /> lists as unordered sets of normalized pair tuples.
    ///     Each pair is normalized so that the smaller <see cref="Guid" /> is always placed first,
    ///     making <c>(A, B)</c> and <c>(B, A)</c> equivalent.  Count equality is a prerequisite
    ///     and must be verified by the caller before invoking this method.
    /// </summary>
    /// <param name="a">The first pair list to compare.</param>
    /// <param name="b">The second pair list to compare.</param>
    /// <returns>
    ///     <see langword="true" /> when both lists contain the same set of normalized pairs;
    ///     <see langword="false" /> otherwise.
    /// </returns>
    private static bool PairsEqual(IReadOnlyList<SkillPair> a, IReadOnlyList<SkillPair> b)
    {
        var setA = new HashSet<(Guid, Guid)>(a.Select(NormalizedPair));
        return b.All(p => setA.Contains(NormalizedPair(p)));
    }

    /// <summary>
    ///     Returns a canonical representation of a <see cref="SkillPair" /> by placing the
    ///     lexicographically smaller <see cref="Guid" /> first.  This ensures that
    ///     <c>(A, B)</c> and <c>(B, A)</c> map to the same tuple and can be compared as equal
    ///     in a <see cref="HashSet{T}" />.
    /// </summary>
    /// <param name="p">The skill pair to normalize.</param>
    /// <returns>
    ///     A <see cref="ValueTuple{T1,T2}" /> where the first element is less than or equal to
    ///     the second element according to <see cref="Guid.CompareTo(Guid)" />.
    /// </returns>
    private static (Guid, Guid) NormalizedPair(SkillPair p)
    {
        return p.SkillA.CompareTo(p.SkillB) <= 0 ? (p.SkillA, p.SkillB) : (p.SkillB, p.SkillA);
    }

    /// <summary>
    ///     Compares two <see cref="UnserializedSkill" /> lists as unordered sets of
    ///     <see cref="UnserializedSkill.NodeId" /> values.  Count equality is a prerequisite
    ///     and must be verified by the caller before invoking this method.
    /// </summary>
    /// <param name="a">The first skill list to compare.</param>
    /// <param name="b">The second skill list to compare.</param>
    /// <returns>
    ///     <see langword="true" /> when both lists contain exactly the same set of node IDs;
    ///     <see langword="false" /> otherwise.
    /// </returns>
    private static bool SkillsEqual(IReadOnlyList<UnserializedSkill> a, IReadOnlyList<UnserializedSkill> b)
    {
        var setA = new HashSet<Guid>(a.Select(s => s.NodeId));
        return b.All(s => setA.Contains(s.NodeId));
    }
}