using Rock.Infrastructure;
using Rock.Services;

namespace Rock.Runtime;

internal static class RockRuntime
{
    private static bool _initialized;

    public static ManualRpsCoordinator Coordinator { get; } = new();

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        RockLog.Info("Runtime initialized.");
    }
}
