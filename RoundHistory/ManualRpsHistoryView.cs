using Godot;
using Rock.Infrastructure;
using Rock.Models;
using Rock.Runtime;
using Rock.Ui;

namespace Rock.RoundHistory;

internal sealed partial class ManualRpsHistoryView : Control
{
    private bool _uiBuilt;
    private VBoxContainer _linesHost = null!;
    private Label _placeholderLabel = null!;
    private static readonly Font ChineseFont = CreateChineseFont();

    public ManualRpsHistoryView()
    {
        BuildUi();
        SetProcess(true);
    }

    public override void _EnterTree()
    {
        BuildUi();
    }

    public override void _Ready()
    {
        BuildUi();
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (!_uiBuilt)
        {
            BuildUi();
        }

        Refresh();
    }

    public void RefreshNow()
    {
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
        OffsetTop = 400f;
        OffsetRight = -16f;
        OffsetBottom = 676f;
        TopLevel = true;
        ZIndex = 4999;
        MouseFilter = MouseFilterEnum.Ignore;

        PanelContainer panel = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(344f, 224f)
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.05f, 0.09f, 0.92f),
            BorderColor = new Color(0.74f, 0.79f, 0.92f, 0.95f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14
        });
        AddChild(panel);

        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        VBoxContainer root = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        Label title = new()
        {
            Text = "猜拳记录",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontOverride("font", ChineseFont);
        title.AddThemeFontSizeOverride("font_size", 16);
        title.Modulate = new Color(1f, 0.96f, 0.76f, 1f);
        root.AddChild(title);

        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(300f, 176f),
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(scroll);

        _linesHost = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _linesHost.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_linesHost);

        _placeholderLabel = CreatePlaceholderLabel();
        _linesHost.AddChild(_placeholderLabel);

        Visible = false;
        RockLog.Trace("HistoryUi", $"BuildUi completed visible={Visible} offsets=({OffsetLeft},{OffsetTop},{OffsetRight},{OffsetBottom}).");
    }

    private void Refresh()
    {
        if (!_uiBuilt)
        {
            return;
        }

        IReadOnlyList<ManualRpsRoundHistoryEntry> entries = RockRuntime.Coordinator.GetRoundHistoryEntries();
        bool shouldShow = RockRuntime.Coordinator.ShouldShowRoundHistoryPanel;
        string currentFingerprint = _linesHost.GetMeta("fingerprint", "").AsString();
        RockLog.Trace(
            "HistoryUi",
            $"Refresh panel={shouldShow} contentReady={RockRuntime.Coordinator.ShouldShowRoundHistory} lines={entries.Count} currentFingerprint={currentFingerprint}.");
        Visible = shouldShow;
        if (!shouldShow)
        {
            if (_linesHost.GetChildCount() > 0)
            {
                foreach (Node child in _linesHost.GetChildren())
                {
                    child.QueueFree();
                }
            }

            RockLog.Trace("HistoryUi", "Refresh hid history panel and cleared children.");
            return;
        }

        if (!RockRuntime.Coordinator.ShouldShowRoundHistory || entries.Count == 0)
        {
            string waitingFingerprint = "__waiting__";
            if (_linesHost.GetMeta("fingerprint", "").AsString() == waitingFingerprint)
            {
                return;
            }

            RockLog.Trace(
                "HistoryUi",
                $"Refresh entering waiting state contentReady={RockRuntime.Coordinator.ShouldShowRoundHistory} lines={entries.Count}.");
            _linesHost.SetMeta("fingerprint", waitingFingerprint);
            foreach (Node child in _linesHost.GetChildren())
            {
                child.QueueFree();
            }

            _placeholderLabel = CreatePlaceholderLabel();
            _linesHost.AddChild(_placeholderLabel);
            return;
        }

        string fingerprint = string.Join(
            "\n",
            entries.Select(entry =>
                $"{entry.RoundNumber}|{string.Join(",", entry.Moves.Select(move => $"{move.PlayerName}:{move.Move}"))}|{entry.OutcomeText}"));
        if (_linesHost.GetMeta("fingerprint", "").AsString() == fingerprint)
        {
            return;
        }

        RockLog.Trace("HistoryUi", $"Refresh rendering history fingerprint={fingerprint}.");
        _linesHost.SetMeta("fingerprint", fingerprint);
        foreach (Node child in _linesHost.GetChildren())
        {
            child.QueueFree();
        }

        foreach (ManualRpsRoundHistoryEntry entry in entries)
        {
            _linesHost.AddChild(CreateHistoryEntry(entry));
        }
    }

    private static Font CreateChineseFont()
    {
        SystemFont font = new();
        font.FontNames = ["Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans SC", "SimHei", "SimSun"];
        return font;
    }

    private static Label CreatePlaceholderLabel()
    {
        Label label = new()
        {
            Text = "本轮结果整理中...",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(296f, 0f)
        };
        label.AddThemeFontOverride("font", ChineseFont);
        label.AddThemeFontSizeOverride("font_size", 14);
        label.Modulate = new Color(0.76f, 0.82f, 0.92f, 0.92f);
        return label;
    }

    private static Control CreateHistoryEntry(ManualRpsRoundHistoryEntry entry)
    {
        VBoxContainer container = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(296f, 0f)
        };
        container.AddThemeConstantOverride("separation", 4);

        Label title = new()
        {
            Text = $"R{entry.RoundNumber}",
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.Modulate = new Color(1f, 0.94f, 0.7f, 1f);
        container.AddChild(title);

        HBoxContainer movesRow = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        movesRow.AddThemeConstantOverride("separation", 8);
        container.AddChild(movesRow);

        foreach (ManualRpsRoundHistoryMove move in entry.Moves)
        {
            HBoxContainer moveItem = new()
            {
                MouseFilter = MouseFilterEnum.Ignore
            };
            moveItem.AddThemeConstantOverride("separation", 4);

            Label name = new()
            {
                Text = move.PlayerName,
                MouseFilter = MouseFilterEnum.Ignore
            };
            name.AddThemeFontOverride("font", ChineseFont);
            name.AddThemeFontSizeOverride("font_size", 13);
            name.Modulate = new Color(0.92f, 0.96f, 1f, 1f);
            moveItem.AddChild(name);

            Control icon = ManualRpsIconViewFactory.Create(move.Move, new Vector2(18f, 18f));
            ManualRpsIconViewFactory.SetTint(icon, Colors.White);
            moveItem.AddChild(icon);
            movesRow.AddChild(moveItem);
        }

        Label outcome = new()
        {
            Text = entry.OutcomeText,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        outcome.AddThemeFontSizeOverride("font_size", 13);
        outcome.Modulate = new Color(0.78f, 0.84f, 0.96f, 1f);
        container.AddChild(outcome);
        return container;
    }
}
