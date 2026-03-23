using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using Rock.HandMarkers;
using Rock.Models;
using Rock.RoundHistory;
using Rock.Ui;
using Rock.Infrastructure;

namespace Rock.Services;

internal static class TreasureRoomRelicUiAccessor
{
    private static readonly AccessTools.FieldRef<NTreasureRoomRelicCollection, Control> FightBackstopRef =
        AccessTools.FieldRefAccess<NTreasureRoomRelicCollection, Control>("_fightBackstop");

    private static readonly AccessTools.FieldRef<NTreasureRoomRelicCollection, NHandImageCollection> HandsRef =
        AccessTools.FieldRefAccess<NTreasureRoomRelicCollection, NHandImageCollection>("_hands");

    private static readonly AccessTools.FieldRef<NTreasureRoomRelicCollection, List<NTreasureRoomRelicHolder>> HoldersInUseRef =
        AccessTools.FieldRefAccess<NTreasureRoomRelicCollection, List<NTreasureRoomRelicHolder>>("_holdersInUse");

    private static readonly AccessTools.FieldRef<NHandImageCollection, List<NHandImage>> HandItemsRef =
        AccessTools.FieldRefAccess<NHandImageCollection, List<NHandImage>>("_hands");

    private static readonly AccessTools.FieldRef<NHandImage, Marker2D> GrabMarkerRef =
        AccessTools.FieldRefAccess<NHandImage, Marker2D>("_grabMarker");

    private static readonly AccessTools.FieldRef<NHandImage, TextureRect> TextureRectRef =
        AccessTools.FieldRefAccess<NHandImage, TextureRect>("_textureRect");

    public static NTreasureRoomRelicCollection? CurrentCollection { get; private set; }

    public static void Attach(NTreasureRoomRelicCollection collection)
    {
        CurrentCollection = collection;
        Rock.Infrastructure.RockLog.Trace("TreasureUi", "Attached treasure relic collection.");
    }

    public static void Detach(NTreasureRoomRelicCollection collection)
    {
        if (ReferenceEquals(CurrentCollection, collection))
        {
            CurrentCollection = null;
            Rock.Infrastructure.RockLog.Trace("TreasureUi", "Detached treasure relic collection.");
        }
    }

    public static Control GetFightBackstop(NTreasureRoomRelicCollection collection) => FightBackstopRef(collection);

    public static NHandImageCollection GetHands(NTreasureRoomRelicCollection collection) => HandsRef(collection);

    public static NTreasureRoomRelicHolder GetHolderForRelic(NTreasureRoomRelicCollection collection, RelicModel relic)
    {
        return HoldersInUseRef(collection).First(holder => holder.Relic.Model == relic);
    }

    public static NHandImage GetHand(NTreasureRoomRelicCollection collection, Player player)
    {
        return GetHands(collection).GetHand(player.NetId)!;
    }

    public static bool TryGetHand(NTreasureRoomRelicCollection collection, Player player, out NHandImage hand)
    {
        hand = GetHands(collection).GetHand(player.NetId)!;
        return hand != null;
    }

    public static IReadOnlyList<NHandImage> GetAllHands(NTreasureRoomRelicCollection collection)
    {
        return HandItemsRef(GetHands(collection));
    }

    public static IReadOnlyList<Player> GetPlayersForHandMarkers(NTreasureRoomRelicCollection collection)
    {
        return GetAllHands(collection)
            .OrderBy(hand => hand.Index)
            .Select(hand => hand.Player)
            .ToList();
    }

