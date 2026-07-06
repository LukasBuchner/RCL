namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

public class AgentInput
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required List<Guid> SkillIds { get; set; }

    public required string RepresentativeColor { get; set; }
}