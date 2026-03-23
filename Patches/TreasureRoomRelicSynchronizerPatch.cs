using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using Rock.Runtime;

namespace Rock.Patches;

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer))]
internal static class TreasureRoomRelicSynchronizerPatch
{
    [HarmonyPatch(nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
    [HarmonyPostfix]
    private static void AfterBeginRelicPicking(TreasureRoomRelicSynchronizer __instance)
    {
        Rock.Infrastructure.RockLog.Trace("Sync", $"BeginRelicPicking postfix currentRelics={__instance.CurrentRelics?.Count ?? 0}.");
        RockRuntime.Coordinator.BeginSession(
            __instance.CurrentRelics,
            Services.TreasureRoomRelicSynchronizerAccessor.GetPlayers(__instance));
    }

    [HarmonyPatch("AwardRelics")]
    [HarmonyPrefix]
    private static bool BeforeAwardRelics(TreasureRoomRelicSynchronizer __instance)
    {
        bool allowOriginal = !RockRuntime.Coordinator.ShouldInterceptAutomaticAward(__instance);
        Rock.Infrastructure.RockLog.Trace("Sync", $"AwardRelics prefix allowOriginal={allowOriginal}.");
        return allowOriginal;
    }

    [HarmonyPatch("EndRelicVoting")]
    [HarmonyPrefix]
    private static bool BeforeEndRelicVoting(TreasureRoomRelicSynchronizer __instance)
    {
        bool allowOriginal = !RockRuntime.Coordinator.ShouldDelayEndRelicVoting(__instance);
        Rock.Infrastructure.RockLog.Trace("Sync", $"EndRelicVoting prefix allowOriginal={allowOriginal}.");
        return allowOriginal;
    }

    [HarmonyPatch("EndRelicVoting")]
    [HarmonyPostfix]
    private static void AfterEndRelicVoting()
    {
        if (RockRuntime.Coordinator.IsAwaitingManualResolution)
        {
            Rock.Infrastructure.RockLog.Trace("Sync", "EndRelicVoting postfix detected awaiting manual resolution; keeping session alive.");
            return;
        }

        Rock.Infrastructure.RockLog.Trace("Sync", "EndRelicVoting postfix ending session.");
        RockRuntime.Coordinator.EndSession();
    }

    [HarmonyPatch(nameof(TreasureRoomRelicSynchronizer.CompleteWithNoRelics))]
    [HarmonyPostfix]
    private static void AfterCompleteWithNoRelics()
    {
        RockRuntime.Coordinator.EndSession();
    }
}
