using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging.Abstractions;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     Constructs the Domain <see cref="Procedure" /> graphs and their backing
///     <see cref="DummyRuntimeAgent" /> mocks for the reactive-execution convergence benchmark.
///     Each builder returns a fully wired procedure together with the agents that own the skills
///     referenced by its skill-execution tasks, ready to be seeded into the in-memory repository
///     and registered with the benchmark agent manager.
/// </summary>
/// <remarks>
///     <para>
///         <b>Dependency-type encoding.</b> A dependency type is expressed purely through the
///         <see cref="DependencyEdge.SourceHandle" />/<see cref="DependencyEdge.TargetHandle" /> pair,
///         decoded by <c>EdgeTypeMapper.Map</c>:
///         <c>(right,left)=FinishToStart</c>, <c>(left,left)=StartToStart</c>,
///         <c>(left,right)=StartToFinish</c>, <c>(right,right)=FinishToFinish</c>.
///         The SS+FF coupling that binds a <c>Hold</c>/<c>Weld</c> core is therefore two edges between
///         the same node pair carrying different handle pairs.
///     </para>
///     <para>
///         <b>Adaptive skills.</b> A skill is adaptive when its <see cref="Skill.Name" /> contains
///         "Adaptive" (the mechanism <see cref="DummyRuntimeAgent" /> uses); the robot mock's hold skill
///         is named "Hold (Adaptive)" so it stretches to track its human partner.
///     </para>
///     <para>
///         <b>Nominal duration.</b> Each skill carries a <c>NominalDuration</c> input
///         <see cref="TypedProperty" /> read by <c>DummyRuntimeAgent.GetExecutionEstimateAsync</c>;
///         durations are kept small (~0.3-1 s) so a full reactive run completes in seconds.
///     </para>
///     <para>
///         <b>Role split.</b> Every shape uses two recurring mock roles: a robot mock that runs the
///         adaptive hold and any non-adaptive successors (Inspect/Shelve/Scrap), and a human mock that
///         runs the paced Weld under a <see cref="DummyRuntimeAgentPacingConfig" />. Only the human mock
///         receives the supplied pacing config; robot mocks use the default behaviour.
///     </para>
/// </remarks>
public static class ProcedureBuilder
{
    /// <summary>
    ///     Handle marking the right-hand (finish) port of a node.
    /// </summary>
    private const string RightHandle = "right";

    /// <summary>
    ///     Handle marking the left-hand (start) port of a node.
    /// </summary>
    private const string LeftHandle = "left";

    /// <summary>
    ///     Default nominal duration, in seconds, for the adaptive hold skill.
    /// </summary>
    private const double HoldNominalDuration = 0.5;

    /// <summary>
    ///     Default nominal duration, in seconds, for the human-paced weld skill.
    /// </summary>
    private const double WeldNominalDuration = 0.5;

    /// <summary>
    ///     Default nominal duration, in seconds, for non-adaptive successor skills
    ///     (Inspect, Shelve, Scrap) and the pure finish-to-start control chain.
    /// </summary>
    private const double SuccessorNominalDuration = 0.3;

    #region Public builders

