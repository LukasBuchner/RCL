using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

public class CreateAgentInput
{
    public required AgentInput AgentInput { get; set; }
}

public class CreateDependencyEdgeInput
{
    public required DependencyEdgeInput DependencyEdge { get; set; }
}

public class CreateNodeInput
{
    public required NodeInput NodeInput { get; set; }
}

public class CreatePositionTagInput
{
    public required PositionTagInput PositionTag { get; set; }
}

public class CreateSceneObjectInput
{
    public required SceneObjectInput SceneObject { get; set; }
}

public class CreateSkillInput
{
    public required SkillInput SkillInput { get; set; }
}

public class DeleteAgentInput
{
    public required Guid Id { get; set; }
}

public class DeleteDependencyEdgeInput
{
    public required Guid Id { get; set; }
}

public class DeleteNodeInput
{
    public required Guid Id { get; set; }
}

public class DeletePositionTagInput
{
    public required Guid Id { get; set; }
}

public class DeleteSceneObjectInput
{
    public required Guid Id { get; set; }
}

public class DeleteSkillInput
{
    public required Guid Id { get; set; }
}

public class CreateProcedureInput
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class DeleteProcedureInput
{
    public required Guid Id { get; set; }
}

public class UpdateAgentInput
{
    public required Guid Id { get; set; }
    public required AgentInput AgentInput { get; set; }
}

public class UpdateDependencyEdgeInput
{
    public required DependencyEdgeInput DependencyEdge { get; set; }
}

public class UpdateNodeInput
{
    public required NodeInput NodeInput { get; set; }
}

public class UpdatePositionTagInput
{
    public required PositionTagInput PositionTag { get; set; }
}

public class UpdateSceneObjectInput
{
    public required SceneObjectInput SceneObject { get; set; }
}

public class UpdateSkillInput
{
    public required Guid Id { get; set; }
    public required SkillInput SkillInput { get; set; }
}

public class DependencyEdgeInput
{
    public required Guid Id { get; set; }
    public required Guid SourceId { get; set; }
    public required Guid TargetId { get; set; }
    public string? SourceHandle { get; set; }
    public string? TargetHandle { get; set; }
}

public class SceneObjectInput
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Position Position { get; set; }
}

public class PositionTagInput
{
    public required Guid Id { get; set; }
    public required string Tag { get; set; }
    public required Position Position { get; set; }
}