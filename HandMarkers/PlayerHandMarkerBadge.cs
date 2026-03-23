using Godot;

namespace Rock.HandMarkers;

internal sealed partial class PlayerHandMarkerBadge : Control
{
    private readonly Label _nameLabel;
    private readonly Label _moveLabel;
    private readonly StyleBoxFlat _panelStyle;

    public PlayerHandMarkerBadge()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        ZIndex = 50;

        PanelContainer panel = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(124f, 52f)
        };
        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.07f, 0.12f, 0.94f),
            BorderWidthBottom = 3,
            BorderWidthTop = 3,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10
        };
        panel.AddThemeStyleboxOverride("panel", _panelStyle);
        AddChild(panel);

        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        VBoxContainer root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 2);
        margin.AddChild(root);

        _nameLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        root.AddChild(_nameLabel);

        _moveLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _moveLabel.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(_moveLabel);
    }

    public void UpdateState(string playerName, string moveText, Color accent, bool hasCommittedMove)
    {
        _nameLabel.Text = playerName;
        _nameLabel.Modulate = accent;
        _moveLabel.Text = moveText;
        _moveLabel.Modulate = hasCommittedMove
            ? new Color(1f, 0.97f, 0.8f, 1f)
            : new Color(0.82f, 0.87f, 0.95f, 1f);
        _panelStyle.BorderColor = accent;
        _panelStyle.BgColor = hasCommittedMove
            ? new Color(0.10f, 0.11f, 0.16f, 0.97f)
            : new Color(0.05f, 0.07f, 0.12f, 0.94f);
    }
}