    /// <summary>
    ///     Builds the optional finish-to-start control chain: a pure FS chain of
    ///     <paramref name="length" /> non-adaptive skills with no coupling. Convergence is trivially
    ///     exact and reschedules are minimal, so this is the safest first smoke shape.
    /// </summary>
    /// <param name="humanPacing">Pacing configuration applied to the human mock that owns the chain.</param>
    /// <param name="length">Number of skills in the chain (2-4).</param>
    /// <returns>The procedure and the single agent that owns every skill in the chain.</returns>
    public static (Procedure Procedure, IReadOnlyList<DummyRuntimeAgent> Agents) FsControl(
        DummyRuntimeAgentPacingConfig humanPacing,
        int length = 4)
    {
        if (length < 2)
            throw new ArgumentOutOfRangeException(nameof(length), length, "FsControl needs at least two nodes.");

        var procedureId = Guid.NewGuid();
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();

        var humanId = Guid.NewGuid();
        var humanSkills = new List<Skill>();

        SkillExecutionNode? previous = null;
        for (var i = 0; i < length; i++)
        {
            var skill = NonAdaptiveSkill($"Step{i + 1}", SuccessorNominalDuration);
            humanSkills.Add(skill);
            var node = SkillNode(procedureId, skill, humanId, SuccessorNominalDuration, null, i);
            nodes.Add(node);

            if (previous is not null)
                edges.Add(FinishToStart(procedureId, previous.Id, node.Id));

            previous = node;
        }

        var human = BuildAgent(humanId, "HumanMock", humanSkills, humanPacing);

        var procedure = Assemble(procedureId, "FsControl", nodes, edges, []);
        var agents = new List<DummyRuntimeAgent> { human };
        AssertAgentSkillBindings(procedure, agents);
        return (procedure, agents);
    }

    /// <summary>
    ///     Builds the minimal convergence unit: a single coupled <c>Welding</c> core containing the
    ///     adaptive <c>Hold (Adaptive)</c> skill (robot mock) and the human-paced <c>Weld</c> skill,
    ///     joined by SS and FF coupling. This reproduces the live trace's "Hold-Weld portion" headlessly.
    /// </summary>
    /// <param name="humanPacing">Pacing configuration applied to the human mock that runs <c>Weld</c>.</param>
    /// <returns>The procedure together with its robot and human mocks.</returns>
    public static (Procedure Procedure, IReadOnlyList<DummyRuntimeAgent> Agents) HoldWeld(
        DummyRuntimeAgentPacingConfig humanPacing)
    {
        var procedureId = Guid.NewGuid();
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();

        var robotId = Guid.NewGuid();
        var humanId = Guid.NewGuid();
        var robotSkills = new List<Skill>();
        var humanSkills = new List<Skill>();

        BuildWeldingCore(procedureId, robotId, humanId, robotSkills, humanSkills, nodes, edges, 0, 0);

        var robot = BuildAgent(robotId, "RobotMock", robotSkills);
        var human = BuildAgent(humanId, "HumanMock", humanSkills, humanPacing);

        var procedure = Assemble(procedureId, "HoldWeld", nodes, edges, []);
        var agents = new List<DummyRuntimeAgent> { robot, human };
        AssertAgentSkillBindings(procedure, agents);
        return (procedure, agents);
    }

