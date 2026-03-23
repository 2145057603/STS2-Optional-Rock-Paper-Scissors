using Godot;

namespace Rock.HandMarkers;

internal sealed partial class PlayerHandNameplate : Control
{
    private readonly PanelContainer _panel;
    private readonly Label _label;
    private readonly StyleBoxFlat _panelStyle;

    public PlayerHandNameplate()
    {
        Name = "RockPlayerHandNameplate";
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Disabled;
        ZIndex = 8;

        _panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(74f, 28f)
        };
        AddChild(_panel);

        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.07f, 0.10f, 0.88f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.32f),
            ShadowSize = 3
        };
        _panel.AddThemeStyleboxOverride("panel", _panelStyle);

        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 3);
        _panel.AddChild(margin);

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _label.AddThemeFontSizeOverride("font_size", 13);
        margin.AddChild(_label);
    }

    public void UpdateState(string markerText, Color accent)
    {
        _label.Text = markerText;
        _label.Modulate = Colors.White;
        _panelStyle.BorderColor = accent;
        _panelStyle.BgColor = new Color(0.05f, 0.07f, 0.10f, 0.88f);

        float width = Math.Max(62f, 28f + markerText.Length * 12f);
        _panel.CustomMinimumSize = new Vector2(width, 28f);
        Size = _panel.CustomMinimumSize;
    }
}
