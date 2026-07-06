namespace FHOOE.Freydis.Domain.Entities.Common;

public record Skill
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required List<TypedProperty> Properties { get; set; }
}