using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Rock.Models;

internal sealed class PendingRelicFight
{
    public PendingRelicFight(IReadOnlyList<Player> players, RelicModel relic)
    {
        Players = players;
        Relic = relic;
    }

    public IReadOnlyList<Player> Players { get; }

    public RelicModel Relic { get; }
}
