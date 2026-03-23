using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using Rock.HandMarkers;
using Rock.Infrastructure;
using Rock.RoundHistory;
using Rock.Services;
using Rock.Ui;

namespace Rock.Patches;

[HarmonyPatch(typeof(NTreasureRoomRelicCollection))]
internal static class TreasureRoomUiOverlayPatch
{
    [HarmonyPatch(nameof(NTreasureRoomRelicCollection.AnimIn))]
    [HarmonyPostfix]
    private static void AfterAnimIn(NTreasureRoomRelicCollection __instance)
    {
        TreasureRoomRelicUiAccessor.Attach(__instance);

        if (__instance.GetNodeOrNull<PlayerHandMarkerLayer>("RockPlayerHandMarkerLayer") == null)
        {
            PlayerHandMarkerLayer markerLayer = new()
            {
                Name = "RockPlayerHandMarkerLayer"
            };
            __instance.AddChild(markerLayer);
            markerLayer.RefreshNow();
            RockLog.Info("Attached player hand marker layer to treasure relic collection.");
        }

        ManualRpsOverlayView? overlay = __instance.GetNodeOrNull<ManualRpsOverlayView>("RockManualRpsOverlay");
        if (overlay == null)
        {
            overlay = new ManualRpsOverlayView
            {
                Name = "RockManualRpsOverlay"
            };
            __instance.AddChild(overlay);
            overlay.RefreshNow();
            RockLog.Info("Attached non-modal manual RPS overlay to treasure relic collection.");
        }

        ManualRpsHistoryView? history = __instance.GetNodeOrNull<ManualRpsHistoryView>("RockManualRpsHistory");
        if (history == null)
        {
            history = new ManualRpsHistoryView
            {
                Name = "RockManualRpsHistory"
            };
            __instance.AddChild(history);
            history.RefreshNow();
            RockLog.Info("Attached manual RPS round history to treasure relic collection.");
        }
    }

    [HarmonyPatch(nameof(NTreasureRoomRelicCollection.AnimOut))]
    [HarmonyPostfix]
    private static void AfterAnimOut(NTreasureRoomRelicCollection __instance)
    {
        if (Rock.Runtime.RockRuntime.Coordinator.HasActiveSession ||
            Rock.Runtime.RockRuntime.Coordinator.HasPendingAward)
        {
            RockLog.Info("Treasure relic UI closed while a shared relic session was still active; forcing session cleanup.");
            Rock.Runtime.RockRuntime.Coordinator.EndSession();
        }

        TreasureRoomRelicUiAccessor.Detach(__instance);

        PlayerHandMarkerLayer? markerLayer = __instance.GetNodeOrNull<PlayerHandMarkerLayer>("RockPlayerHandMarkerLayer");
        markerLayer?.QueueFree();
        ManualRpsOverlayView? overlay = __instance.GetNodeOrNull<ManualRpsOverlayView>("RockManualRpsOverlay");
        overlay?.QueueFree();
        ManualRpsHistoryView? history = __instance.GetNodeOrNull<ManualRpsHistoryView>("RockManualRpsHistory");
        history?.QueueFree();
    }
}
