namespace FHOOE.Freydis.Domain.Entities.Common;

public record PositionTag
{
    public Guid Id { get; set; }

    public required string Tag { get; set; }

    public required Position Position { get; set; }
}