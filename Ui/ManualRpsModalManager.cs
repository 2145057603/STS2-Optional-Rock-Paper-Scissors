using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using Rock.Infrastructure;
using Rock.Services;

namespace Rock.Ui;

internal static class ManualRpsModalManager
{
    public static void ShowOrRefresh()
    {
        CleanupLegacyModal();
        RockLog.Trace("Modal", "ShowOrRefresh routed to non-modal treasure overlay.");
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
    }

    public static void Hide()
    {
        CleanupLegacyModal();
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
        TreasureRoomRelicUiAccessor.ResetManualFightUi();
    }

    public static void ShowFullscreenHint(string text)
    {
        if (NGame.Instance == null)
        {
            return;
        }

        NFullscreenTextVfx? vfx = NFullscreenTextVfx.Create(text);
        if (vfx != null)
        {
            NGame.Instance.AddChild(vfx);
        }
    }

    private static void CleanupLegacyModal()
    {
        NModalContainer? modalContainer = NModalContainer.Instance;
        if (modalContainer == null)
        {
            return;
        }

        if (modalContainer.OpenModal is ManualRpsChoiceModal modal)
        {
            RockLog.Trace("Modal", $"Cleanup closing legacy manual modal. openModal={DescribeOpenModal(modalContainer)}");
            modal.CloseModal();
            modalContainer.HideBackstop();
            RockLog.Info("Closed legacy manual RPS modal.");
            return;
        }

        ManualRpsChoiceModal? orphanedModal = modalContainer
            .GetChildren()
            .OfType<ManualRpsChoiceModal>()
            .FirstOrDefault();
        if (orphanedModal != null)
        {
            RockLog.Trace("Modal", $"Cleanup removing orphaned legacy manual modal. openModal={DescribeOpenModal(modalContainer)} childCount={modalContainer.GetChildCount()}");
            orphanedModal.QueueFree();
            modalContainer.HideBackstop();
            RockLog.Info("Closed orphaned legacy manual RPS modal.");
            return;
        }

        if (modalContainer.OpenModal == null)
        {
            modalContainer.HideBackstop();
        }
    }

    private static string DescribeOpenModal(NModalContainer container)
    {
        return container.OpenModal switch
        {
            null => "<none>",
            Node node => $"{node.Name}:{node.GetType().Name}",
            _ => container.OpenModal.GetType().Name
        };
    }

    private static string DescribeChildren(Node node)
    {
        return string.Join(
            ", ",
            node.GetChildren().Cast<Node>().Select(child =>
                $"{child.GetIndex()}={child.Name}:{child.GetType().Name}:insideTree={child.IsInsideTree()}"));
    }

    private static string DescribeNode(Node? node)
    {
        return node == null ? "<none>" : $"{node.Name}:{node.GetType().Name}";
    }
}
