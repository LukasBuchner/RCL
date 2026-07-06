using System.Runtime.InteropServices;
using FHOOE.Freydis.Application.Services.Common.Support.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Common.Platform;

/// <summary>
///     Hosted service that sets the Windows system timer resolution to 1ms on startup.
///     <para>
///         By default, Windows clamps <see cref="System.Threading.Timer" /> (and therefore
///         Rx.NET's <c>Scheduler.Default</c>) to ~15.625ms (~64 Hz). This makes sub-16ms
///         intervals specified via <c>Observable.Sample</c>, <c>Observable.Timer</c>, etc.
///         silently degrade to the system tick rate.
///     </para>
///     <para>
///         Calling <c>timeBeginPeriod(1)</c> from <c>winmm.dll</c> raises the resolution
///         to 1ms for the lifetime of the process, restoring accurate timing for all
///         timer-based operations. On Linux and macOS the timer resolution is already
///         sufficient, so this service is a no-op on non-Windows platforms.
///     </para>
/// </summary>
public sealed partial class WindowsTimerResolutionService : IHostedService
{
    private readonly ILogger<WindowsTimerResolutionService> _logger;
    private bool _active;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WindowsTimerResolutionService" /> class.
    /// </summary>
    /// <param name="logger">Logger for recording timer resolution changes.</param>
    public WindowsTimerResolutionService(ILogger<WindowsTimerResolutionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Sets the Windows system timer resolution to 1ms if running on Windows.
    ///     On non-Windows platforms this method is a no-op.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token (unused).</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ = TimeBeginPeriod(1);
            _active = true;
            _logger.LogTimerResolutionSet();
        }
        else
        {
            _logger.LogNonWindowsPlatform(RuntimeInformation.OSDescription);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Restores the default Windows timer resolution if it was previously raised.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token (unused).</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_active)
        {
            _ = TimeEndPeriod(1);
            _active = false;
            _logger.LogTimerResolutionRestored();
        }

        return Task.CompletedTask;
    }

    [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static partial uint TimeBeginPeriod(uint uMilliseconds);

    [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static partial uint TimeEndPeriod(uint uMilliseconds);
}