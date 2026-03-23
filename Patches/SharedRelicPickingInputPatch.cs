using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using Rock.Input;
using Rock.Runtime;
using Rock.Services;

namespace Rock.Patches;

[HarmonyPatch(typeof(NGame))]
internal static class SharedRelicPickingInputPatch
{
    [HarmonyPatch("_Input")]
    [HarmonyPostfix]
    private static void AfterInput(InputEvent inputEvent)
    {
        if (ManualMoveHotkeyMapper.TryMap(inputEvent, out Models.ManualRpsMove move))
        {
            if (!RockRuntime.Coordinator.HasActiveSession)
            {
                return;
            }

            if (!IsManualRpsInputContextValid())
            {
                Rock.Infrastructure.RockLog.Warn("Ignored manual RPS hotkey because the shared relic UI is no longer active; cleaning up stale session.");
                RockRuntime.Coordinator.EndSession();
                return;
            }

            Rock.Infrastructure.RockLog.Trace("Input", $"NGame._Input mapped {inputEvent.AsText()} -> {move}.");
            RockRuntime.Coordinator.SetLocalDraftMove(move);
            return;
        }

        if (!RockRuntime.Coordinator.IsAwaitingManualResolution ||
            inputEvent is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        Key key = keyEvent.Keycode != Key.None ? keyEvent.Keycode :
            keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode :
            keyEvent.KeyLabel;
        if (key is Key.Enter or Key.KpEnter)
        {
            Rock.Infrastructure.RockLog.Trace("Input", $"NGame._Input mapped {inputEvent.AsText()} -> ToggleConfirm.");
            RockRuntime.Coordinator.ToggleLocalDraftConfirmation();
        }
    }

    private static bool IsManualRpsInputContextValid()
    {
        return TreasureRoomRelicUiAccessor.CurrentCollection is { } collection &&
               collection.IsInsideTree();
    }
}
