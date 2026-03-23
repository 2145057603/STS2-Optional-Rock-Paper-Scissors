using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Models;
using Rock.Runtime;

namespace Rock.Patches;

[HarmonyPatch(typeof(RelicPickingResult))]
internal static class RelicPickingResultPatch
{
    [HarmonyPatch(nameof(RelicPickingResult.GenerateRelicFight))]
    [HarmonyPrefix]
    private static bool TryOverrideFight(
        List<Player> players,
        RelicModel relic,
        ref RelicPickingResult __result)
    {
        if (!RockRuntime.Coordinator.TryResolveFight(players, relic, out RelicPickingResult? overrideResult) ||
            overrideResult == null)
        {
            Rock.Infrastructure.RockLog.Trace(
                "Result",
                $"GenerateRelicFight using original logic players=[{string.Join(",", players.Select(player => player.NetId))}] relic={relic}.");
            return true;
        }

        Rock.Infrastructure.RockLog.Trace(
            "Result",
            $"GenerateRelicFight overridden players=[{string.Join(",", players.Select(player => player.NetId))}] relic={relic} winner={overrideResult.player.NetId} rounds={overrideResult.fight?.rounds?.Count ?? -1}.");
        __result = overrideResult;
        return false;
    }
}
