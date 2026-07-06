using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.GraphQLServer.Types;

public class CreateAgentPayload
{
    public Agent? Agent { get; set; }
}

public class CreateDependencyEdgePayload
{
    public DependencyEdge? DependencyEdge { get; set; }
}

public class CreateNodePayload
{
    public Node? Node { get; set; }
}

public class CreatePositionTagPayload
{
    public PositionTag? PositionTag { get; set; }
}

public class CreateSceneObjectPayload
{
    public SceneObject? SceneObject { get; set; }
}

public class CreateSkillPayload
{
    public Skill? Skill { get; set; }
}

public class DeleteAgentPayload
{
    public bool Boolean { get; set; }
}

public class DeleteDependencyEdgePayload
{
    public bool Boolean { get; set; }
}

public class DeleteNodePayload
{
    public bool Boolean { get; set; }
}

public class DeletePositionTagPayload
{
    public bool Boolean { get; set; }
}

public class DeleteSceneObjectPayload
{
    public bool Boolean { get; set; }
}

public class DeleteSkillPayload
{
    public bool Boolean { get; set; }
}

public class StartLoadedProcedurePayload
{
    public bool Boolean { get; set; }
}

public class UpdateAgentPayload
{
    public Agent? Agent { get; set; }
}

public class UpdateDependencyEdgePayload
{
    public bool Boolean { get; set; }
}

public class UpdateNodePayload
{
    public bool Boolean { get; set; }
}

public class UpdatePositionTagPayload
{
    public bool Boolean { get; set; }
}

public class UpdateSceneObjectPayload
{
    public bool Boolean { get; set; }
}

public class UpdateSkillPayload
{
    public Skill? Skill { get; set; }
}

public class CreateProcedurePayload
{
    public Procedure? Procedure { get; set; }
}

public class DeleteProcedurePayload
{
    public bool Boolean { get; set; }
}

public class LoadProcedurePayload
{
    public Procedure? Procedure { get; set; }
}

public class UnloadProcedurePayload
{
    public bool Success { get; set; }
}