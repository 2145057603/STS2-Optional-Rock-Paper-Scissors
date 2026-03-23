using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Rock.Models;

internal sealed class ManualRpsSession
{
    private readonly Dictionary<ulong, ManualRpsMove> _movesByPlayerId = new();

    public ManualRpsSession(IReadOnlyList<RelicModel> offeredRelics, IReadOnlyList<Player> players)
    {
        OfferedRelics = offeredRelics;
        Players = players.ToList();
        StartedAtUtc = DateTime.UtcNow;
    }

    public IReadOnlyList<RelicModel> OfferedRelics { get; }

    public IReadOnlyList<Player> Players { get; }

    public DateTime StartedAtUtc { get; }

    public void SetMove(ulong playerId, ManualRpsMove move)
    {
        _movesByPlayerId[playerId] = move;
    }

    public bool TryGetMove(ulong playerId, out ManualRpsMove move)
    {
        return _movesByPlayerId.TryGetValue(playerId, out move);
    }

    public void RemoveMove(ulong playerId)
    {
        _movesByPlayerId.Remove(playerId);
    }

    public void ClearMoves(IEnumerable<ulong> playerIds)
    {
        foreach (ulong playerId in playerIds)
        {
            _movesByPlayerId.Remove(playerId);
        }
    }
}
