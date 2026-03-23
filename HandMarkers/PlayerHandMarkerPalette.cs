using Godot;

namespace Rock.HandMarkers;

internal static class PlayerHandMarkerPalette
{
    private static readonly Color[] Colors =
    {
        new(0.98f, 0.47f, 0.37f, 1f),
        new(0.29f, 0.76f, 0.98f, 1f),
        new(0.98f, 0.82f, 0.29f, 1f),
        new(0.55f, 0.86f, 0.41f, 1f),
        new(0.82f, 0.49f, 0.97f, 1f),
        new(0.98f, 0.61f, 0.79f, 1f)
    };

    public static Color GetColor(int playerIndex)
    {
        if (playerIndex < 0)
        {
            playerIndex = 0;
        }

        return Colors[playerIndex % Colors.Length];
    }
}
