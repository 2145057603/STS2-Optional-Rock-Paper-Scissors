using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using Rock.Infrastructure;
using Rock.Models;
using Rock.Runtime;

namespace Rock.Ui;

internal sealed partial class ManualRpsChoiceModal : Control, IScreenContext
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

    public Control? DefaultFocusedControl => _rockButton;

    public ManualRpsChoiceModal()
    {
        BuildUi();
        SetProcess(true);
        SetProcessInput(true);
        SetProcessUnhandledInput(true);
    }

    public override void _EnterTree()
    {
        BuildUi();
    }

    public override void _Ready()
    {
        BuildUi();
        RockLog.Info($"Manual RPS choice modal is ready. childCount={GetChildCount()}");
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
        AnchorsPreset = (int)LayoutPreset.FullRect;
        MouseFilter = MouseFilterEnum.Stop;

        CenterContainer center = new()
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(center);

        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(560f, 260f),
            MouseFilter = MouseFilterEnum.Stop
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.07f, 0.11f, 0.97f),
            BorderColor = new Color(1f, 0.9f, 0.46f, 1f),
            BorderWidthBottom = 4,
            BorderWidthTop = 4,
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18
        });
        center.AddChild(panel);

        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        panel.AddChild(margin);

        VBoxContainer root = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 14);
        margin.AddChild(root);

        _titleLabel = new Label
        {
            Text = "CHOOSE R / P / S",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 24);
        _titleLabel.Modulate = new Color(1f, 0.96f, 0.76f, 1f);
        root.AddChild(_titleLabel);

        PanelContainer previewPanel = new()
        {
            CustomMinimumSize = new Vector2(260f, 110f),
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
        previewCenter.AddThemeConstantOverride("separation", 4);
        previewPanel.AddChild(previewCenter);

        _previewLabel = new Label
        {
            Text = "WAITING",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _previewLabel.AddThemeFontSizeOverride("font_size", 22);
        _previewLabel.Modulate = new Color(0.94f, 0.97f, 1f, 1f);
        previewCenter.AddChild(_previewLabel);

        _statusLabel = new Label
        {
            Text = "PRESS 1 / 2 / 3",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 16);
        _statusLabel.Modulate = new Color(0.78f, 0.84f, 0.96f, 1f);
        previewCenter.AddChild(_statusLabel);

        _previewIconHost = ManualRpsIconViewFactory.Create(ManualRpsMove.Rock, new Vector2(84f, 54f));
        previewCenter.AddChild(_previewIconHost);

        HBoxContainer buttonRow = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttonRow.AddThemeConstantOverride("separation", 12);
        root.AddChild(buttonRow);

        _rockButton = ManualRpsMoveButtonFactory.Create("1", ManualRpsMove.Rock, new Vector2(140f, 128f), () => OnMovePressed(ManualRpsMove.Rock));
        _paperButton = ManualRpsMoveButtonFactory.Create("2", ManualRpsMove.Paper, new Vector2(140f, 128f), () => OnMovePressed(ManualRpsMove.Paper));
        _scissorsButton = ManualRpsMoveButtonFactory.Create("3", ManualRpsMove.Scissors, new Vector2(140f, 128f), () => OnMovePressed(ManualRpsMove.Scissors));
        buttonRow.AddChild(_rockButton);
        buttonRow.AddChild(_paperButton);
        buttonRow.AddChild(_scissorsButton);

        _confirmButton = new Button
        {
            Text = "CONFIRM",
            CustomMinimumSize = new Vector2(220f, 52f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        _confirmButton.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.22f, 0.28f, 0.16f, 0.98f),
            BorderColor = new Color(0.92f, 0.98f, 0.62f, 1f),
            BorderWidthBottom = 3,
            BorderWidthTop = 3,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12
        });
        _confirmButton.Pressed += OnConfirmPressed;
        root.AddChild(_confirmButton);
        RockLog.Trace("Modal", $"BuildUi completed childCount={GetChildCount()}.");
    }

    public override void _Process(double delta)
    {
        if (!_uiBuilt)
        {
            BuildUi();
        }

        if (!RockRuntime.Coordinator.IsAwaitingManualResolution)
        {
            RockLog.Info("Manual RPS choice modal closing because manual resolution is no longer pending.");
            CloseModal();
            return;
        }

        Refresh();
    }

    public override void _Input(InputEvent @event)
    {
    }

    public override void _UnhandledInput(InputEvent @event)
    {
    }

    private void OnMovePressed(ManualRpsMove move)
    {
        bool published = RockRuntime.Coordinator.SetLocalDraftMove(move);
        RockLog.Info(published
            ? $"Manual modal drafted {move}."
            : $"Manual modal could not draft {move}.");

        Refresh();
    }

    private void OnConfirmPressed()
    {
        bool toggled = RockRuntime.Coordinator.ToggleLocalDraftConfirmation();
        RockLog.Info(toggled
            ? "Manual modal toggled confirmation."
            : "Manual modal could not toggle confirmation.");
        Refresh();
    }

    private void Refresh()
    {
        if (!_uiBuilt)
        {
            return;
        }

        bool hasCommittedMove = RockRuntime.Coordinator.TryGetLocalMove(out ManualRpsMove committedMove);
        bool hasDraftMove = RockRuntime.Coordinator.TryGetLocalDraftMove(out ManualRpsMove draftMove);
        ManualRpsMove previewMove = hasDraftMove ? draftMove : hasCommittedMove ? committedMove : ManualRpsMove.Rock;
        bool isConfirmedDraft = hasCommittedMove && hasDraftMove && committedMove == draftMove;
        _titleLabel.Text = hasCommittedMove ? "CHOICE SENT" : hasDraftMove ? "PRESS ENTER OR CONFIRM" : "CHOOSE 1 / 2 / 3";
        _titleLabel.Modulate = hasCommittedMove
            ? new Color(1f, 0.93f, 0.62f, 1f)
            : hasDraftMove
                ? new Color(0.82f, 0.93f, 1f, 1f)
                : new Color(1f, 0.96f, 0.76f, 1f);
        _previewLabel.Text = hasDraftMove
            ? "SELECTED"
            : hasCommittedMove
                ? "CONFIRMED"
                : "NONE";
        _previewLabel.Modulate = hasCommittedMove
            ? new Color(1f, 0.95f, 0.68f, 1f)
            : hasDraftMove
                ? new Color(0.82f, 0.93f, 1f, 1f)
                : new Color(0.94f, 0.97f, 1f, 1f);
        _statusLabel.Text = hasCommittedMove
            ? isConfirmedDraft ? "ENTER AGAIN TO CANCEL" : "WAITING FOR OTHER PLAYER"
            : hasDraftMove ? "PRESS ENTER OR CLICK CONFIRM" : "PRESS 1 / 2 / 3";
        _statusLabel.Modulate = hasCommittedMove
            ? new Color(0.99f, 0.9f, 0.5f, 1f)
            : hasDraftMove
                ? new Color(0.82f, 0.93f, 1f, 1f)
                : new Color(0.78f, 0.84f, 0.96f, 1f);

        if (_previewIconHost is Control previewGlyph)
        {
            previewGlyph.Visible = hasDraftMove || hasCommittedMove;
            ManualRpsIconViewFactory.SetMove(previewGlyph, previewMove);
            ManualRpsIconViewFactory.SetTint(
                previewGlyph,
                hasCommittedMove
                    ? new Color(0.98f, 0.91f, 0.52f, 1f)
                    : hasDraftMove
                        ? new Color(0.74f, 0.88f, 1f, 1f)
                        : new Color(0.68f, 0.72f, 0.82f, 1f));
        }

        ManualRpsMoveButtonFactory.ApplySelectedVisual(_rockButton, hasDraftMove && draftMove == ManualRpsMove.Rock);
        ManualRpsMoveButtonFactory.ApplySelectedVisual(_paperButton, hasDraftMove && draftMove == ManualRpsMove.Paper);
        ManualRpsMoveButtonFactory.ApplySelectedVisual(_scissorsButton, hasDraftMove && draftMove == ManualRpsMove.Scissors);
        _rockButton.Modulate = hasDraftMove && draftMove != ManualRpsMove.Rock ? new Color(0.78f, 0.78f, 0.82f, 0.92f) : Colors.White;
        _paperButton.Modulate = hasDraftMove && draftMove != ManualRpsMove.Paper ? new Color(0.78f, 0.78f, 0.82f, 0.92f) : Colors.White;
        _scissorsButton.Modulate = hasDraftMove && draftMove != ManualRpsMove.Scissors ? new Color(0.78f, 0.78f, 0.82f, 0.92f) : Colors.White;
        _confirmButton.Disabled = !hasDraftMove && !hasCommittedMove;
        _confirmButton.Text = isConfirmedDraft ? "CANCEL" : "CONFIRM";

        RockLog.Trace("Modal", $"Refresh visible={Visible} size={Size} preview={_previewLabel.Text}.");
    }

    public void RefreshNow()
    {
        Refresh();
    }

    public void CloseModal()
    {
        if (NModalContainer.Instance?.OpenModal == this)
        {
            NModalContainer.Instance.Clear();
        }
        else
        {
            QueueFree();
        }
    }
}
