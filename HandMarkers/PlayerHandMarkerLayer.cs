using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using Rock.Infrastructure;
using Rock.Runtime;
using Rock.Services;

namespace Rock.HandMarkers;

internal sealed partial class PlayerHandMarkerLayer : Control
{
    public PlayerHandMarkerLayer()
    {
        LayoutMode = 1;
        AnchorsPreset = (int)LayoutPreset.FullRect;
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 20;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        Refresh();
    }

    public void RefreshNow()
    {
        Refresh();
    }

    private void Refresh()
    {
        NTreasureRoomRelicCollection? collection = GetParent() as NTreasureRoomRelicCollection;
        if (collection == null)
        {
            Visible = false;
            return;
        }

        TreasureRoomRelicUiAccessor.ResetPlayerHandMarkerStyles(collection);
        if (!RockRuntime.Coordinator.HasActiveSession)
        {
            Visible = false;
            return;
        }

        IReadOnlyList<Player> players = TreasureRoomRelicUiAccessor.GetPlayersForHandMarkers(collection);
        if (players.Count == 0)
        {
            Visible = false;
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            if (TreasureRoomRelicUiAccessor.TryGetHand(collection, player, out NHandImage hand))
            {
                TreasureRoomRelicUiAccessor.ApplyPlayerHandMarkerStyle(
                    hand,
                    PlayerHandMarkerPalette.GetColor(i),
                    PlayerHandMarkerNameResolver.ResolveMarkerText(player, i));
            }
            else
            {
                RockLog.Trace("HandMarkers",
                    $"Refresh missing hand for player={player.NetId} index={i} totalPlayers={players.Count}.");
            }
        }

        Visible = false;
    }
}
