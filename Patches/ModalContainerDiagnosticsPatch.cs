using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Rock.Infrastructure;

namespace Rock.Patches;

[HarmonyPatch(typeof(NModalContainer))]
internal static class ModalContainerDiagnosticsPatch
{
    [HarmonyPatch(nameof(NModalContainer.Add))]
    [HarmonyPrefix]
    private static void BeforeAdd(Node modalToCreate, bool showBackstop)
    {
        RockLog.Trace(
            "ModalContainer",
            $"Add prefix modal={modalToCreate.Name}:{modalToCreate.GetType().Name} showBackstop={showBackstop} instanceReady={NModalContainer.Instance != null}.");
    }

    [HarmonyPatch(nameof(NModalContainer.Add))]
    [HarmonyPostfix]
    private static void AfterAdd(NModalContainer __instance)
    {
        RockLog.Trace(
            "ModalContainer",
            $"Add postfix openModal={Describe(__instance.OpenModal)} mouseFilter={__instance.MouseFilter} childCount={__instance.GetChildCount()}.");
    }

    [HarmonyPatch(nameof(NModalContainer.Clear))]
    [HarmonyPrefix]
    private static void BeforeClear(NModalContainer __instance)
    {
        RockLog.Trace(
            "ModalContainer",
            $"Clear prefix openModal={Describe(__instance.OpenModal)} childCount={__instance.GetChildCount()}.");
    }

    [HarmonyPatch(nameof(NModalContainer.Clear))]
    [HarmonyPostfix]
    private static void AfterClear(NModalContainer __instance)
    {
        RockLog.Trace(
            "ModalContainer",
            $"Clear postfix openModal={Describe(__instance.OpenModal)} mouseFilter={__instance.MouseFilter} childCount={__instance.GetChildCount()}.");
    }

    [HarmonyPatch(nameof(NModalContainer.ShowBackstop))]
    [HarmonyPostfix]
    private static void AfterShowBackstop(NModalContainer __instance)
    {
        RockLog.Trace(
            "ModalContainer",
            $"ShowBackstop postfix openModal={Describe(__instance.OpenModal)} mouseFilter={__instance.MouseFilter}.");
    }

    [HarmonyPatch(nameof(NModalContainer.HideBackstop))]
    [HarmonyPostfix]
    private static void AfterHideBackstop(NModalContainer __instance)
    {
        RockLog.Trace(
            "ModalContainer",
            $"HideBackstop postfix openModal={Describe(__instance.OpenModal)} mouseFilter={__instance.MouseFilter}.");
    }

    private static string Describe(object? modal)
    {
        return modal switch
        {
            null => "<none>",
            Node node => $"{node.Name}:{node.GetType().Name}",
            _ => modal.GetType().Name
        };
    }
}