    public static bool TryGetHandAnchorPosition(
        NTreasureRoomRelicCollection collection,
        Player player,
        out Vector2 anchorPosition)
    {
        anchorPosition = default;

        try
        {
            NHandImage? hand = GetHands(collection).GetHand(player.NetId);
            if (hand == null || !hand.Visible)
            {
                return false;
            }

            Marker2D grabMarker = GrabMarkerRef(hand);
            Transform2D inverse = collection.GetGlobalTransformWithCanvas().AffineInverse();
            anchorPosition = inverse * grabMarker.GetGlobalTransformWithCanvas().Origin;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void ResetPlayerHandMarkerStyles(NTreasureRoomRelicCollection collection)
    {
        foreach (NHandImage hand in GetAllHands(collection))
        {
            TextureRect textureRect = TextureRectRef(hand);
            textureRect.SelfModulate = Colors.White;
            PlayerHandNameplate? nameplate = GetExistingNameplate(hand);
            if (nameplate != null)
            {
                nameplate.Visible = false;
            }
        }
    }

    public static void ApplyPlayerHandMarkerStyle(NHandImage hand, Color accent, string markerText)
    {
        TextureRect textureRect = TextureRectRef(hand);
        textureRect.SelfModulate = Colors.White.Lerp(accent, 0.62f);

        PlayerHandNameplate nameplate = GetOrCreateNameplate(hand);
        nameplate.UpdateState(markerText, accent);
        nameplate.Position = GetNameplateLocalPosition(hand, nameplate);
        nameplate.Rotation = -textureRect.Rotation;
        nameplate.Visible = hand.Visible;

        LogHandMarkerState(hand, textureRect, nameplate, markerText);
    }

    public static void ResetManualFightUi()
    {
        NTreasureRoomRelicCollection? collection = CurrentCollection;
        if (collection == null)
        {
            Rock.Infrastructure.RockLog.Trace("TreasureUi", "ResetManualFightUi skipped because no current collection.");
            return;
        }

        Control backstop = GetFightBackstop(collection);
        Color modulate = backstop.Modulate;
        modulate.A = 0f;
        backstop.Modulate = modulate;
        backstop.Visible = false;

        NHandImageCollection hands = GetHands(collection);
        foreach (NHandImage hand in HandItemsRef(hands))
        {
            hand.SetIsInFight(inFight: false);
            hand.SetFrozenForRelicAwards(frozenForRelicAwards: false);
            hand.AnimateAway();
            TextureRect textureRect = TextureRectRef(hand);
            textureRect.SelfModulate = Colors.White;
            PlayerHandNameplate? nameplate = GetExistingNameplate(hand);
            if (nameplate != null)
            {
                nameplate.Visible = false;
            }
        }

        Rock.Infrastructure.RockLog.Trace("TreasureUi", $"ResetManualFightUi completed hands={HandItemsRef(hands).Count}.");
    }

    public static void RefreshManualOverlay()
    {
        ManualRpsOverlayView? overlay = CurrentCollection?.GetNodeOrNull<ManualRpsOverlayView>("RockManualRpsOverlay");
        overlay?.RefreshNow();
    }

    public static void RefreshPlayerHandMarkers()
    {
        PlayerHandMarkerLayer? markerLayer = CurrentCollection?.GetNodeOrNull<PlayerHandMarkerLayer>("RockPlayerHandMarkerLayer");
        markerLayer?.RefreshNow();
    }

    public static void RefreshRoundHistory()
    {
        ManualRpsHistoryView? history = CurrentCollection?.GetNodeOrNull<ManualRpsHistoryView>("RockManualRpsHistory");
        history?.RefreshNow();
    }

    public static RelicPickingFightRound CreateRound(IReadOnlyList<Player> allPlayers, IReadOnlyDictionary<ulong, ManualRpsMove> movesByPlayer)
    {
        RelicPickingFightRound round = new();
        foreach (Player player in allPlayers)
        {
            round.moves.Add(
                movesByPlayer.TryGetValue(player.NetId, out ManualRpsMove move)
                    ? (RelicPickingFightMove)(int)move
                    : null);
        }

        return round;
    }

    private static PlayerHandNameplate GetOrCreateNameplate(NHandImage hand)
    {
        TextureRect textureRect = TextureRectRef(hand);
        PlayerHandNameplate? existing = textureRect.GetNodeOrNull<PlayerHandNameplate>("RockPlayerHandNameplate");
        if (existing != null)
        {
            return existing;
        }

        PlayerHandNameplate created = new();
        textureRect.AddChild(created);
        return created;
    }

    private static PlayerHandNameplate? GetExistingNameplate(NHandImage hand)
    {
        return TextureRectRef(hand).GetNodeOrNull<PlayerHandNameplate>("RockPlayerHandNameplate");
    }

    private static Vector2 GetNameplateLocalPosition(NHandImage hand, PlayerHandNameplate nameplate)
    {
        TextureRect textureRect = TextureRectRef(hand);
        
        // Use a stable forearm-local anchor so the plate stays attached to the visible arm panel.
        Vector2 anchor = new Vector2(
            textureRect.Size.X * 0.39f,
            textureRect.Size.Y * 0.44f);

        float halfWidth = nameplate.Size.X * 0.5f;
        float halfHeight = nameplate.Size.Y * 0.5f;
        anchor.X = Mathf.Clamp(anchor.X, halfWidth + 12f, textureRect.Size.X - halfWidth - 12f);
        anchor.Y = Mathf.Clamp(anchor.Y, halfHeight + 10f, textureRect.Size.Y - halfHeight - 10f);
        return anchor - nameplate.Size * 0.5f;
    }

    private static readonly Dictionary<string, ulong> LastHandMarkerLogs = new();

    private static void LogHandMarkerState(
        NHandImage hand,
        TextureRect textureRect,
        PlayerHandNameplate nameplate,
        string markerText)
    {
        Marker2D grabMarker = GrabMarkerRef(hand);
        Vector2 handGlobal = hand.GetGlobalTransformWithCanvas().Origin;
        Vector2 textureGlobal = textureRect.GetGlobalTransformWithCanvas().Origin;
        Vector2 grabGlobal = grabMarker.GetGlobalTransformWithCanvas().Origin;
        Vector2 plateGlobal = nameplate.GetGlobalTransformWithCanvas().Origin;
        string key = $"{hand.Player.NetId}:{hand.Index}";
        ulong fingerprint = ComputeFingerprint(hand, textureRect, nameplate, grabGlobal, plateGlobal);

        if (LastHandMarkerLogs.TryGetValue(key, out ulong previous) && previous == fingerprint)
        {
            return;
        }

        LastHandMarkerLogs[key] = fingerprint;
        RockLog.Trace(
            "HandMarkers",
            $"player={hand.Player.NetId} idx={hand.Index} handVisible={hand.Visible} handPos=({handGlobal.X:0.0},{handGlobal.Y:0.0}) " +
            $"texturePos=({textureGlobal.X:0.0},{textureGlobal.Y:0.0}) textureSize=({textureRect.Size.X:0.0},{textureRect.Size.Y:0.0}) textureRot={textureRect.Rotation:0.000} " +
            $"grab=({grabGlobal.X:0.0},{grabGlobal.Y:0.0}) plateVisible={nameplate.Visible} plateParent={nameplate.GetParent()?.GetType().Name}:{nameplate.GetParent()?.Name} " +
            $"plateLocal=({nameplate.Position.X:0.0},{nameplate.Position.Y:0.0}) plateGlobal=({plateGlobal.X:0.0},{plateGlobal.Y:0.0}) plateSize=({nameplate.Size.X:0.0},{nameplate.Size.Y:0.0}) text={markerText}.");
    }

    private static ulong ComputeFingerprint(
        NHandImage hand,
        TextureRect textureRect,
        PlayerHandNameplate nameplate,
        Vector2 grabGlobal,
        Vector2 plateGlobal)
    {
        HashCode hash = new();
        hash.Add(hand.Visible);
        hash.Add(MathF.Round(hand.GetGlobalTransformWithCanvas().Origin.X));
        hash.Add(MathF.Round(hand.GetGlobalTransformWithCanvas().Origin.Y));
        hash.Add(MathF.Round(textureRect.Rotation * 1000f));
        hash.Add(MathF.Round(grabGlobal.X));
        hash.Add(MathF.Round(grabGlobal.Y));
        hash.Add(nameplate.Visible);
        hash.Add(MathF.Round(nameplate.Position.X));
        hash.Add(MathF.Round(nameplate.Position.Y));
        hash.Add(MathF.Round(plateGlobal.X));
        hash.Add(MathF.Round(plateGlobal.Y));
        return (ulong)hash.ToHashCode();
    }

}
