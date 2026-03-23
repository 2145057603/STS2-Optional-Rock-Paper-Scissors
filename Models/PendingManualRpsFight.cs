using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Models;

namespace Rock.Models;

internal sealed class PendingManualRpsFight
{
    private readonly HashSet<ulong> _activePlayerIds;
    private readonly RelicPickingFight _finalFight = new();

    public PendingManualRpsFight(IReadOnlyList<Player> players, RelicModel relic)
    {
        Players = players.ToList();
        Relic = relic;
        _activePlayerIds = players.Select(player => player.NetId).ToHashSet();
        _finalFight.playersInvolved.AddRange(players);
    }

    public IReadOnlyList<Player> Players { get; }

    public RelicModel Relic { get; }

    public RelicPickingFight Fight => _finalFight;

    public Player? Winner { get; private set; }

    public bool IsResolved => Winner != null;

    public int CompletedRounds { get; private set; }

    public int NextRoundNumber => CompletedRounds + 1;

    public IReadOnlyList<Player> ActivePlayers =>
        Players.Where(player => _activePlayerIds.Contains(player.NetId)).ToList();

    public bool Matches(IReadOnlyList<Player> players, RelicModel relic)
    {
        if (!ReferenceEquals(Relic, relic) || players.Count != Players.Count)
        {
            return false;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].NetId != Players[i].NetId)
            {
                return false;
            }
        }

        return true;
    }

    public bool AreAllActivePlayersReady(Func<ulong, bool> hasMove)
    {
        return _activePlayerIds.All(hasMove);
    }

    public ManualRoundAdvanceResult AdvanceRound(Func<ulong, ManualRpsMove?> getMove)
    {
        RelicPickingFightRound round = new();
        Dictionary<ulong, ManualRpsMove> moveLookup = new();
        HashSet<ManualRpsMove> distinctMoves = new();

        foreach (Player player in Players)
        {
            if (!_activePlayerIds.Contains(player.NetId))
            {
                round.moves.Add(null);
                continue;
            }

            ManualRpsMove? move = getMove(player.NetId);
            if (!move.HasValue)
            {
                return ManualRoundAdvanceResult.WaitingForMoves();
            }

            moveLookup[player.NetId] = move.Value;
            distinctMoves.Add(move.Value);
            round.moves.Add((RelicPickingFightMove)(int)move.Value);
        }

        CompletedRounds++;
        Rock.Infrastructure.RockLog.Trace(
            "Fight",
            $"AdvanceRound #{CompletedRounds} active=[{string.Join(",", _activePlayerIds)}] moves=[{string.Join(",", moveLookup.Select(entry => $"{entry.Key}:{entry.Value}"))}] distinct={distinctMoves.Count}.");

        if (distinctMoves.Count != 2)
        {
            Rock.Infrastructure.RockLog.Trace("Fight", $"Round #{CompletedRounds} tied.");
            return ManualRoundAdvanceResult.Tied(round, ActivePlayers, Array.Empty<Player>());
        }

        RelicPickingFightMove[] pair = distinctMoves
            .Select(move => (RelicPickingFightMove)(int)move)
            .ToArray();
        RelicPickingFightMove losingMove = GetLosingMove(pair[0], pair[1]);

        List<Player> losers = new();
        foreach (Player player in Players)
        {
            if (_activePlayerIds.Contains(player.NetId) &&
                (RelicPickingFightMove)(int)moveLookup[player.NetId] == losingMove)
            {
                losers.Add(player);
                _activePlayerIds.Remove(player.NetId);
            }
        }

        List<Player> survivors = ActivePlayers.ToList();
        Rock.Infrastructure.RockLog.Trace(
            "Fight",
            $"Round #{CompletedRounds} losers=[{string.Join(",", losers.Select(player => player.NetId))}] survivors=[{string.Join(",", survivors.Select(player => player.NetId))}].");
        if (survivors.Count == 1)
        {
            Winner = survivors[0];
            _finalFight.rounds.Clear();
            _finalFight.rounds.Add(round);
            Rock.Infrastructure.RockLog.Trace("Fight", $"Fight resolved winner={Winner.NetId} storedRounds={_finalFight.rounds.Count}.");
            return ManualRoundAdvanceResult.Resolved(round, Winner, losers);
        }

        return ManualRoundAdvanceResult.Continue(round, survivors, losers);
    }

    private static RelicPickingFightMove GetLosingMove(RelicPickingFightMove move1, RelicPickingFightMove move2)
    {
        return (int)(move1 + 1) % 3 == (int)move2 ? move1 : move2;
    }
}

internal readonly record struct ManualRoundAdvanceResult(
    bool DidAdvance,
    bool IsTie,
    bool IsResolved,
    RelicPickingFightRound? Round,
    Player? Winner,
    IReadOnlyList<Player> NextPlayers,
    IReadOnlyList<Player> LosingPlayers)
{
    public static ManualRoundAdvanceResult WaitingForMoves() =>
        new(false, false, false, null, null, Array.Empty<Player>(), Array.Empty<Player>());

    public static ManualRoundAdvanceResult Tied(
        RelicPickingFightRound round,
        IReadOnlyList<Player> players,
        IReadOnlyList<Player> losers) =>
        new(true, true, false, round, null, players, losers);

    public static ManualRoundAdvanceResult Continue(
        RelicPickingFightRound round,
        IReadOnlyList<Player> players,
        IReadOnlyList<Player> losers) =>
        new(true, false, false, round, null, players, losers);

    public static ManualRoundAdvanceResult Resolved(
        RelicPickingFightRound round,
        Player winner,
        IReadOnlyList<Player> losers) =>
        new(true, false, true, round, winner, new[] { winner }, losers);
}
