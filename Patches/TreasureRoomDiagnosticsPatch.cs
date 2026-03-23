using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using Rock.Infrastructure;

namespace Rock.Patches;

[HarmonyPatch]
internal static class TreasureRoomDiagnosticsPatch
{
    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection.RelicPickingFinished))]
    [HarmonyPostfix]
    private static void AfterRelicPickingFinished()
    {
        RockLog.Trace("TreasureUi", "RelicPickingFinished() called and task source created.");
    }

    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection.AnimIn))]
    [HarmonyPostfix]
    private static void AfterAnimIn()
    {
        RockLog.Trace("TreasureUi", "AnimIn completed.");
    }

    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection.AnimOut))]
    [HarmonyPostfix]
    private static void AfterAnimOut()
    {
        RockLog.Trace("TreasureUi", "AnimOut completed.");
    }

    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), "OnRelicsAwarded")]
    [HarmonyPrefix]
    private static void BeforeOnRelicsAwarded(List<MegaCrit.Sts2.Core.Entities.TreasureRelicPicking.RelicPickingResult> results)
    {
        RockLog.Trace("TreasureUi", $"OnRelicsAwarded received {results.Count} result(s).");
    }

    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), "AnimateRelicAwards")]
    [HarmonyPrefix]
    private static void BeforeAnimateRelicAwards(List<MegaCrit.Sts2.Core.Entities.TreasureRelicPicking.RelicPickingResult> results)
    {
        RockLog.Trace("TreasureUi", $"AnimateRelicAwards starting with {results.Count} result(s).");
    }

    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), "AnimateRelicAwards")]
    [HarmonyPostfix]
    private static void AfterAnimateRelicAwards()
    {
        RockLog.Trace("TreasureUi", "AnimateRelicAwards completed.");
    }

    [HarmonyPatch(typeof(NTreasureRoomRelicCollection), "AnimateRelicAwards")]
    [HarmonyFinalizer]
    private static Exception? AnimateRelicAwardsFinalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            RockLog.Exception("AnimateRelicAwards failed", __exception);
        }

        return __exception;
    }

    [HarmonyPatch(typeof(NHandImageCollection), nameof(NHandImageCollection.DoFight))]
    [HarmonyPrefix]
    private static void BeforeDoFight(MegaCrit.Sts2.Core.Entities.TreasureRelicPicking.RelicPickingResult result)
    {
        int rounds = result.fight?.rounds?.Count ?? -1;
        ulong winner = result.player?.NetId ?? 0;
        RockLog.Trace("TreasureUi", $"NHandImageCollection.DoFight starting. rounds={rounds}, winner={winner}.");
    }

    [HarmonyPatch(typeof(NHandImageCollection), nameof(NHandImageCollection.DoFight))]
    [HarmonyPostfix]
    private static void AfterDoFight()
    {
        RockLog.Trace("TreasureUi", "NHandImageCollection.DoFight completed.");
    }

    [HarmonyPatch(typeof(NHandImageCollection), nameof(NHandImageCollection.DoFight))]
    [HarmonyFinalizer]
    private static Exception? DoFightFinalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            RockLog.Exception("NHandImageCollection.DoFight failed", __exception);
        }

        return __exception;
    }

    [HarmonyPatch(typeof(NTreasureRoom), "OpenChest")]
    [HarmonyPrefix]
    private static void BeforeOpenChest()
    {
        RockLog.Trace("TreasureRoom", "OpenChest starting.");
    }

    [HarmonyPatch(typeof(NTreasureRoom), "OpenChest")]
    [HarmonyPostfix]
    private static void AfterOpenChest()
    {
        RockLog.Trace("TreasureRoom", "OpenChest completed.");
    }

    [HarmonyPatch(typeof(NTreasureRoom), "OpenChest")]
    [HarmonyFinalizer]
    private static Exception? OpenChestFinalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            RockLog.Exception("OpenChest failed", __exception);
        }

        return __exception;
    }
}
