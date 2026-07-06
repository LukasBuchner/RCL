# Agent Serialization — Proofs

> Graph-level conditions under which every feasible LP solution temporally separates same-agent skills, mechanised in Lean 4.

## Overview

Agent serialization is a safety invariant: no physical agent may be asked to run two skills simultaneously. The procedure graph expresses *soft* ordering via dependency edges, and the LP scheduler converts those edges into numerical start and finish times. Without a formal argument, nothing prevents the LP from parallelising two same-agent skills it is not forbidden to parallelise — the scheduler minimises makespan and will gladly do so.

The theorems below close this gap. They identify a **graph-level** sufficient condition — an FS-first path in the prerequisite graph — that forces the LP to separate any two same-agent skills in time, in every feasible solution.

## Setup

Let

- $V$ be the finite set of skill and task nodes in the loaded procedure.
- $A$ the set of physical agents, with $\alpha : V \to A$ the agent assignment. Two nodes $a, b \in V$ are *same-agent* when $\alpha(a) = \alpha(b)$ and $a \ne b$.
- $E_{FS}, E_{SS} \subseteq V \times V$ the sets of finish-to-start and start-to-start prerequisite edges. $(a, b) \in E_{FS}$ means "$b$ lists $a$ as an FS prerequisite"; temporal direction is $a \to b$ ($a$ finishes before $b$ starts). Similarly for $E_{SS}$ with "starts before".
- An LP solution $\sigma = (S, F, D, M)$ assigns each $v \in V$ a start $S_v \in \mathbb{R}$, finish $F_v \in \mathbb{R}$, duration $D_v \in \mathbb{R}$, and fixes a global makespan $M$. Feasibility requires:
  - **Duration linkage.** $F_v = S_v + D_v$ for every $v \in V$.
  - **Duration bounds.** $0 \le d^{\min}_v \le D_v \le d^{\max}_v$ for every $v \in V$.
  - **FS constraint.** For every $(a, b) \in E_{FS}$, $S_b \ge F_a$.
  - **SS constraint.** For every $(a, b) \in E_{SS}$, $S_b \ge S_a$.

Non-overlap of a pair $(a, b)$ is expressed as the disjunction $S_b \ge F_a \lor S_a \ge F_b$ — one of the two intervals $[S_\cdot, F_\cdot)$ ends on or before the other begins.

Router branches partition a router's subtree into mutually exclusive alternatives. Two nodes are *router-exclusive* when they lie under distinct branch targets of a common ancestor router; such pairs never co-execute in any run and need no ordering.

### Definition 1 (Prerequisite-edge relations).
The FS and SS edge relations on $V$ are
$$
(a, b) \in E_{FS} \iff b \text{ lists } a \text{ as an FS prereq},
\qquad
(a, b) \in E_{SS} \iff b \text{ lists } a \text{ as an SS prereq}.
$$

### Definition 2 (AnyPath).
$\mathrm{AnyPath} \subseteq V \times V$ is the transitive closure of $E_{FS} \cup E_{SS}$. Equivalently, $\mathrm{AnyPath}(a, b)$ holds iff there is a sequence $a = v_0, v_1, \dots, v_k = b$ with $k \ge 1$ and each $(v_{i-1}, v_i) \in E_{FS} \cup E_{SS}$.

### Definition 3 (FsThenAny).
$\mathrm{FsThenAny}(a, b)$ holds iff there exists $m \in V$ such that $(a, m) \in E_{FS}$ and either $m = b$ or $\mathrm{AnyPath}(m, b)$. The path from $a$ to $b$ starts with an FS edge and continues through any mixture of FS and SS edges.

### Lemma 4 (Start-monotonicity along $\mathrm{AnyPath}$).
$$
\forall a, b \in V.\ \mathrm{AnyPath}(a, b) \implies S_a \le S_b
\qquad \text{(in every feasible LP solution).}
$$

