using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Positioning;

/// <summary>
///     Integration test to verify that the scheduling configuration can be loaded from appsettings.json.
/// </summary>
public class ConfigurationIntegrationTest
{
    [Fact]
    public void Configuration_CanBeLoadedFromAppSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Scheduling:Positioning:TimeToPixelScale"] = "150.0",
                ["Scheduling:Positioning:BaseYOffset"] = "75.0",
                ["Scheduling:Positioning:SiblingSpacing"] = "80.0",
                ["Scheduling:Positioning:ContainerTopPadding"] = "35.0",
                ["Scheduling:Positioning:ContainerBottomPadding"] = "15.0",
                ["Scheduling:Positioning:BaseHeight"] = "60.0"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<SchedulingConfiguration>(configuration.GetSection("Scheduling"));
        services.AddSingleton<INodePositionXCalculator, NodePositionXCalculator>();
        services.AddSingleton<INodePositionYCalculator, NodePositionYCalculator>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var xCalculator = serviceProvider.GetRequiredService<INodePositionXCalculator>();
        var yCalculator = serviceProvider.GetRequiredService<INodePositionYCalculator>();

        // Assert
        Assert.Equal(150.0, xCalculator.TimeToPixelScale);
        Assert.Equal(75.0, yCalculator.ChildVerticalOffset);
        Assert.Equal(35.0, yCalculator.ContainerTopPadding);
        Assert.Equal(15.0, yCalculator.ContainerBottomPadding);
        Assert.Equal(60.0, yCalculator.BaseHeight);
    }

    [Fact]
    public void Configuration_UsesDefaultsWhenNotConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build(); // Empty configuration

        var services = new ServiceCollection();
        services.Configure<SchedulingConfiguration>(configuration.GetSection("Scheduling"));
        services.AddSingleton<INodePositionXCalculator, NodePositionXCalculator>();
        services.AddSingleton<INodePositionYCalculator, NodePositionYCalculator>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var xCalculator = serviceProvider.GetRequiredService<INodePositionXCalculator>();
        var yCalculator = serviceProvider.GetRequiredService<INodePositionYCalculator>();

        // Assert - Should use defaults defined in PositioningConfiguration
        Assert.Equal(100.0, xCalculator.TimeToPixelScale);
        Assert.Equal(50.0, yCalculator.ChildVerticalOffset);
        Assert.Equal(30.0, yCalculator.ContainerTopPadding);
        Assert.Equal(10.0, yCalculator.ContainerBottomPadding);
        Assert.Equal(50.0, yCalculator.BaseHeight);
    }
}