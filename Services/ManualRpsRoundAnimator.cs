using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using Rock.Infrastructure;
using Rock.Models;
using Rock.Runtime;

namespace Rock.Services;

internal sealed class ManualRpsRoundAnimator
{
    public async Task PlayIntermediateRoundAsync(
        PendingManualRpsFight fight,
        RelicPickingFightRound round,
        IReadOnlyList<Player> losers)
    {
        RockLog.Trace(
            "RoundAnimator",
            $"PlayIntermediateRoundAsync starting players=[{string.Join(",", fight.Players.Select(player => player.NetId))}] losers=[{string.Join(",", losers.Select(player => player.NetId))}].");
        NTreasureRoomRelicCollection? collection = TreasureRoomRelicUiAccessor.CurrentCollection;
        if (collection == null)
        {
            RockLog.Warn("Could not animate manual RPS round because treasure relic UI is unavailable.");
            return;
        }

        NTreasureRoomRelicHolder holder = TreasureRoomRelicUiAccessor.GetHolderForRelic(collection, fight.Relic);
        NHandImageCollection hands = TreasureRoomRelicUiAccessor.GetHands(collection);
        Control backstop = TreasureRoomRelicUiAccessor.GetFightBackstop(collection);

        holder.ZIndex = 1;
        backstop.Visible = true;
        Tween tween = collection.CreateTween();
        tween.TweenProperty(holder, "global_position", (backstop.Size - holder.Size) * 0.5f, 0.25)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(backstop, "modulate:a", 1f, 0.25);
        hands.BeforeFightStarted(fight.Players.ToList());
        await collection.ToSignal(tween, Tween.SignalName.Finished);
        await Cmd.Wait(0.4f);

        List<Tween> moveTweens = new();
        for (int i = 0; i < fight.Players.Count; i++)
        {
            RelicPickingFightMove? move = round.moves[i];
            if (!move.HasValue)
            {
                continue;
            }

            NHandImage hand = TreasureRoomRelicUiAccessor.GetHand(collection, fight.Players[i]);
            moveTweens.Add(hand.DoFightMove(move.Value, 1f));
        }

        if (moveTweens.Count > 0)
        {
            await Task.WhenAll(moveTweens.Select(t => collection.ToSignal(t, Tween.SignalName.Finished).ToTask()));
        }

        if (losers.Count > 0)
        {
            await Task.WhenAll(losers.Select(player => TreasureRoomRelicUiAccessor.GetHand(collection, player).DoLoseShake(0.7f)));
        }
        else
        {
            await Cmd.Wait(0.6f);
        }

        foreach (Player player in fight.Players)
        {
            TreasureRoomRelicUiAccessor.GetHand(collection, player).SetIsInFight(inFight: false);
        }

        tween = collection.CreateTween();
        tween.TweenProperty(backstop, "modulate:a", 0f, 0.25);
        await collection.ToSignal(tween, Tween.SignalName.Finished);
        backstop.Visible = false;
        holder.ZIndex = 0;
        RockRuntime.Coordinator.RevealRoundHistoryAfterAnimation();
        RockLog.Trace("RoundAnimator", "PlayIntermediateRoundAsync completed.");
    }
}
