namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

[OneOf]
public record PropertyTypeInput
{
    public BooleanPropertyInput? BooleanProperty { get; set; }
    public NumberPropertyInput? NumberProperty { get; set; }
    public StringPropertyInput? StringProperty { get; set; }
    public PositionPropertyInput? PositionProperty { get; set; }
    public PositionTagPropertyInput? PositionTagProperty { get; set; }
    public SceneObjectPropertyInput? SceneObjectProperty { get; set; }
}