    /// <summary>
    ///     Builds the paper's running example: a coupled <c>Welding</c> core, an
    ///     FS-separated robot <c>Inspect</c> that writes the <c>quality</c> variable, and a <c>Sort</c>
    ///     router branching to a <c>Store{Shelve}</c> or <c>Discard{Scrap}</c> path. Confirms convergence
    ///     holds with a downstream finish-to-start successor and a router/branch present.
    /// </summary>
    /// <param name="humanPacing">Pacing configuration applied to the human mock that runs <c>Weld</c>.</param>
    /// <returns>The nine-node procedure together with its robot and human mocks.</returns>
    public static (Procedure Procedure, IReadOnlyList<DummyRuntimeAgent> Agents) HoldWeldInspectSort(
        DummyRuntimeAgentPacingConfig humanPacing)
    {
        var procedureId = Guid.NewGuid();
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();

        var robotId = Guid.NewGuid();
        var humanId = Guid.NewGuid();
        var robotSkills = new List<Skill>();
        var humanSkills = new List<Skill>();

        // Coupled Welding core (Hold + Weld).
        var welding = BuildWeldingCore(
            procedureId, robotId, humanId, robotSkills, humanSkills, nodes, edges, 0, 0);

        // Robot Inspect, finish-to-start after the Welding container, writing the quality variable.
        var inspectSkill = NonAdaptiveSkill("Inspect", SuccessorNominalDuration) with
        {
            Properties =
            [
                NominalDurationProperty(SuccessorNominalDuration),
                new TypedProperty
                {
                    Name = "quality",
                    Value = TypedValue.Text("OK"),
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding
                    {
                        VariableName = "quality",
                        Mode = BindingMode.Write
                    }
                }
            ]
        };
        robotSkills.Add(inspectSkill);
        var inspect = SkillNode(procedureId, inspectSkill, robotId, SuccessorNominalDuration, null, 2);
        nodes.Add(inspect);
        edges.Add(FinishToStart(procedureId, welding.Id, inspect.Id));

        // Sort router, finish-to-start after Inspect, branching on the quality variable. Its id is
        // generated up front so the Store and Discard branch containers can be nested beneath it: the
        // router subsystem resolves branch membership through the ParentId hierarchy, so each branch's
        // container and skill must descend from the router rather than connect to it by an edge.
        var sortId = Guid.NewGuid();

        // Store branch container (nested under the router) with its Shelve skill (robot mock).
        var shelveSkill = NonAdaptiveSkill("Shelve", SuccessorNominalDuration);
        robotSkills.Add(shelveSkill);
        var store = TaskContainer(procedureId, "Store", 3, sortId);
        var shelve = SkillNode(procedureId, shelveSkill, robotId, SuccessorNominalDuration, store.Id, 0);
        nodes.Add(store);
        nodes.Add(shelve);

        // Discard branch container (nested under the router) with its Scrap skill (robot mock).
        var scrapSkill = NonAdaptiveSkill("Scrap", SuccessorNominalDuration);
        robotSkills.Add(scrapSkill);
        var discard = TaskContainer(procedureId, "Discard", 4, sortId);
        var scrap = SkillNode(procedureId, scrapSkill, robotId, SuccessorNominalDuration, discard.Id, 0);
        nodes.Add(discard);
        nodes.Add(scrap);

        // The router branches on the quality variable. The robot mock runs Inspect with an output config of
        // UseConfiguredValues = true (see BuildProcedure below), so the dummy honors the "quality" output's
        // configured value and deterministically writes "OK"; the Store condition quality == "OK" is a real,
        // evaluable match, realizing the running example's rho(Sort) = Store. Discard is the default branch
        // (empty Condition): the branch selector requires a default so that any run where the conditional did
        // not match still resolves rather than throwing NoBranchMatchException. Each branch targets its direct
        // child container nested above; no router -> branch dependency edge is added, since the analyzer
        // derives the Router.Start prerequisite for branch skills from the hierarchy and a finish-to-start
        // edge into a branch would instead make its skills wait on Router.Finish (published only after the
        // branch completes) and deadlock.
        var sort = new RouterNode
        {
            Id = sortId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 250 },
            RouterTask = new RouterTask
            {
                Name = "Sort",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality" },
                Branches =
                [
                    new ConditionalBranch
                    {
                        Name = "Store",
                        Condition = "quality == \"OK\"",
                        Priority = 0,
                        TargetNodeId = store.Id
                    },
                    new ConditionalBranch
                    {
                        Name = "Discard",
                        Priority = 999,
                        TargetNodeId = discard.Id
                    }
                ]
            }
        };
        nodes.Add(sort);
        edges.Add(FinishToStart(procedureId, inspect.Id, sort.Id));

        var qualityVariable = new VariableDefinition
        {
            Name = "quality",
            Type = new StringType(),
            DefaultValue = "OK",
            Scope = VariableScope.Procedure
        };

        // The robot mock runs Inspect, whose "quality" output the Sort router evaluates. It is configured to
        // honor configured output values so the dummy writes the property's "OK" verbatim (a real router
        // condition) instead of a synthetic placeholder. The linchpin has two halves: this config AND the
        // output property's non-null configured value, asserted below.
        var robot = BuildAgent(
            robotId, "RobotMock", robotSkills,
            outputConfig: new DummyRuntimeAgentOutputConfig { UseConfiguredValues = true });
        var human = BuildAgent(humanId, "HumanMock", humanSkills, humanPacing);

