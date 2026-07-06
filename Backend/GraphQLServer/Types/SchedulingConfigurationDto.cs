namespace FHOOE.Freydis.GraphQLServer.Types;

/// <summary>
///     Data Transfer Object for scheduling configuration exposed via GraphQL.
///     Provides frontend with necessary configuration values for rendering.
/// </summary>
public class SchedulingConfigurationDto
{
    /// <summary>
    ///     Scale factor for converting time units to pixels for X-axis positioning.
    ///     Used by frontend to render timeline and calculate node widths.
    /// </summary>
    public double TimeToPixelScale { get; set; }

    /// <summary>
    ///     Base Y offset for root-level nodes in pixels.
    /// </summary>
    public double BaseYOffset { get; set; }

    /// <summary>
    ///     Vertical spacing between sibling nodes at the same level in pixels.
    /// </summary>
    public double SiblingSpacing { get; set; }

    /// <summary>
    ///     Top padding inside container nodes before the first child in pixels.
    /// </summary>
    public double ContainerTopPadding { get; set; }

    /// <summary>
    ///     Bottom padding inside container nodes after the last child in pixels.
    /// </summary>
    public double ContainerBottomPadding { get; set; }

    /// <summary>
    ///     Base height for leaf nodes (nodes without children) in pixels.
    /// </summary>
    public double BaseHeight { get; set; }

    /// <summary>
    ///     Additional height for RouterNode dropdown selector in pixels.
    /// </summary>
    public double RouterDropdownHeight { get; set; }
}