*Proof sketch.* Induction on path length. A single FS step $(a, b) \in E_{FS}$ gives $F_a \le S_b$; duration linkage with $D_a \ge 0$ gives $S_a \le F_a$, so $S_a \le S_b$. A single SS step gives $S_a \le S_b$ directly. Composition is transitivity of $\le$. $\square$

### Theorem 5 (FS-first prevents overlap — L2 soundness).
In every feasible LP solution,
$$
\forall x, y \in V.\ \mathrm{FsThenAny}(x, y) \implies S_y \ge F_x.
$$

*Proof sketch.* Unfold the witness. If the path is a single FS edge $(x, y)$, the FS constraint gives $F_x \le S_y$. Otherwise it is an FS step $(x, m)$ followed by $\mathrm{AnyPath}(m, y)$. The FS step yields $F_x \le S_m$; Lemma 4 yields $S_m \le S_y$; transitivity gives $F_x \le S_y$. $\square$

### Theorem 6 (Agent serialization soundness).
Assume that for every same-agent non-router-exclusive pair $(a, b)$,
$$
\mathrm{FsThenAny}(a, b) \lor \mathrm{FsThenAny}(b, a).
$$
Then every feasible LP solution satisfies
$$
S_b \ge F_a \lor S_a \ge F_b
$$
for every such pair — no two skills on the same agent overlap in time.

*Proof sketch.* Fix a pair. Pick either direction from the hypothesis; Theorem 5 supplies the required inequality. $\square$

### Corollary 6.1 (Level 1 — pure FS chains).
If every same-agent non-router-exclusive pair is connected by a chain of FS edges only — writing $\mathrm{FsReachable}$ for the transitive closure of $E_{FS}$, $\mathrm{FsReachable}(a, b) \lor \mathrm{FsReachable}(b, a)$ — the conclusion of Theorem 6 holds.

*Proof sketch.* An FS chain is an $\mathrm{FsThenAny}$: the first edge is FS and the remainder forms an $\mathrm{AnyPath}$. Apply Theorem 6. $\square$

## Conservatism — soundness without completeness

The converse of Theorem 6 is not proved. Consider the graph
$$
x \xrightarrow{SS} u \xrightarrow{FS} y,
$$
with duration bounds satisfying $d^{\min}_u \ge d^{\max}_x$. Every feasible LP solution has
$$
S_y \ge F_u \ge S_u + d^{\min}_u \ge S_x + d^{\max}_x \ge F_x,
$$
so the pair is already serialised. Yet $\lnot\mathrm{FsThenAny}(x, y)$, because the first edge on the only path is SS. The graph-level rule therefore rejects the graph and suggests adding an FS edge.

Closing this gap would require Farkas-style reasoning or a difference-constraint-system argument over the full LP polytope with duration bounds, introducing substantial polyhedral machinery. It is deliberately out of scope. The rule is graph-only and conservative; the suggested fix — add an FS edge — is always safe and imposes no runtime cost, so the conservatism has no operational downside.

## Event-driven dispatch and the router caveat

Freydis dispatches against **events**, not LP times. A node becomes ready when `combineLatestFired bus np.startPrereqs` holds — every listed prerequisite event must have fired on the bus. The LP's $S$ and $F$ are a projection of the same constraint algebra, not a runtime command.

Under event-driven dispatch the guarantee is realised by **event-chain propagation**. Let $x$ and $y$ share an agent and let $\pi$ be an FS-first witness path from $x$ to $y$. The chain's last edge is, by construction, a listed start-prerequisite of $y$. Walk the chain backward: each intermediate node's outgoing event is a listed start-prerequisite of its successor, terminating at $y$. `combineLatestFired` is an AND over its inputs, so $y$'s start is held off until every event in the chain has fired. The first link is FS, so $y$'s gate cannot open before $F_x$. The two cannot overlap on the agent.

**Unselected-branch corollary.** If $\pi$ threads through a node in an unselected router branch, that node never runs and its start/finish events never fire. $y$'s `combineLatestFired` gate therefore never satisfies and $y$ deadlocks. Safety (no overlap) is preserved *because* the AND-semantics stalls $y$ rather than letting it run on partial evidence. A broken chain cannot produce a simultaneous dispatch.

