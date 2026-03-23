using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Rock.Core;
using Rock.Infrastructure;
using Rock.Runtime;

namespace Rock;

[ModInitializer(nameof(Initialize))]
public static class RockModInitializer
{
    public static void Initialize()
    {
        RockRuntime.Initialize();

        Harmony harmony = new(RockModInfo.HarmonyId);
        harmony.PatchAll(typeof(RockModInitializer).Assembly);

        RockLog.Info("Harmony patches applied.");
    }
}
