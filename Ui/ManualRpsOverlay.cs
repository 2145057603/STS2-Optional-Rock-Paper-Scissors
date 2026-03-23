using Godot;
using MegaCrit.Sts2.Core.Nodes;
using Rock.Infrastructure;
using Rock.Models;
using Rock.Runtime;

namespace Rock.Ui;

internal sealed partial class ManualRpsOverlay : Control
{
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Button _rockButton = null!;
    private Button _paperButton = null!;
    private Button _scissorsButton = null!;
    private string? _localFeedback;

    public override void _Ready()
    {
        LayoutMode = 1;
        AnchorsPreset = (int)LayoutPreset.TopRight;
        AnchorLeft = 1f;
        AnchorRight = 1f;
        AnchorTop = 0f;
        AnchorBottom = 0f;
        OffsetLeft = -360f;
        OffsetTop = 24f;
        OffsetRight = -16f;
        OffsetBottom = 220f;
        ZIndex = 5000;
        TopLevel = true;
        MouseFilter = MouseFilterEnum.Pass;

        PanelContainer panel = new()
        {
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(344f, 180f)
        };
        AddChild(panel);

        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);

        VBoxContainer root = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        _titleLabel = new Label
        {
            Text = "Rock Manual RPS",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 22);
        root.AddChild(_titleLabel);

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 16);
        root.AddChild(_statusLabel);

        HBoxContainer buttonRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttonRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(buttonRow);

        _rockButton = CreateMoveButton("石头", ManualRpsMove.Rock);
        _paperButton = CreateMoveButton("布", ManualRpsMove.Paper);
        _scissorsButton = CreateMoveButton("剪刀", ManualRpsMove.Scissors);
        buttonRow.AddChild(_rockButton);
        buttonRow.AddChild(_paperButton);
        buttonRow.AddChild(_scissorsButton);

        Visible = false;
        RockLog.Info("Manual RPS overlay is ready.");
        Refresh();
    }

    public override void _Process(double delta)
    {
        Refresh();
    }

    private Button CreateMoveButton(string text, ManualRpsMove move)
    {
        Button button = new()
        {
            Text = text,
            CustomMinimumSize = new Vector2(92f, 40f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        button.Pressed += () => OnMoveButtonPressed(move);
        return button;
    }

    private void OnMoveButtonPressed(ManualRpsMove move)
    {
        bool published = RockRuntime.Coordinator.PublishLocalMove(move);
        _localFeedback = published ? $"已选择：{GetMoveText(move)}" : "当前无法提交出拳，请确认仍处于共享宝箱阶段。";
        Refresh();
    }

    private void Refresh()
    {
        if (!RockRuntime.Coordinator.HasActiveSession)
        {
            Visible = false;
            _localFeedback = null;
            return;
        }

        Visible = true;

        bool hasPendingAward = RockRuntime.Coordinator.HasPendingAward;
        bool hasLocalMove = RockRuntime.Coordinator.TryGetLocalMove(out ManualRpsMove localMove);

        _titleLabel.Text = hasPendingAward ? "共享遗物猜拳中" : "共享遗物预选出拳";
        _statusLabel.Text = BuildStatusText(hasPendingAward, hasLocalMove, localMove);

        UpdateButtonState(_rockButton, hasLocalMove && localMove == ManualRpsMove.Rock);
        UpdateButtonState(_paperButton, hasLocalMove && localMove == ManualRpsMove.Paper);
        UpdateButtonState(_scissorsButton, hasLocalMove && localMove == ManualRpsMove.Scissors);
    }

    private string BuildStatusText(bool hasPendingAward, bool hasLocalMove, ManualRpsMove localMove)
    {
        if (hasPendingAward)
        {
            if (hasLocalMove)
            {
                return $"多人选择了同一遗物。\n你已提交：{GetMoveText(localMove)}\n正在等待其他玩家出拳...";
            }

            return "多人选择了同一遗物。\n请点击下方按钮选择石头、布或剪刀。";
        }

        if (hasLocalMove)
        {
            return $"当前预选：{GetMoveText(localMove)}\n如果之后多人选择同一遗物，将优先使用这次选择。";
        }

        if (!string.IsNullOrWhiteSpace(_localFeedback))
        {
            return _localFeedback;
        }

        return "你可以先点击下方按钮预选出拳。\n如果多人选择同一遗物，系统会等待双方手动出拳。";
    }

    private static void UpdateButtonState(Button button, bool isSelected)
    {
        button.Modulate = isSelected ? new Color(0.84f, 0.93f, 0.66f, 1f) : Colors.White;
    }

    private static string GetMoveText(ManualRpsMove move)
    {
        return move switch
        {
            ManualRpsMove.Rock => "石头",
            ManualRpsMove.Paper => "布",
            ManualRpsMove.Scissors => "剪刀",
            _ => move.ToString()
        };
    }
}