Deadlock-avoidance is a separate invariant, proved in `NoDeadlocks.lean` and `RouterBranchConsistency.lean`; the theorems above guarantee agent-serialization safety only.

## Mechanisation

| Artifact | Location | Status |
|---|---|---|
| `hasFSPrereqEdge`, `hasSSPrereqEdge`, `FsReachable` | `Sunstone/Sunstone/Common/PrerequisiteGraph.lean`, `Sunstone/Sunstone/Scheduling/AgentSerialization.lean` | `sorry`-free |
| `AnyEdge`, `AnyPath`, `FsThenAny` | `AgentSerialization.lean` | `sorry`-free |
| `single_fs_prevents_overlap`, `single_ss_preserves_start`, `fs_chain_prevents_overlap`, `finish_geq_start` | `AgentSerialization.lean` | `sorry`-free |
| `anyPath_start_monotone` (Lemma 4) | `AgentSerialization.lean` | `sorry`-free |
| `fs_reachable_prevents_overlap` (L1 core) | `AgentSerialization.lean` | `sorry`-free |
| `fsThenAny_prevents_overlap` (Theorem 5) | `AgentSerialization.lean` | `sorry`-free |
| `agent_serialization_sound` (Theorem 6) | `AgentSerialization.lean` | `sorry`-free |
| `agent_serialization_sound_fs_only` (Corollary 6.1) | `AgentSerialization.lean` | `sorry`-free |
| `MutuallyExclusive`, `mutuallyExclusive_symm` | `AgentSerialization.lean` | `sorry`-free |

Every Lean hypothesis — `h_fs_constraint`, `h_ss_constraint`, `h_linkage`, `h_bounds` — is audited against its C# enforcement point in [`Sunstone/assumption-audit.md`](../../../Sunstone/docs/assumption-audit.md). Lean ↔ C# declaration mapping is in [`Sunstone/cross-reference.md`](../../../Sunstone/docs/cross-reference.md).

### Lemma 4 — Lean statement

```lean
lemma anyPath_start_monotone
    (pg : PrereqGraph) (sol : LPSolution) (taskOf : ℕ → ScheduledTask)
    (h_fs_constraint : …) (h_ss_constraint : …)
    (h_linkage : …) (h_bounds : …)
    {a b : ℕ} (h_path : AnyPath pg a b) :
    sol.S (taskOf a) ≤ sol.S (taskOf b)
```

The feasibility hypotheses are instantiated from `depConstraint` / `durationLinkage` / `durationBounds` in `Sunstone/Sunstone/Scheduling/LPSchedulingValidity.lean`.

### Theorem 5 — Lean statement

```lean
theorem fsThenAny_prevents_overlap
    … {a b : ℕ} (h_reach : FsThenAny pg a b) :
    sol.S (taskOf b) ≥ sol.F (taskOf a)
```

### Theorem 6 — Lean statement

```lean
theorem agent_serialization_sound
    … -- hypothesis list extends the L1 form with `h_ss_constraint`
```

The conclusion is the standard inequality-disjunction — no explicit `overlap` predicate is introduced, matching the convention in `LPSchedulingValidity.lean`.

### Corollary 6.1 — Lean statement

```lean
theorem agent_serialization_sound_fs_only
    …
```

Direct instantiation of `fs_reachable_prevents_overlap`. Kept as a drop-in for callers that construct `FsReachable` witnesses without needing the SS-constraint hypothesis.

## Related Documentation

- [Agent Serialization overview](README.md)
- [Validator](validator.md) — C# counterpart of Theorem 6
- [Verification](verification.md) — how to build the proofs and run the validator tests
- [Sunstone Proofs](../../../Sunstone/README.md) — Lean 4 verification index
- [Assumption Audit](../../../Sunstone/docs/assumption-audit.md) — every Lean hypothesis versus its C# enforcement point
- [Lean ↔ C# Cross Reference](../../../Sunstone/docs/cross-reference.md)
