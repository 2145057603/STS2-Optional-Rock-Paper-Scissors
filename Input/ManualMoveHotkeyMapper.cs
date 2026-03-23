using Godot;
using Rock.Models;

namespace Rock.Input;

internal static class ManualMoveHotkeyMapper
{
    public static bool TryMap(InputEvent inputEvent, out ManualRpsMove move)
    {
        move = default;

        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return false;
        }

        if (TryMapKey(keyEvent.Keycode, out move))
        {
            return true;
        }

        if (TryMapKey(keyEvent.PhysicalKeycode, out move))
        {
            return true;
        }

        return TryMapKey(keyEvent.KeyLabel, out move);
    }

    private static bool TryMapKey(Key key, out ManualRpsMove move)
    {
        move = key switch
        {
            Key.Key1 => ManualRpsMove.Rock,
            Key.Kp1 => ManualRpsMove.Rock,
            Key.Key2 => ManualRpsMove.Paper,
            Key.Kp2 => ManualRpsMove.Paper,
            Key.Key3 => ManualRpsMove.Scissors,
            Key.Kp3 => ManualRpsMove.Scissors,
            _ => default
        };

        return key is Key.Key1 or Key.Kp1 or Key.Key2 or Key.Kp2 or Key.Key3 or Key.Kp3;
    }
}
