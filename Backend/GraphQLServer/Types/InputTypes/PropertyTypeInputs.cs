using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

public record BooleanPropertyInput
{
    public bool Value { get; set; }
}

public record NumberPropertyInput
{
    public double Value { get; set; }
}

public record StringPropertyInput
{
    public string? Value { get; set; }
}

public record PositionPropertyInput
{
    public Position? Value { get; set; }
}

public record PositionTagPropertyInput
{
    public PositionTagInput? Value { get; set; }
}

public record SceneObjectPropertyInput
{
    public SceneObjectInput? Value { get; set; }
}