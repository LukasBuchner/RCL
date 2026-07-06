namespace FHOOE.Freydis.Application.Configuration;

/// <summary>
///     Configuration options for scheduling and positioning services.
/// </summary>
public class SchedulingConfiguration
{
    /// <summary>
    ///     Configuration for node positioning calculations.
    /// </summary>
    public PositioningConfiguration Positioning { get; set; } = new();

    /// <summary>
    ///     Default values for node creation.
    /// </summary>
    public DefaultsConfiguration Defaults { get; set; } = new();
}

/// <summary>
///     Default values for node creation.
/// </summary>
public class DefaultsConfiguration
{
    /// <summary>
    ///     Default duration for empty task nodes (in time units).
    ///     Used when creating new tasks, including router branch tasks.
    ///     Default is 200 time units.
    /// </summary>
    public double DefaultTaskDuration { get; set; } = 200.0;
}

/// <summary>
///     Configuration for node positioning calculations.
/// </summary>
public class PositioningConfiguration
{
    /// <summary>
    ///     Scale factor for converting time units to pixels for X-axis positioning.
    ///     Default is 100 pixels per time unit.
    /// </summary>
    public double TimeToPixelScale { get; set; } = 100.0;

    /// <summary>
    ///     Base Y offset for root-level nodes.
    ///     Default is 50 pixels.
    /// </summary>
    public double BaseYOffset { get; set; } = 50.0;

    /// <summary>
    ///     Vertical spacing between sibling nodes at the same level.
    ///     Default is 60 pixels.
    /// </summary>
    public double SiblingSpacing { get; set; } = 60.0;

    /// <summary>
    ///     Top padding inside container nodes before the first child.
    ///     Default is 30 pixels.
    /// </summary>
    public double ContainerTopPadding { get; set; } = 30.0;

    /// <summary>
    ///     Bottom padding inside container nodes after the last child.
    ///     Default is 10 pixels.
    /// </summary>
    public double ContainerBottomPadding { get; set; } = 10.0;

    /// <summary>
    ///     Base height for leaf nodes (nodes without children).
    ///     Default is 50 pixels.
    /// </summary>
    public double BaseHeight { get; set; } = 50.0;

    /// <summary>
    ///     Additional height for RouterNode dropdown selector.
    ///     Only applied when the RouterNode has branches.
    ///     Default is 26 pixels (20px dropdown + 6px spacing).
    /// </summary>
    public double RouterDropdownHeight { get; set; } = 26.0;
}