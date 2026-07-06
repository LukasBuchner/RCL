namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

public record SkillInput
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public List<PropertyInput>? Properties { get; set; }
}