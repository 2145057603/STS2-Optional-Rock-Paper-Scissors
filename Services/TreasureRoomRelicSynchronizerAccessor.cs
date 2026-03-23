using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace Rock.Services;

internal static class TreasureRoomRelicSynchronizerAccessor
{
    private static readonly AccessTools.FieldRef<TreasureRoomRelicSynchronizer, List<RelicModel>?> CurrentRelicsRef =
        AccessTools.FieldRefAccess<TreasureRoomRelicSynchronizer, List<RelicModel>?>("_currentRelics");

    private static readonly AccessTools.FieldRef<TreasureRoomRelicSynchronizer, List<int?>> VotesRef =
        AccessTools.FieldRefAccess<TreasureRoomRelicSynchronizer, List<int?>>("_votes");

    private static readonly AccessTools.FieldRef<TreasureRoomRelicSynchronizer, IPlayerCollection> PlayerCollectionRef =
        AccessTools.FieldRefAccess<TreasureRoomRelicSynchronizer, IPlayerCollection>("_playerCollection");

    private static readonly Action<TreasureRoomRelicSynchronizer> AwardRelicsInvoker =
        AccessTools.MethodDelegate<Action<TreasureRoomRelicSynchronizer>>(
            AccessTools.Method(typeof(TreasureRoomRelicSynchronizer), "AwardRelics"));

    private static readonly Action<TreasureRoomRelicSynchronizer> EndRelicVotingInvoker =
        AccessTools.MethodDelegate<Action<TreasureRoomRelicSynchronizer>>(
            AccessTools.Method(typeof(TreasureRoomRelicSynchronizer), "EndRelicVoting"));

    public static IReadOnlyList<RelicModel>? GetCurrentRelics(TreasureRoomRelicSynchronizer synchronizer)
    {
        return CurrentRelicsRef(synchronizer);
    }

    public static IReadOnlyList<int?> GetVotes(TreasureRoomRelicSynchronizer synchronizer)
    {
        return VotesRef(synchronizer);
    }

    public static IReadOnlyList<Player> GetPlayers(TreasureRoomRelicSynchronizer synchronizer)
    {
        return PlayerCollectionRef(synchronizer).Players;
    }

    public static void InvokeAwardRelics(TreasureRoomRelicSynchronizer synchronizer)
    {
        AwardRelicsInvoker(synchronizer);
    }

    public static void InvokeEndRelicVoting(TreasureRoomRelicSynchronizer synchronizer)
    {
        EndRelicVotingInvoker(synchronizer);
    }
}
