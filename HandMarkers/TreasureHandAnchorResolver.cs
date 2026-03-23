using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using Rock.Infrastructure;
using Rock.Services;

namespace Rock.HandMarkers;

internal static class TreasureHandAnchorResolver
{
    private static readonly Dictionary<ulong, bool> LoggedCollections = new();

    public static bool TryResolveAnchor(
        NTreasureRoomRelicCollection collection,
        IReadOnlyList<Player> players,
        Player player,
        out Vector2 anchorPosition)
    {
        anchorPosition = default;
        LogCandidateSummaryOnce(collection);

        if (TryResolvePreFightAnchor(collection, players, player, out anchorPosition))
        {
            return true;
        }

        return TryResolveFightAnchor(collection, player, out anchorPosition);
    }

    private static bool TryResolvePreFightAnchor(
        NTreasureRoomRelicCollection collection,
        IReadOnlyList<Player> players,
        Player player,
        out Vector2 anchorPosition)
    {
        anchorPosition = default;
        IReadOnlyList<CanvasItem> candidates = GetPreFightCandidates(collection);
        if (candidates.Count == 0)
        {
            return false;
        }

        int playerIndex = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].NetId == player.NetId)
            {
                playerIndex = i;
                break;
            }
        }

        if (playerIndex < 0)
        {
            return false;
        }

        int candidateIndex = Math.Min(playerIndex, candidates.Count - 1);
        Transform2D inverse = collection.GetGlobalTransformWithCanvas().AffineInverse();
        anchorPosition = inverse * candidates[candidateIndex].GetGlobalTransformWithCanvas().Origin;
        return true;
    }

    private static bool TryResolveFightAnchor(
        NTreasureRoomRelicCollection collection,
        Player player,
        out Vector2 anchorPosition)
    {
        anchorPosition = default;

        try
        {
            if (TreasureRoomRelicUiAccessor.GetHand(collection, player) is not CanvasItem handItem)
            {
                return false;
            }

            Transform2D collectionInverse = collection.GetGlobalTransformWithCanvas().AffineInverse();
            anchorPosition = collectionInverse * handItem.GetGlobalTransformWithCanvas().Origin;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<CanvasItem> GetPreFightCandidates(NTreasureRoomRelicCollection collection)
    {
        List<CanvasItem> rawCandidates = new();
        foreach (Node descendant in EnumerateDescendants(collection))
        {
            if (descendant is not CanvasItem item ||
                !item.Visible ||
                descendant is PlayerHandMarkerLayer ||
                descendant is PlayerHandMarkerBadge)
            {
                continue;
            }

            string haystack = $"{descendant.Name} {descendant.GetType().Name}".ToLowerInvariant();
            if (!haystack.Contains("hand") &&
                !haystack.Contains("finger") &&
                !haystack.Contains("pointer") &&
                !haystack.Contains("cursor"))
            {
                continue;
            }

            rawCandidates.Add(item);
        }

        return rawCandidates
            .OrderBy(item => item.GetGlobalTransformWithCanvas().Origin.X)
            .ThenBy(item => item.GetGlobalTransformWithCanvas().Origin.Y)
            .GroupBy(item => Quantize(item.GetGlobalTransformWithCanvas().Origin))
            .Select(group => group.Last())
            .ToList();
    }

    private static string Quantize(Vector2 position)
    {
        return $"{MathF.Round(position.X / 20f)}:{MathF.Round(position.Y / 20f)}";
    }

    private static IEnumerable<Node> EnumerateDescendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;

            foreach (Node grandChild in EnumerateDescendants(child))
            {
                yield return grandChild;
            }
        }
    }

    private static void LogCandidateSummaryOnce(NTreasureRoomRelicCollection collection)
    {
        ulong key = collection.GetInstanceId();
        if (LoggedCollections.ContainsKey(key))
        {
            return;
        }

        LoggedCollections[key] = true;
        List<string> lines = new();
        foreach (CanvasItem candidate in GetPreFightCandidates(collection))
        {
            Vector2 origin = candidate.GetGlobalTransformWithCanvas().Origin;
            lines.Add($"{candidate.Name}:{candidate.GetType().Name}@({origin.X:0},{origin.Y:0})");
        }

        RockLog.Trace(
            "HandMarkers",
            lines.Count == 0
                ? "No pre-fight hand candidates found in treasure relic collection."
                : $"Pre-fight hand candidates: {string.Join(", ", lines)}");
    }
}
