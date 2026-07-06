namespace FHOOE.Freydis.Domain.Entities.Common;

public record SceneObject
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required Position Position { get; set; }
}