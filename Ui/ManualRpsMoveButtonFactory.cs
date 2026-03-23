using Godot;
using MegaCrit.Sts2.addons.mega_text;
using Rock.Models;

namespace Rock.Ui;

internal static class ManualRpsMoveButtonFactory
{
    public static Button Create(string hotkey, ManualRpsMove move, Vector2 minSize, Action onPressed)
    {
        Button button = new()
        {
            Text = string.Empty,
            CustomMinimumSize = minSize,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.All
        };
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.13f, 0.15f, 0.2f, 0.96f), new Color(0.58f, 0.67f, 0.9f, 1f), 3));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.18f, 0.2f, 0.27f, 0.98f), new Color(0.8f, 0.86f, 0.99f, 1f), 4));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.22f, 0.25f, 0.33f, 1f), new Color(1f, 0.92f, 0.56f, 1f), 4));
        button.AddThemeStyleboxOverride("focus", CreateButtonStyle(new Color(0.18f, 0.2f, 0.27f, 0.98f), new Color(1f, 0.92f, 0.56f, 1f), 5));

        MarginContainer margin = new()
        {
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        button.AddChild(margin);

        VBoxContainer column = new()
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 6);
        margin.AddChild(column);

        MegaLabel hotkeyLabel = new()
        {
            Text = hotkey,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutoSizeEnabled = false,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        hotkeyLabel.AddThemeFontSizeOverride("font_size", 28);
        hotkeyLabel.Modulate = new Color(0.93f, 0.96f, 1f, 1f);
        column.AddChild(hotkeyLabel);
        button.SetMeta("RockHotkeyLabel", hotkeyLabel);

        Control glyph = ManualRpsIconViewFactory.Create(move, new Vector2(minSize.X - 40f, Mathf.Max(32f, minSize.Y - 74f)));
        glyph.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        glyph.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.SetMeta("RockGlyph", glyph);
        column.AddChild(glyph);

        button.Pressed += onPressed;
        return button;
    }

    public static void ApplySelectedVisual(Button button, bool isSelected)
    {
        Color tint = isSelected
            ? new Color(0.98f, 0.91f, 0.52f, 1f)
            : new Color(0.87f, 0.89f, 0.95f, 1f);

        button.Modulate = Colors.White;
        button.SelfModulate = Colors.White;
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(
            isSelected ? new Color(0.3f, 0.27f, 0.1f, 0.98f) : new Color(0.13f, 0.15f, 0.2f, 0.96f),
            isSelected ? new Color(1f, 0.9f, 0.44f, 1f) : new Color(0.58f, 0.67f, 0.9f, 1f),
            isSelected ? 5 : 3));

        if (button.GetMeta("RockGlyph").AsGodotObject() is Control glyph)
        {
            ManualRpsIconViewFactory.SetTint(glyph, tint);
        }

        if (button.GetMeta("RockHotkeyLabel").AsGodotObject() is CanvasItem hotkeyLabel)
        {
            hotkeyLabel.Modulate = isSelected
                ? new Color(1f, 0.96f, 0.72f, 1f)
                : new Color(0.93f, 0.96f, 1f, 1f);
        }

    }

    private static StyleBoxFlat CreateButtonStyle(Color background, Color border, int borderWidth)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthBottom = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12
        };
    }
}
