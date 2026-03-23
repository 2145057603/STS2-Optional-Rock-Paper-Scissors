using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using Rock.Runtime;

namespace Rock.Patches;

[HarmonyPatch(
    typeof(PlayerChoiceSynchronizer),
    MethodType.Constructor,
    typeof(INetGameService),
    typeof(IPlayerCollection))]
internal static class PlayerChoiceSynchronizerPatch
{
    [HarmonyPostfix]
    private static void AfterConstruct(PlayerChoiceSynchronizer __instance)
    {
        RockRuntime.Coordinator.AttachChoiceSynchronizer(__instance);
    }
}
