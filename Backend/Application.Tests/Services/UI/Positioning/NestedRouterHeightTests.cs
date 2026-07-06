using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Positioning;

/// <summary>
///     Tests verifying that nested router hierarchies produce correct heights.
///     Uses the real <see cref="NodePositionYCalculator"/> and <see cref="NodeHeightCalculator"/>
///     to detect integration issues that mocked tests would miss.
///     <para>
///         Hierarchy under test:
///         <c>OuterRouter → BranchTaskA → InnerRouter → BranchTask2 → SkillNode</c>
///     </para>
/// </summary>
public class NestedRouterHeightTests
{
    private const double BaseHeight = 50.0;
    private const double RouterDropdownHeight = 26.0;
    private const double ContainerTopPadding = 30.0;
    private const double ContainerBottomPadding = 10.0;
    private const double SiblingSpacing = 60.0;

    private readonly NodeHeightCalculator _heightCalculator;
    private readonly NodePositionYCalculator _positionYCalculator;

    public NestedRouterHeightTests()
    {
        var schedulingConfig = new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                BaseHeight = BaseHeight,
                RouterDropdownHeight = RouterDropdownHeight,
                ContainerTopPadding = ContainerTopPadding,
                ContainerBottomPadding = ContainerBottomPadding,
                SiblingSpacing = SiblingSpacing
            }
        };

        _positionYCalculator = new NodePositionYCalculator(Options.Create(schedulingConfig));
        _heightCalculator = new NodeHeightCalculator(
            _positionYCalculator,
            new Mock<ILogger<NodeHeightCalculator>>().Object);
    }

    /// <summary>
    ///     Verifies that an inner RouterNode (nested inside an outer router's branch task)
    ///     receives the correct container height that accounts for both its dropdown selector
    ///     and its children.
    ///     <para>
    ///         Hierarchy: <c>OuterRouter → BranchTaskA → InnerRouter → SkillNode</c>
    ///     </para>
    ///     <para>
    ///         Expected InnerRouter height:
    ///         <c>ContainerTopPadding(30) + RouterDropdown(26) + SkillLeafHeight(50) + ContainerBottomPadding(10) = 116</c>
    ///     </para>
    /// </summary>
    [Fact]
    public void NestedRouterAsContainer_MustIncludeDropdownInContainerHeight()
    {
        // Arrange — OuterRouter → BranchTaskA → InnerRouter → SkillNode
        var outerRouter = CreateRouterNode("OuterRouter", 2);
        var branchTaskA = CreateTaskNode("BranchTaskA", outerRouter.Id);
        var innerRouter = CreateRouterNode("InnerRouter", 2, branchTaskA.Id);
        var skillNode = CreateSkillNode("Skill1", innerRouter.Id);

        var allNodes = new List<Node> { outerRouter, branchTaskA, innerRouter, skillNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouter.Id] = new List<Node> { branchTaskA },
            [branchTaskA.Id] = new List<Node> { innerRouter },
            [innerRouter.Id] = new List<Node> { skillNode }
        };

        // Act
        var heights = _heightCalculator.CalculateNodeHeights(allNodes, parentToChildrenMapping);

        // Assert — InnerRouter as a container with one skill child
        // InnerRouter height = ContainerTopPadding(30) + RouterDropdown(26) + SkillHeight(50) + ContainerBottomPadding(10) = 116
        var expectedInnerRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + BaseHeight + ContainerBottomPadding;
        Assert.Equal(expectedInnerRouterHeight, heights[innerRouter.Id]); // 116

        // BranchTaskA height = ContainerTopPadding(30) + InnerRouterHeight(116) + ContainerBottomPadding(10) = 156
        var expectedBranchTaskHeight = ContainerTopPadding + expectedInnerRouterHeight + ContainerBottomPadding;
        Assert.Equal(expectedBranchTaskHeight, heights[branchTaskA.Id]); // 156

        // OuterRouter height = ContainerTopPadding(30) + RouterDropdown(26) + BranchTaskHeight(156) + ContainerBottomPadding(10) = 222
        var expectedOuterRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + expectedBranchTaskHeight + ContainerBottomPadding;
        Assert.Equal(expectedOuterRouterHeight, heights[outerRouter.Id]); // 222
    }

    /// <summary>
    ///     Verifies that an inner RouterNode's height correctly cascades into the outer
    ///     router's total height when the inner router has multiple children (branch tasks
    ///     with sibling spacing between them).
    ///     <para>
    ///         Hierarchy:
    ///         <c>OuterRouter → BranchTaskA → InnerRouter → { SkillA, SkillB }</c>
    ///     </para>
    ///     <para>
    ///         Expected InnerRouter height:
    ///         <c>ContainerTopPadding(30) + RouterDropdown(26) + Skill(50) + SiblingSpacing(60) + Skill(50) + ContainerBottomPadding(10) = 226</c>
    ///     </para>
    /// </summary>
    [Fact]
    public void NestedRouterWithMultipleChildren_HeightCascadesCorrectlyToOuterRouter()
    {
        // Arrange — OuterRouter → BranchTaskA → InnerRouter → { SkillA, SkillB }
        var outerRouter = CreateRouterNode("OuterRouter", 2);
        var branchTaskA = CreateTaskNode("BranchTaskA", outerRouter.Id);
        var innerRouter = CreateRouterNode("InnerRouter", 2, branchTaskA.Id);
        var skillA = CreateSkillNode("SkillA", innerRouter.Id);
        var skillB = CreateSkillNode("SkillB", innerRouter.Id);

        var allNodes = new List<Node> { outerRouter, branchTaskA, innerRouter, skillA, skillB };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouter.Id] = new List<Node> { branchTaskA },
            [branchTaskA.Id] = new List<Node> { innerRouter },
            [innerRouter.Id] = new List<Node> { skillA, skillB }
        };

        // Act
        var heights = _heightCalculator.CalculateNodeHeights(allNodes, parentToChildrenMapping);

        // Assert
        // InnerRouter: topPadding(30) + dropdown(26) + skill(50) + spacing(60) + skill(50) + bottomPadding(10) = 226
        var expectedInnerRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + BaseHeight + SiblingSpacing + BaseHeight +
            ContainerBottomPadding;
        Assert.Equal(expectedInnerRouterHeight, heights[innerRouter.Id]); // 226

        // BranchTaskA: topPadding(30) + innerRouter(226) + bottomPadding(10) = 266
        var expectedBranchTaskHeight = ContainerTopPadding + expectedInnerRouterHeight + ContainerBottomPadding;
        Assert.Equal(expectedBranchTaskHeight, heights[branchTaskA.Id]); // 266

        // OuterRouter: topPadding(30) + dropdown(26) + branchTask(266) + bottomPadding(10) = 332
        var expectedOuterRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + expectedBranchTaskHeight + ContainerBottomPadding;
        Assert.Equal(expectedOuterRouterHeight, heights[outerRouter.Id]); // 332
    }

    /// <summary>
    ///     Verifies correctness for a three-level nesting scenario:
    ///     <c>Router1 → BranchTask1 → Router2 → BranchTask2 → Router3 → Skill</c>.
    ///     The height of each level must correctly incorporate the dropdown of every
    ///     nested router it contains.
    /// </summary>
    [Fact]
    public void ThreeLevelNestedRouters_AllHeightsAccountForDropdowns()
    {
        // Arrange — Router1 → BT1 → Router2 → BT2 → Router3 → Skill
        var router1 = CreateRouterNode("Router1", 2);
        var bt1 = CreateTaskNode("BT1", router1.Id);
        var router2 = CreateRouterNode("Router2", 2, bt1.Id);
        var bt2 = CreateTaskNode("BT2", router2.Id);
        var router3 = CreateRouterNode("Router3", 2, bt2.Id);
        var skill = CreateSkillNode("Skill", router3.Id);

        var allNodes = new List<Node> { router1, bt1, router2, bt2, router3, skill };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [router1.Id] = new List<Node> { bt1 },
            [bt1.Id] = new List<Node> { router2 },
            [router2.Id] = new List<Node> { bt2 },
            [bt2.Id] = new List<Node> { router3 },
            [router3.Id] = new List<Node> { skill }
        };

        // Act
        var heights = _heightCalculator.CalculateNodeHeights(allNodes, parentToChildrenMapping);

        // Assert — build expected heights bottom-up
        // Router3: top(30) + dropdown(26) + skill(50) + bottom(10) = 116
        var expectedRouter3 = ContainerTopPadding + RouterDropdownHeight + BaseHeight + ContainerBottomPadding;
        Assert.Equal(expectedRouter3, heights[router3.Id]);

        // BT2: top(30) + router3(116) + bottom(10) = 156
        var expectedBt2 = ContainerTopPadding + expectedRouter3 + ContainerBottomPadding;
        Assert.Equal(expectedBt2, heights[bt2.Id]);

        // Router2: top(30) + dropdown(26) + bt2(156) + bottom(10) = 222
        var expectedRouter2 = ContainerTopPadding + RouterDropdownHeight + expectedBt2 + ContainerBottomPadding;
        Assert.Equal(expectedRouter2, heights[router2.Id]);

        // BT1: top(30) + router2(222) + bottom(10) = 262
        var expectedBt1 = ContainerTopPadding + expectedRouter2 + ContainerBottomPadding;
        Assert.Equal(expectedBt1, heights[bt1.Id]);

        // Router1: top(30) + dropdown(26) + bt1(262) + bottom(10) = 328
        var expectedRouter1 = ContainerTopPadding + RouterDropdownHeight + expectedBt1 + ContainerBottomPadding;
        Assert.Equal(expectedRouter1, heights[router1.Id]);
    }

    /// <summary>
    ///     Verifies that the outer router's branch task containing a sibling skill node
    ///     alongside a nested router gets the correct height accounting for both children.
    ///     <para>
    ///         Hierarchy: <c>OuterRouter → BranchTaskA → { InnerRouter(→Skill1), Skill2 }</c>
    ///     </para>
    /// </summary>
    [Fact]
    public void BranchTaskWithNestedRouterAndSiblingSkill_HeightAccountsForBoth()
    {
        // Arrange — OuterRouter → BranchTaskA → { InnerRouter → Skill1, Skill2 }
        var outerRouter = CreateRouterNode("OuterRouter", 2);
        var branchTaskA = CreateTaskNode("BranchTaskA", outerRouter.Id);
        var innerRouter = CreateRouterNode("InnerRouter", 2, branchTaskA.Id);
        var skill1 = CreateSkillNode("Skill1", innerRouter.Id);
        var skill2 = CreateSkillNode("Skill2", branchTaskA.Id);

        var allNodes = new List<Node> { outerRouter, branchTaskA, innerRouter, skill1, skill2 };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouter.Id] = new List<Node> { branchTaskA },
            [branchTaskA.Id] = new List<Node> { innerRouter, skill2 },
            [innerRouter.Id] = new List<Node> { skill1 }
        };

        // Act
        var heights = _heightCalculator.CalculateNodeHeights(allNodes, parentToChildrenMapping);

        // Assert
        // InnerRouter: top(30) + dropdown(26) + skill1(50) + bottom(10) = 116
        var expectedInnerRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + BaseHeight + ContainerBottomPadding;
        Assert.Equal(expectedInnerRouterHeight, heights[innerRouter.Id]);

        // BranchTaskA: top(30) + innerRouter(116) + spacing(60) + skill2(50) + bottom(10) = 266
        var expectedBranchTaskHeight =
            ContainerTopPadding + expectedInnerRouterHeight + SiblingSpacing + BaseHeight + ContainerBottomPadding;
        Assert.Equal(expectedBranchTaskHeight, heights[branchTaskA.Id]);

        // OuterRouter: top(30) + dropdown(26) + branchTask(266) + bottom(10) = 332
        var expectedOuterRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + expectedBranchTaskHeight + ContainerBottomPadding;
        Assert.Equal(expectedOuterRouterHeight, heights[outerRouter.Id]);
    }

    /// <summary>
    ///     Reproduces the production scenario from database data:
    ///     <c>OuterRouter(rfg) → { OKBranch(→Skill), NOKBranch(→InnerRouter(AnotherRouter) → { InnerBranch1(→Skill), InnerBranch2(→Skill) }) }</c>.
    ///     When only the selected branch is visible (execution mode), the outer router must
    ///     still have a height that fully contains the visible nested router.
    /// </summary>
    [Fact]
    public void ProductionScenario_OuterRouterWithSelectedBranchContainingNestedRouter_HeightIsCorrect()
    {
        // Arrange — production hierarchy (selected branch = NOK Branch containing nested router)
        var outerRouter = CreateRouterNode("rfg", 2);
        var nokBranch = CreateTaskNode("NOK Branch", outerRouter.Id);
        var innerRouter = CreateRouterNode("Another Router", 2, nokBranch.Id);
        var innerBranch1 = CreateTaskNode("Inner Branch 1", innerRouter.Id);
        var innerBranch2 = CreateTaskNode("Inner Branch 2", innerRouter.Id);
        var skillInBranch1 = CreateSkillNode("Move To Position Tag", innerBranch1.Id);
        var skillInBranch2 = CreateSkillNode("Release Object", innerBranch2.Id);

        // Execution mode: only NOK branch selected for outer router, both inner branches visible
        var allNodes = new List<Node>
        {
            outerRouter, nokBranch, innerRouter,
            innerBranch1, innerBranch2,
            skillInBranch1, skillInBranch2
        };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouter.Id] = new List<Node> { nokBranch },
            [nokBranch.Id] = new List<Node> { innerRouter },
            [innerRouter.Id] = new List<Node> { innerBranch1, innerBranch2 },
            [innerBranch1.Id] = new List<Node> { skillInBranch1 },
            [innerBranch2.Id] = new List<Node> { skillInBranch2 }
        };

        // Act
        var heights = _heightCalculator.CalculateNodeHeights(allNodes, parentToChildrenMapping);

        // Assert — build bottom-up
        // Skills are leaf: 50
        Assert.Equal(BaseHeight, heights[skillInBranch1.Id]);
        Assert.Equal(BaseHeight, heights[skillInBranch2.Id]);

        // Inner branch tasks (containers with 1 skill each):
        // top(30) + skill(50) + bottom(10) = 90
        var expectedInnerBranchHeight = ContainerTopPadding + BaseHeight + ContainerBottomPadding;
        Assert.Equal(expectedInnerBranchHeight, heights[innerBranch1.Id]); // 90
        Assert.Equal(expectedInnerBranchHeight, heights[innerBranch2.Id]); // 90

        // InnerRouter (container with 2 branch tasks):
        // top(30) + dropdown(26) + branch1(90) + spacing(60) + branch2(90) + bottom(10) = 306
        var expectedInnerRouterHeight =
            ContainerTopPadding + RouterDropdownHeight +
            expectedInnerBranchHeight + SiblingSpacing + expectedInnerBranchHeight +
            ContainerBottomPadding;
        Assert.Equal(expectedInnerRouterHeight, heights[innerRouter.Id]); // 306

        // NOK Branch (container with InnerRouter):
        // top(30) + innerRouter(306) + bottom(10) = 346
        var expectedNokBranchHeight = ContainerTopPadding + expectedInnerRouterHeight + ContainerBottomPadding;
        Assert.Equal(expectedNokBranchHeight, heights[nokBranch.Id]); // 346

        // OuterRouter (container with NOK Branch):
        // top(30) + dropdown(26) + nokBranch(346) + bottom(10) = 412
        var expectedOuterRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + expectedNokBranchHeight + ContainerBottomPadding;
        Assert.Equal(expectedOuterRouterHeight, heights[outerRouter.Id]); // 412
    }

    /// <summary>
    ///     Tests the design-mode scenario where the outer router has BOTH branches visible
    ///     (no selection yet). One branch contains a nested router, the other contains a
    ///     single skill. The outer router's height must be large enough to contain the
    ///     taller branch.
    /// </summary>
    [Fact]
    public void DesignMode_OuterRouterWithBothBranchesVisible_HeightFitsTallerBranch()
    {
        // Arrange — OuterRouter → { BranchA(→Skill), BranchB(→InnerRouter→Skill) }
        var outerRouter = CreateRouterNode("OuterRouter", 2);
        var branchA = CreateTaskNode("BranchA", outerRouter.Id);
        var branchB = CreateTaskNode("BranchB", outerRouter.Id);
        var skillInA = CreateSkillNode("SkillA", branchA.Id);
        var innerRouter = CreateRouterNode("InnerRouter", 2, branchB.Id);
        var skillInInner = CreateSkillNode("SkillInner", innerRouter.Id);

        var allNodes = new List<Node>
        {
            outerRouter, branchA, branchB, skillInA, innerRouter, skillInInner
        };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouter.Id] = new List<Node> { branchA, branchB },
            [branchA.Id] = new List<Node> { skillInA },
            [branchB.Id] = new List<Node> { innerRouter },
            [innerRouter.Id] = new List<Node> { skillInInner }
        };

        // Act
        var heights = _heightCalculator.CalculateNodeHeights(allNodes, parentToChildrenMapping);

        // Assert — build bottom-up
        // BranchA: top(30) + skill(50) + bottom(10) = 90
        var expectedBranchAHeight = ContainerTopPadding + BaseHeight + ContainerBottomPadding;
        Assert.Equal(expectedBranchAHeight, heights[branchA.Id]);

        // InnerRouter: top(30) + dropdown(26) + skill(50) + bottom(10) = 116
        var expectedInnerRouterHeight =
            ContainerTopPadding + RouterDropdownHeight + BaseHeight + ContainerBottomPadding;
        Assert.Equal(expectedInnerRouterHeight, heights[innerRouter.Id]);

        // BranchB: top(30) + innerRouter(116) + bottom(10) = 156
        var expectedBranchBHeight = ContainerTopPadding + expectedInnerRouterHeight + ContainerBottomPadding;
        Assert.Equal(expectedBranchBHeight, heights[branchB.Id]);

        // OuterRouter with both branches:
        // top(30) + dropdown(26) + branchA(90) + spacing(60) + branchB(156) + bottom(10) = 372
        var expectedOuterRouterHeight =
            ContainerTopPadding + RouterDropdownHeight +
            expectedBranchAHeight + SiblingSpacing + expectedBranchBHeight +
            ContainerBottomPadding;
        Assert.Equal(expectedOuterRouterHeight, heights[outerRouter.Id]);
    }

    /// <summary>
    ///     Exposes the <see cref="GetNodeHeight"/> inconsistency: when a leaf RouterNode
    ///     without branches is a child of a container, the container uses
    ///     <c>BaseHeight + RouterDropdownHeight = 76</c> (via <c>GetNodeHeight</c>) but the
    ///     node's own height is only <c>BaseHeight = 50</c>.
    ///     This means the parent container allocates 26 extra pixels for a RouterNode child
    ///     that doesn't actually display a dropdown.
    /// </summary>
    [Fact]
    public void LeafRouterWithoutBranches_ParentHeightMustMatchChildOwnHeight()
    {
        // Arrange — TaskParent → LeafRouter (no branches, no children)
        var parentTask = CreateTaskNode("Parent");
        var leafRouter = CreateRouterNodeWithoutBranches("LeafRouter", parentTask.Id);

        var allNodes = new List<Node> { parentTask, leafRouter };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [parentTask.Id] = new List<Node> { leafRouter }
        };

        // Act
        var heights = _heightCalculator.CalculateNodeHeights(allNodes, parentToChildrenMapping);

        // The leaf router's own height (no branches → BaseHeight only = 50)
        var leafRouterOwnHeight = heights[leafRouter.Id];
        Assert.Equal(BaseHeight, leafRouterOwnHeight); // 50

        // The parent's height should use the child's ACTUAL height (50), not GetNodeHeight (76)
        // Expected: ContainerTopPadding(30) + leafRouterOwnHeight(50) + ContainerBottomPadding(10) = 90
        var expectedParentHeight = ContainerTopPadding + leafRouterOwnHeight + ContainerBottomPadding;
        Assert.Equal(expectedParentHeight, heights[parentTask.Id]); // 90 — currently fails (returns 96)
    }

    /// <summary>
    ///     End-to-end integration test using production config values (ContainerTopPadding=40,
    ///     SiblingSpacing=10) that reproduces the bug where the inner router retains the height
    ///     for ALL its branches instead of just the selected one.
    ///     <para>
    ///         Production hierarchy (12 nodes):
    ///         <code>
    ///             rfg (OuterRouter, root)                            — ManuallySelectedBranch="Default"
    ///             ├── OK Branch (Task, parent=rfg)
    ///             │    └── skill-in-ok-branch (SkillExecution)
    ///             └── Default Branch (Task, parent=rfg)
    ///                  ├── Another Router (InnerRouter) — ManuallySelectedBranch="Branch 1"
    ///                  │    ├── Branch 1 Branch (Task) → skill-in-branch1
    ///                  │    └── Inner Default Branch (Task) → skill-in-inner-default
    ///                  └── skill-in-default-branch (SkillExecution)
    ///             root-skill-1 (SkillExecution, no parent)
    ///             root-skill-2 (SkillExecution, no parent)
    ///         </code>
    ///     </para>
    ///     <para>
    ///         After filtering, 8 nodes remain. The inner router should have ChildCount=1 and
    ///         H=176 (not 286 which would indicate both branches leaked through).
    ///     </para>
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ProductionScenario_InnerRouterHeightShrinksAfterBranchSelection()
    {
        // Use production config values
        const double prodTopPadding = 40.0;
        const double prodBottomPadding = 10.0;
        const double prodSiblingSpacing = 10.0;
        const double prodBaseHeight = 50.0;
        const double prodRouterDropdown = 26.0;

        var prodConfig = new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                BaseHeight = prodBaseHeight,
                RouterDropdownHeight = prodRouterDropdown,
                ContainerTopPadding = prodTopPadding,
                ContainerBottomPadding = prodBottomPadding,
                SiblingSpacing = prodSiblingSpacing
            }
        };
        var prodPositionYCalc = new NodePositionYCalculator(Options.Create(prodConfig));
        var prodHeightCalc = new NodeHeightCalculator(
            prodPositionYCalc, new Mock<ILogger<NodeHeightCalculator>>().Object);

        // Build the full 12-node hierarchy
        var procedureId = Guid.NewGuid();

        var outerRouterId = Guid.NewGuid();
        var okBranchId = Guid.NewGuid();
        var skillInOkBranchId = Guid.NewGuid();
        var defaultBranchId = Guid.NewGuid();
        var innerRouterId = Guid.NewGuid();
        var branch1BranchId = Guid.NewGuid();
        var skillInBranch1Id = Guid.NewGuid();
        var innerDefaultBranchId = Guid.NewGuid();
        var skillInInnerDefaultId = Guid.NewGuid();
        var skillInDefaultBranchId = Guid.NewGuid();
        var rootSkill1Id = Guid.NewGuid();
        var rootSkill2Id = Guid.NewGuid();

        var outerRouter = new RouterNode
        {
            Id = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "rfg",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality_result" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "OK", TargetNodeId = okBranchId, Priority = 0, Condition = "quality_result == 'OK'"
                    },
                    new() { Name = "Default", TargetNodeId = defaultBranchId, Priority = 1 }
                },
                ManuallySelectedBranch = "Default"
            }
        };

        var okBranch = new TaskNode
        {
            Id = okBranchId,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "OK Branch", StartTime = 0, Duration = 10 }
        };

        var skillInOkBranch = new SkillExecutionNode
        {
            Id = skillInOkBranchId,
            ParentId = okBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Skill in OK",
                StartTime = 0,
                Duration = 5,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Skill in OK", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var defaultBranch = new TaskNode
        {
            Id = defaultBranchId,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "Default Branch", StartTime = 0, Duration = 100 }
        };

        var innerRouter = new RouterNode
        {
            Id = innerRouterId,
            ParentId = defaultBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Another Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "inner_var" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "Branch 1", TargetNodeId = branch1BranchId, Priority = 0, Condition = "inner_var == 1"
                    },
                    new() { Name = "Default", TargetNodeId = innerDefaultBranchId, Priority = 1 }
                },
                ManuallySelectedBranch = "Branch 1"
            }
        };

        var branch1Branch = new TaskNode
        {
            Id = branch1BranchId,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "Branch 1 Branch", StartTime = 0, Duration = 30 }
        };

        var skillInBranch1 = new SkillExecutionNode
        {
            Id = skillInBranch1Id,
            ParentId = branch1BranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Move To Position Tag",
                StartTime = 0,
                Duration = 15,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Move To Position Tag",
                    Description = "",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };

        var innerDefaultBranch = new TaskNode
        {
            Id = innerDefaultBranchId,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "Inner Default Branch", StartTime = 0, Duration = 40 }
        };

        var skillInInnerDefault = new SkillExecutionNode
        {
            Id = skillInInnerDefaultId,
            ParentId = innerDefaultBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Release Object",
                StartTime = 0,
                Duration = 10,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Release Object", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var skillInDefaultBranch = new SkillExecutionNode
        {
            Id = skillInDefaultBranchId,
            ParentId = defaultBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Default Branch Skill",
                StartTime = 0,
                Duration = 20,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Default Branch Skill",
                    Description = "",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };

        var rootSkill1 = new SkillExecutionNode
        {
            Id = rootSkill1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Root Skill 1",
                StartTime = 0,
                Duration = 25,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Root Skill 1", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var rootSkill2 = new SkillExecutionNode
        {
            Id = rootSkill2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Root Skill 2",
                StartTime = 0,
                Duration = 25,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Root Skill 2", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var allNodes = new List<Node>
        {
            outerRouter, okBranch, skillInOkBranch, defaultBranch,
            innerRouter, branch1Branch, skillInBranch1,
            innerDefaultBranch, skillInInnerDefault,
            skillInDefaultBranch, rootSkill1, rootSkill2
        };

        // Step 1: Filter nodes
        var filterService = new RouterBranchFilterService(
            new Mock<ILogger<RouterBranchFilterService>>().Object);
        var filterResult = await filterService.FilterNodesAsync(allNodes);
        var includedNodes = filterResult.IncludedNodes;

        // Step 2: Build parentToChildrenMapping from filtered nodes (same as NodeRelationshipMapper)
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();
        var lookup = includedNodes.ToLookup(n => n.ParentId ?? Guid.Empty);
        foreach (var group in lookup)
            if (group.Key != Guid.Empty)
                parentToChildrenMapping[group.Key] = group.ToList().AsReadOnly();

        // Step 3: Calculate heights
        var heights = prodHeightCalc.CalculateNodeHeights(includedNodes, parentToChildrenMapping);

        // Assert — inner router should have exactly 1 child (Branch 1 Branch) after filtering
        var innerRouterChildren = parentToChildrenMapping.ContainsKey(innerRouterId)
            ? parentToChildrenMapping[innerRouterId]
            : [];
        Assert.Single(innerRouterChildren); // ChildCount=1, not 2

        // Assert — inner router height = top(40) + dropdown(26) + branch1Branch_height + bottom(10)
        //   where branch1Branch_height = top(40) + skill(50) + bottom(10) = 100
        //   so inner router height = 40 + 26 + 100 + 10 = 176
        var expectedBranch1Height = prodTopPadding + prodBaseHeight + prodBottomPadding; // 100
        var expectedInnerRouterHeight =
            prodTopPadding + prodRouterDropdown + expectedBranch1Height + prodBottomPadding; // 176
        Assert.Equal(expectedInnerRouterHeight, heights[innerRouterId]); // 176, NOT 286

        // Assert — Default Branch height = top(40) + innerRouter(176) + spacing(10) + skill(50) + bottom(10) = 286
        var expectedDefaultBranchHeight =
            prodTopPadding + expectedInnerRouterHeight + prodSiblingSpacing + prodBaseHeight + prodBottomPadding; // 286
        Assert.Equal(expectedDefaultBranchHeight, heights[defaultBranchId]);

        // Assert — outer router height = top(40) + dropdown(26) + defaultBranch(286) + bottom(10) = 362
        var expectedOuterRouterHeight =
            prodTopPadding + prodRouterDropdown + expectedDefaultBranchHeight + prodBottomPadding; // 362
        Assert.Equal(expectedOuterRouterHeight, heights[outerRouterId]);
    }

    #region Helper Methods

    private static RouterNode CreateRouterNodeWithoutBranches(string name, Guid? parentId = null)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                Duration = 0.0,
                StartTime = 0.0,
                FinishTime = 0.0,
                Selector = new SimpleVariableSelector { Expression = "status" },
                Branches = new List<ConditionalBranch>() // No branches
            }
        };
    }


    private static RouterNode CreateRouterNode(string name, int branchCount, Guid? parentId = null)
    {
        var branches = new List<ConditionalBranch>();
        for (var i = 0; i < branchCount; i++)
            branches.Add(new ConditionalBranch
            {
                Name = $"Branch {i}",
                Condition = $"condition{i}",
                Priority = i,
                TargetNodeId = Guid.NewGuid()
            });

        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                Duration = 0.0,
                StartTime = 0.0,
                FinishTime = 0.0,
                Selector = new SimpleVariableSelector { Expression = "status" },
                Branches = branches
            }
        };
    }

    private static TaskNode CreateTaskNode(string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = name,
                Duration = 100.0,
                StartTime = 0,
                FinishTime = 100.0
            }
        };
    }

    private static SkillExecutionNode CreateSkillNode(string name, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                Duration = 50.0,
                StartTime = 0,
                FinishTime = 50.0,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Skill for {name}",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };
    }

    #endregion
}