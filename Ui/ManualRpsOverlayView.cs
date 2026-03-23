using Godot;
using Rock.Infrastructure;
using Rock.Models;
using Rock.Runtime;

namespace Rock.Ui;

internal sealed partial class ManualRpsOverlayView : Control
{
    private bool _uiBuilt;
    private Control _previewIconHost = null!;
    private Label _titleLabel = null!;
    private Label _previewLabel = null!;
    private Label _statusLabel = null!;
    private Button _rockButton = null!;
    private Button _paperButton = null!;
    private Button _scissorsButton = null!;
    private Button _confirmButton = null!;

    public ManualRpsOverlayView()
    {
        BuildUi();
        SetProcess(true);
    }

    public override void _EnterTree()
    {
        BuildUi();
        RockLog.Info("Manual RPS overlay view entered tree.");
    }

    public override void _Ready()
    {
        BuildUi();
        RockLog.Info($"Manual RPS overlay view is ready. childCount={GetChildCount()}");
        Refresh();
    }

    private void BuildUi()
    {
        if (_uiBuilt)
        {
            return;
        }

        _uiBuilt = true;
        LayoutMode = 1;
        AnchorsPreset = (int)LayoutPreset.TopRight;
        AnchorLeft = 1f;
        AnchorRight = 1f;
        AnchorTop = 0f;
        AnchorBottom = 0f;
        OffsetLeft = -360f;
        OffsetTop = 92f;
        OffsetRight = -16f;
        OffsetBottom = 288f;
        TopLevel = true;
        ZIndex = 5000;
        MouseFilter = MouseFilterEnum.Pass;

        PanelContainer panel = new()
        {
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(344f, 180f)
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.05f, 0.09f, 0.96f),
            BorderColor = new Color(1f, 0.9f, 0.46f, 1f),
            BorderWidthBottom = 3,
            BorderWidthTop = 3,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14
        });
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
            Text = "PRESS 1 / 2 / 3",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.Modulate = new Color(1f, 0.96f, 0.76f, 1f);
        root.AddChild(_titleLabel);

        PanelContainer previewPanel = new()
        {
            CustomMinimumSize = new Vector2(112f, 88f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        previewPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.15f, 0.22f, 0.98f),
            BorderColor = new Color(0.7f, 0.8f, 1f, 1f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10
        });
        root.AddChild(previewPanel);

        VBoxContainer previewCenter = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        previewCenter.Alignment = BoxContainer.AlignmentMode.Center;
        previewCenter.AddThemeConstantOverride("separation", 2);
        previewPanel.AddChild(previewCenter);

        _previewLabel = new Label
        {
            Text = "NONE",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _previewLabel.AddThemeFontSizeOverride("font_size", 16);
        _previewLabel.Modulate = new Color(0.94f, 0.97f, 1f, 1f);
        previewCenter.AddChild(_previewLabel);

        _statusLabel = new Label
        {
            Text = "WAITING",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        _statusLabel.Modulate = new Color(0.78f, 0.84f, 0.96f, 1f);
        previewCenter.AddChild(_statusLabel);

        _previewIconHost = ManualRpsIconViewFactory.Create(ManualRpsMove.Rock, new Vector2(68f, 40f));
        previewCenter.AddChild(_previewIconHost);

        HBoxContainer buttonRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttonRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(buttonRow);

        _rockButton = ManualRpsMoveButtonFactory.Create("1", ManualRpsMove.Rock, new Vector2(92f, 108f), () => OnMoveButtonPressed(ManualRpsMove.Rock));
        _paperButton = ManualRpsMoveButtonFactory.Create("2", ManualRpsMove.Paper, new Vector2(92f, 108f), () => OnMoveButtonPressed(ManualRpsMove.Paper));
        _scissorsButton = ManualRpsMoveButtonFactory.Create("3", ManualRpsMove.Scissors, new Vector2(92f, 108f), () => OnMoveButtonPressed(ManualRpsMove.Scissors));
        buttonRow.AddChild(_rockButton);
        buttonRow.AddChild(_paperButton);
        buttonRow.AddChild(_scissorsButton);

        _confirmButton = new Button
        {
            Text = "CONFIRM",
            CustomMinimumSize = new Vector2(144f, 40f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        _confirmButton.Pressed += OnConfirmPressed;
        root.AddChild(_confirmButton);

        Visible = false;
        RockLog.Trace("Overlay", $"BuildUi completed childCount={GetChildCount()}.");
    }

    public override void _Process(double delta)
    {
        if (!_uiBuilt)
        {
            BuildUi();
        }

        Refresh();
    }

    private void OnMoveButtonPressed(ManualRpsMove move)
    {
        RockRuntime.Coordinator.SetLocalDraftMove(move);
        Refresh();
    }

    private void OnConfirmPressed()
    {
        RockRuntime.Coordinator.CommitLocalDraftMove();
        Refresh();
    }

    private void Refresh()
    {
        if (!_uiBuilt)
        {
            return;
        }

        if (!RockRuntime.Coordinator.IsAwaitingManualResolution)
        {
            if (Visible)
            {
                RockLog.Debug("Manual RPS overlay view hidden because manual resolution is not pending.");
            }

            Visible = false;
            return;
        }

        if (!Visible)
        {
            RockLog.Info("Manual RPS overlay view became visible.");
        }

        Visible = true;

        bool hasCommittedMove = RockRuntime.Coordinator.TryGetLocalMove(out ManualRpsMove committedMove);
        bool hasDraftMove = RockRuntime.Coordinator.TryGetLocalDraftMove(out ManualRpsMove draftMove);
        bool hasAnySelection = hasDraftMove || hasCommittedMove;
        ManualRpsMove previewMove = hasDraftMove ? draftMove : committedMove;
        _titleLabel.Text = hasCommittedMove ? "CHOICE SENT" : hasDraftMove ? "PRESS CONFIRM" : "PRESS 1 / 2 / 3";
        _titleLabel.Modulate = hasCommittedMove
            ? new Color(1f, 0.93f, 0.62f, 1f)
            : hasDraftMove
                ? new Color(0.82f, 0.93f, 1f, 1f)
                : new Color(1f, 0.96f, 0.76f, 1f);
        _previewLabel.Text = hasDraftMove ? "SELECTED" : hasCommittedMove ? "CONFIRMED" : "NONE";
        _previewLabel.Modulate = hasCommittedMove
            ? new Color(1f, 0.95f, 0.68f, 1f)
            : hasDraftMove
                ? new Color(0.82f, 0.93f, 1f, 1f)
                : new Color(0.94f, 0.97f, 1f, 1f);
        _statusLabel.Text = hasCommittedMove ? "WAITING FOR OTHER PLAYER" : hasDraftMove ? "CLICK CONFIRM" : "WAITING";
        _statusLabel.Modulate = hasCommittedMove
            ? new Color(0.99f, 0.9f, 0.5f, 1f)
            : hasDraftMove
                ? new Color(0.82f, 0.93f, 1f, 1f)
                : new Color(0.78f, 0.84f, 0.96f, 1f);

        if (_previewIconHost is Control previewGlyph)
        {
            previewGlyph.Visible = hasAnySelection;
            if (hasAnySelection)
            {
                ManualRpsIconViewFactory.SetMove(previewGlyph, previewMove);
                ManualRpsIconViewFactory.SetTint(
                    previewGlyph,
                    hasCommittedMove
                        ? new Color(0.98f, 0.91f, 0.52f, 1f)
                        : hasDraftMove
                            ? new Color(0.74f, 0.88f, 1f, 1f)
                            : new Color(0.68f, 0.72f, 0.82f, 1f));
            }
        }

        ManualRpsMoveButtonFactory.ApplySelectedVisual(_rockButton, hasDraftMove && draftMove == ManualRpsMove.Rock);
        ManualRpsMoveButtonFactory.ApplySelectedVisual(_paperButton, hasDraftMove && draftMove == ManualRpsMove.Paper);
        ManualRpsMoveButtonFactory.ApplySelectedVisual(_scissorsButton, hasDraftMove && draftMove == ManualRpsMove.Scissors);
        _rockButton.Modulate = hasDraftMove && draftMove != ManualRpsMove.Rock ? new Color(0.78f, 0.78f, 0.82f, 0.92f) : Colors.White;
        _paperButton.Modulate = hasDraftMove && draftMove != ManualRpsMove.Paper ? new Color(0.78f, 0.78f, 0.82f, 0.92f) : Colors.White;
        _scissorsButton.Modulate = hasDraftMove && draftMove != ManualRpsMove.Scissors ? new Color(0.78f, 0.78f, 0.82f, 0.92f) : Colors.White;
        _confirmButton.Disabled = !hasDraftMove || (hasCommittedMove && committedMove == draftMove);
        _confirmButton.Text = hasCommittedMove && hasDraftMove && committedMove == draftMove ? "CONFIRMED" : "CONFIRM";
        RockLog.Trace("Overlay", $"Refresh visible={Visible} size={Size} preview={_previewLabel.Text}.");
    }

    public void RefreshNow()
    {
        Refresh();
    }
}
