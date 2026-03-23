using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using Rock.Infrastructure;
using Rock.Ui;

namespace Rock.Patches;

[HarmonyPatch(typeof(NRun))]
internal static class NRunOverlayPatch
{
    [HarmonyPatch(nameof(NRun._Ready))]
    [HarmonyPostfix]
    private static void AfterReady(NRun __instance)
    {
        RockLog.Info("Observed NRun ready.");
    }
}
