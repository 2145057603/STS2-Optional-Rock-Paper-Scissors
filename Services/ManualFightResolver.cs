using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Models;
using Rock.Models;

namespace Rock.Services;

internal sealed class ManualFightResolver
{
    public bool TryCreateResult(
        IReadOnlyList<Player> players,
        RelicModel relic,
        Func<Player, ManualRpsMove?> getMove,
        out RelicPickingResult? result)
    {
        result = null;
        if (players.Count <= 1)
        {
            return false;
        }

        RelicPickingFight fight = new();
        fight.playersInvolved.AddRange(players);

        Dictionary<ulong, ManualRpsMove> moveLookup = new(players.Count);
        HashSet<ManualRpsMove> distinctMoves = new();
        RelicPickingFightRound round = new();

        foreach (Player player in players)
        {
            ManualRpsMove? move = getMove(player);
            if (!move.HasValue)
            {
                return false;
            }

            moveLookup[player.NetId] = move.Value;
            distinctMoves.Add(move.Value);
            round.moves.Add((RelicPickingFightMove)(int)move.Value);
        }

        if (distinctMoves.Count != 2)
        {
            return false;
        }

        fight.rounds.Add(round);

        RelicPickingFightMove[] pair = distinctMoves
            .Select(move => (RelicPickingFightMove)(int)move)
            .ToArray();
        RelicPickingFightMove losingMove = GetLosingMove(pair[0], pair[1]);

        List<Player> winners = players
            .Where(player => (RelicPickingFightMove)(int)moveLookup[player.NetId] != losingMove)
            .ToList();

        if (winners.Count != 1)
        {
            return false;
        }

        result = new RelicPickingResult
        {
            type = RelicPickingResultType.FoughtOver,
            player = winners[0],
            relic = relic,
            fight = fight
        };

        return true;
    }

    private static RelicPickingFightMove GetLosingMove(RelicPickingFightMove move1, RelicPickingFightMove move2)
    {
        return (int)(move1 + 1) % 3 == (int)move2 ? move1 : move2;
    }
}