        AssertConfiguredOutputPresent(inspectSkill, "quality");

        var procedure = Assemble(procedureId, "HoldWeldInspectSort", nodes, edges, [qualityVariable]);
        var agents = new List<DummyRuntimeAgent> { robot, human };
        AssertAgentSkillBindings(procedure, agents);
        return (procedure, agents);
    }

    /// <summary>
    ///     Builds <paramref name="width" /> independent coupled <c>Welding</c> cores that run
    ///     concurrently, stressing the reschedule loop with several adaptive variables at once. The
    ///     serialization constraint forces one robot mock and one human mock per seam.
    /// </summary>
    /// <param name="humanPacing">Pacing configuration applied to every human mock.</param>
    /// <param name="width">Number of concurrent seams (typically 2 or 4).</param>
    /// <returns>The procedure together with its <paramref name="width" /> robot and human mocks.</returns>
    public static (Procedure Procedure, IReadOnlyList<DummyRuntimeAgent> Agents) ParallelSeams(
        DummyRuntimeAgentPacingConfig humanPacing,
        int width)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width), width, "ParallelSeams needs at least one seam.");

        var procedureId = Guid.NewGuid();
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();
        var agents = new List<DummyRuntimeAgent>();

        for (var seam = 0; seam < width; seam++)
        {
            var robotId = Guid.NewGuid();
            var humanId = Guid.NewGuid();
            var robotSkills = new List<Skill>();
            var humanSkills = new List<Skill>();

            BuildWeldingCore(
                procedureId, robotId, humanId, robotSkills, humanSkills, nodes, edges, seam, seam);

            agents.Add(BuildAgent(robotId, $"RobotMock{seam + 1}", robotSkills));
            agents.Add(BuildAgent(humanId, $"HumanMock{seam + 1}", humanSkills, humanPacing));
        }

        var procedure = Assemble(procedureId, $"ParallelSeams_{width}", nodes, edges, []);
        AssertAgentSkillBindings(procedure, agents);
        return (procedure, agents);
    }

    /// <summary>
    ///     Builds <paramref name="depth" /> coupled <c>Welding</c> cores chained in series by
    ///     finish-to-start edges between consecutive containers, sharing one robot mock and one
    ///     human mock. Convergence must propagate down the chain so cumulative drift can be measured.
    /// </summary>
    /// <param name="humanPacing">Pacing configuration applied to the shared human mock.</param>
    /// <param name="depth">Number of cores in the chain (typically 2 or 4).</param>
    /// <returns>The procedure together with its single robot and single human mock.</returns>
    public static (Procedure Procedure, IReadOnlyList<DummyRuntimeAgent> Agents) SequentialPipeline(
        DummyRuntimeAgentPacingConfig humanPacing,
        int depth)
    {
        if (depth < 1)
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "SequentialPipeline needs at least one core.");

        var procedureId = Guid.NewGuid();
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();

        var robotId = Guid.NewGuid();
        var humanId = Guid.NewGuid();
        var robotSkills = new List<Skill>();
        var humanSkills = new List<Skill>();

        TaskNode? previousCore = null;
        for (var stage = 0; stage < depth; stage++)
        {
            var core = BuildWeldingCore(
                procedureId, robotId, humanId, robotSkills, humanSkills, nodes, edges, stage, stage);

            if (previousCore is not null)
                edges.Add(FinishToStart(procedureId, previousCore.Id, core.Id));

            previousCore = core;
        }

        var robot = BuildAgent(robotId, "RobotMock", robotSkills);
        var human = BuildAgent(humanId, "HumanMock", humanSkills, humanPacing);

        var procedure = Assemble(procedureId, $"SequentialPipeline_{depth}", nodes, edges, []);
        var agents = new List<DummyRuntimeAgent> { robot, human };
        AssertAgentSkillBindings(procedure, agents);
        return (procedure, agents);
    }

    #endregion

    #region Core construction helpers

    /// <summary>
    ///     Adds one coupled <c>Welding</c> core to the supplied node/edge collections: a
    ///     <see cref="TaskNode" /> container holding the adaptive <c>Hold (Adaptive)</c> skill (robot mock)
    ///     and the human-paced <c>Weld</c> skill, joined by SS (Hold to Weld) and FF (Weld to Hold) coupling.
    ///     The skills are appended to the corresponding role skill lists so the agents can own them.
    /// </summary>
    /// <param name="procedureId">Identifier of the procedure the core belongs to.</param>
    /// <param name="robotId">Identifier of the robot mock that runs the hold skill.</param>
    /// <param name="humanId">Identifier of the human mock that runs the weld skill.</param>
    /// <param name="robotSkills">Accumulator for the robot mock's skills; the hold skill is appended.</param>
    /// <param name="humanSkills">Accumulator for the human mock's skills; the weld skill is appended.</param>
    /// <param name="nodes">Accumulator the container and its two leaf nodes are appended to.</param>
    /// <param name="edges">Accumulator the two coupling edges are appended to.</param>
    /// <param name="indexY">Vertical layout index used to space cores apart in the layout.</param>
    /// <param name="seam">Seam index used to disambiguate skill names across multiple cores.</param>
    /// <returns>The <see cref="TaskNode" /> container of the core, used as the FS endpoint for chaining.</returns>
    private static TaskNode BuildWeldingCore(
        Guid procedureId,
        Guid robotId,
        Guid humanId,
        List<Skill> robotSkills,
        List<Skill> humanSkills,
        List<Node> nodes,
        List<DependencyEdge> edges,
        int indexY,
        int seam)
    {
        var suffix = seam > 0 ? $" {seam + 1}" : string.Empty;

        var welding = TaskContainer(procedureId, $"Welding{suffix}", indexY);
        nodes.Add(welding);

        var holdSkill = AdaptiveSkill($"Hold (Adaptive){suffix}", HoldNominalDuration);
        robotSkills.Add(holdSkill);
        var hold = SkillNode(procedureId, holdSkill, robotId, HoldNominalDuration, welding.Id, 0);
        nodes.Add(hold);

        var weldSkill = NonAdaptiveSkill($"Weld{suffix}", WeldNominalDuration);
        humanSkills.Add(weldSkill);
        var weld = SkillNode(procedureId, weldSkill, humanId, WeldNominalDuration, welding.Id, 1);
        nodes.Add(weld);

        // SS(Hold,Weld): both seams start together; FF(Weld,Hold): the hold finishes with the weld.
        edges.Add(StartToStart(procedureId, hold.Id, weld.Id));
        edges.Add(FinishToFinish(procedureId, weld.Id, hold.Id));

        return welding;
    }

    #endregion

    #region Node and edge factories

    /// <summary>
    ///     Creates a plain <see cref="TaskNode" /> container used to group skill nodes or to anchor a
    ///     router branch. When <paramref name="parentId" /> is a router id, the container becomes that
    ///     router's branch subtree: the router subsystem (branch navigator, serialization validator, and
    ///     dependency-graph analyzer) determines branch membership by walking <see cref="Node.ParentId" />,
    ///     so a branch's nodes must be hierarchical descendants of the router, not merely edge-connected.
    /// </summary>
    /// <param name="procedureId">Identifier of the procedure the node belongs to.</param>
    /// <param name="name">Display name of the container.</param>
    /// <param name="indexY">Vertical layout index for spacing.</param>
    /// <param name="parentId">Optional parent the container is nested within (a router id for a branch container).</param>
    /// <returns>The constructed container node.</returns>
    private static TaskNode TaskContainer(Guid procedureId, string name, int indexY, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = indexY * 150 },
            Task = new Task
            {
                Name = name,
                StartTime = 0,
                Duration = 0
            }
        };
    }

    /// <summary>
    ///     Creates a <see cref="SkillExecutionNode" /> assigning the given skill to the given agent.
    ///     The task's <see cref="SkillExecutionTask.StartTime" /> is zero and its duration is the skill's
    ///     nominal duration; <see cref="SkillExecutionTask.AgentId" /> must be a registered agent id.
    /// </summary>
    /// <param name="procedureId">Identifier of the procedure the node belongs to.</param>
    /// <param name="skill">The skill executed by this node.</param>
    /// <param name="agentId">Identifier of the agent assigned to execute the skill.</param>
    /// <param name="nominalDuration">Planned duration of the task in seconds.</param>
    /// <param name="parentId">Optional container the node is nested within.</param>
    /// <param name="indexY">Vertical layout index for spacing.</param>
    /// <returns>The constructed skill-execution node.</returns>
    private static SkillExecutionNode SkillNode(
        Guid procedureId,
        Skill skill,
        Guid agentId,
        double nominalDuration,
        Guid? parentId,
        int indexY)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            ParentId = parentId,
            Position = new NodePosition { X = 20, Y = indexY * 60 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skill.Name,
                Description = skill.Description,
                StartTime = 0,
                Duration = nominalDuration,
                Skill = skill,
                AgentId = agentId
            }
        };
    }

    /// <summary>
    ///     Creates a finish-to-start dependency edge: the target cannot start until the source finishes.
    /// </summary>
    /// <param name="procedureId">Identifier of the procedure the edge belongs to.</param>
    /// <param name="sourceId">Identifier of the predecessor node.</param>
    /// <param name="targetId">Identifier of the successor node.</param>
    /// <returns>The constructed dependency edge with right-to-left handles.</returns>
    private static DependencyEdge FinishToStart(Guid procedureId, Guid sourceId, Guid targetId)
    {
        return Edge(procedureId, sourceId, targetId, RightHandle, LeftHandle);
    }

    /// <summary>
    ///     Creates a start-to-start dependency edge: the source and target start together.
    /// </summary>
    /// <param name="procedureId">Identifier of the procedure the edge belongs to.</param>
    /// <param name="sourceId">Identifier of the source node.</param>
    /// <param name="targetId">Identifier of the target node.</param>
    /// <returns>The constructed dependency edge with left-to-left handles.</returns>
    private static DependencyEdge StartToStart(Guid procedureId, Guid sourceId, Guid targetId)
    {
        return Edge(procedureId, sourceId, targetId, LeftHandle, LeftHandle);
    }

    /// <summary>
    ///     Creates a finish-to-finish dependency edge: the source and target finish together.
    /// </summary>
    /// <param name="procedureId">Identifier of the procedure the edge belongs to.</param>
    /// <param name="sourceId">Identifier of the source node.</param>
    /// <param name="targetId">Identifier of the target node.</param>
    /// <returns>The constructed dependency edge with right-to-right handles.</returns>
    private static DependencyEdge FinishToFinish(Guid procedureId, Guid sourceId, Guid targetId)
    {
        return Edge(procedureId, sourceId, targetId, RightHandle, RightHandle);
    }

    /// <summary>
    ///     Creates a dependency edge with explicit source and target handles. The handle pair is the
    ///     sole encoding of the dependency type decoded downstream by the edge-type mapper.
    /// </summary>
    /// <param name="procedureId">Identifier of the procedure the edge belongs to.</param>
    /// <param name="sourceId">Identifier of the source node.</param>
    /// <param name="targetId">Identifier of the target node.</param>
    /// <param name="sourceHandle">Source-side handle.</param>
    /// <param name="targetHandle">Target-side handle.</param>
    /// <returns>The constructed dependency edge.</returns>
    private static DependencyEdge Edge(
        Guid procedureId,
        Guid sourceId,
        Guid targetId,
        string sourceHandle,
        string targetHandle)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = sourceHandle,
            TargetHandle = targetHandle
        };
    }

    #endregion

    #region Skill factories

    /// <summary>
    ///     Creates an adaptive skill whose name embeds "Adaptive" so the dummy agent treats it as
    ///     stretchable, carrying a <c>NominalDuration</c> input property.
    /// </summary>
    /// <param name="name">Skill name; must contain "Adaptive".</param>
    /// <param name="nominalDuration">Nominal duration in seconds.</param>
    /// <returns>The constructed adaptive skill.</returns>
    private static Skill AdaptiveSkill(string name, double nominalDuration)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = name,
            Properties = [NominalDurationProperty(nominalDuration)]
        };
    }

    /// <summary>
    ///     Creates a non-adaptive skill carrying a <c>NominalDuration</c> input property.
    /// </summary>
    /// <param name="name">Skill name; must not contain "Adaptive".</param>
    /// <param name="nominalDuration">Nominal duration in seconds.</param>
    /// <returns>The constructed non-adaptive skill.</returns>
    private static Skill NonAdaptiveSkill(string name, double nominalDuration)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = name,
            Properties = [NominalDurationProperty(nominalDuration)]
        };
    }

    /// <summary>
    ///     Creates the <c>NominalDuration</c> input property read by
    ///     <c>DummyRuntimeAgent.GetExecutionEstimateAsync</c> to seed the planned duration.
    /// </summary>
    /// <param name="nominalDuration">Nominal duration in seconds.</param>
    /// <returns>The constructed typed property.</returns>
    private static TypedProperty NominalDurationProperty(double nominalDuration)
    {
        return new TypedProperty
        {
            Name = "NominalDuration",
            Value = TypedValue.Number(nominalDuration),
            Direction = PropertyDirection.Input
        };
    }

    #endregion

    #region Agent and procedure assembly

    /// <summary>
    ///     Builds a <see cref="DummyRuntimeAgent" /> owning exactly the supplied skill instances. The
    ///     same <see cref="Skill" /> references that back the agent's nodes are handed to it so that
    ///     id-based skill resolution succeeds at execution time. A pacing configuration is supplied only
    ///     for human mocks; an output configuration is supplied for a robot mock whose skills produce
    ///     outputs a router must evaluate deterministically.
    /// </summary>
    /// <param name="agentId">Identifier of the agent.</param>
    /// <param name="name">Display name of the agent.</param>
    /// <param name="skills">The exact skill instances the agent can execute.</param>
    /// <param name="pacingConfig">Optional pacing configuration; null leaves default behaviour.</param>
    /// <param name="outputConfig">Optional output configuration; null leaves default type-based simulation.</param>
    /// <returns>The constructed dummy runtime agent.</returns>
    private static DummyRuntimeAgent BuildAgent(
        Guid agentId,
        string name,
        IEnumerable<Skill> skills,
        DummyRuntimeAgentPacingConfig? pacingConfig = null,
        DummyRuntimeAgentOutputConfig? outputConfig = null)
    {
        return new DummyRuntimeAgent(
            agentId,
            name,
            skills,
            NullLogger<DummyRuntimeAgent>.Instance,
            pacingConfig,
            outputConfig);
    }

    /// <summary>
    ///     Assembles the final <see cref="Procedure" /> from its nodes, edges, and variables, deriving
    ///     the root-node ids as the top-level nodes (those without a parent) that have no incoming
    ///     finish-to-start edge.
    /// </summary>
    /// <param name="procedureId">Identifier assigned to the procedure.</param>
    /// <param name="name">Display name of the procedure.</param>
    /// <param name="nodes">All nodes of the procedure.</param>
    /// <param name="edges">All dependency edges of the procedure.</param>
    /// <param name="variables">Variable definitions of the procedure.</param>
    /// <returns>The fully wired procedure.</returns>
    private static Procedure Assemble(
        Guid procedureId,
        string name,
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges,
        IReadOnlyList<VariableDefinition> variables)
    {
        var nodeById = nodes.ToDictionary(n => n.Id);

        // A finish-to-start edge marks its target as depending on a predecessor, so such targets
        // are not roots. Coupling edges (SS/FF) do not gate the start in the same way and are ignored.
        var fsTargets = edges
            .Where(e => string.Equals(e.SourceHandle, RightHandle, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(e.TargetHandle, LeftHandle, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.TargetId)
            .ToHashSet();

        var rootNodeIds = nodes
            .Where(n => n.ParentId is null && !fsTargets.Contains(n.Id))
            .Select(n => n.Id)
            .ToList();

        return new Procedure
        {
            Id = procedureId,
            Name = name,
            Nodes = nodes,
            Edges = edges,
            Variables = variables.ToList(),
            RootNodeIds = rootNodeIds,
            RootNodes = rootNodeIds.Select(id => nodeById[id]).ToList()
        };
    }

    #endregion

    #region Builder-time validation

    /// <summary>
    ///     Verifies that the named output property of a skill carries a non-null configured value. A robot
    ///     mock built with <see cref="DummyRuntimeAgentOutputConfig.UseConfiguredValues" /> honors a
    ///     configured output value only when it is present; a null value falls through to a synthetic
    ///     placeholder, which would silently break a router condition that reads the output and route to the
    ///     default branch instead. Asserting it here fails loudly at build time rather than at run time.
    /// </summary>
    /// <param name="skill">The skill whose output property is validated.</param>
    /// <param name="outputPropertyName">The name of the output property that must carry a value.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the named output property is absent or carries a null value.
    /// </exception>
    private static void AssertConfiguredOutputPresent(Skill skill, string outputPropertyName)
    {
        var property = skill.Properties.FirstOrDefault(p =>
            p.Name == outputPropertyName
            && p.Direction is PropertyDirection.Output or PropertyDirection.InputOutput);

        if (property is null)
            throw new InvalidOperationException(
                $"Skill '{skill.Name}' has no output property named '{outputPropertyName}'.");

        if (property.Value.Value is null)
            throw new InvalidOperationException(
                $"Output property '{outputPropertyName}' on skill '{skill.Name}' carries a null value. " +
                "A mock configured with UseConfiguredValues honors a configured output only when present; " +
                "a null value falls through to a synthetic placeholder and breaks router conditions on it.");
    }

    /// <summary>
    ///     Verifies that every <see cref="SkillExecutionTask" /> in the procedure references a registered
    ///     agent and that the assigned <see cref="Skill" /> id appears in that agent's available skills.
    ///     A mismatch would cause the execution initializer to silently skip the node (or the run to hang
    ///     when the adaptive skill never receives its finish signal), so it is surfaced here at build time.
    /// </summary>
    /// <param name="procedure">The procedure whose skill-execution tasks are validated.</param>
    /// <param name="agents">The agents that should own every assigned skill.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a task references an unregistered agent id, or assigns a skill id that the
    ///     referenced agent does not own.
    /// </exception>
    private static void AssertAgentSkillBindings(Procedure procedure, IReadOnlyList<DummyRuntimeAgent> agents)
    {
        var skillIdsByAgent = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var agent in agents)
        {
            var skillIds = agent
                .GetAvailableSkillsAsync()
                .GetAwaiter()
                .GetResult()
                .Select(s => s.Id)
                .ToHashSet();
            skillIdsByAgent[agent.Id] = skillIds;
        }

        var skillNodes = (procedure.Nodes ?? [])
            .OfType<SkillExecutionNode>()
            .Select(n => n.SkillExecutionTask);

        foreach (var task in skillNodes)
        {
            if (!skillIdsByAgent.TryGetValue(task.AgentId, out var ownedSkillIds))
                throw new InvalidOperationException(
                    $"Skill '{task.Skill.Name}' is assigned to unregistered agent id '{task.AgentId}'. " +
                    "Every SkillExecutionTask.AgentId must be a registered agent.");

            if (!ownedSkillIds.Contains(task.Skill.Id))
                throw new InvalidOperationException(
                    $"Agent '{task.AgentId}' does not own skill '{task.Skill.Name}' (id '{task.Skill.Id}'). " +
                    "Every assigned Skill.Id must be in the agent's available skills.");
        }
    }

    #endregion
}