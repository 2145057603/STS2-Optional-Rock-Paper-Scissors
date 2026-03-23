using Godot;
using Rock.Infrastructure;
using Rock.Models;

namespace Rock.Ui;

internal static class ManualRpsIconViewFactory
{
    public static Control Create(ManualRpsMove move, Vector2 minimumSize)
    {
        Texture2D? texture = ManualRpsIconTextures.Get(move);
        if (texture != null)
        {
            RockLog.Trace(
                "Icons",
                $"Create texture icon view move={move} minSize={minimumSize} textureSize={texture.GetSize()}.");
            return new TextureRect
            {
                Texture = texture,
                CustomMinimumSize = minimumSize,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
        }

        RockLog.Warn($"Falling back to ManualRpsIconGlyph for move={move} minSize={minimumSize}.");
        return new ManualRpsIconGlyph
        {
            Move = move,
            CustomMinimumSize = minimumSize,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }

    public static void SetMove(Control control, ManualRpsMove move)
    {
        if (control is TextureRect textureRect)
        {
            textureRect.Texture = ManualRpsIconTextures.Get(move);
            RockLog.Trace(
                "Icons",
                $"SetMove TextureRect move={move} textureAssigned={textureRect.Texture != null} size={textureRect.Size} minSize={textureRect.CustomMinimumSize}.");
            return;
        }

        if (control is ManualRpsIconGlyph glyph)
        {
            glyph.Move = move;
            RockLog.Trace(
                "Icons",
                $"SetMove ManualRpsIconGlyph move={move} size={glyph.Size} minSize={glyph.CustomMinimumSize}.");
        }
    }

    public static void SetTint(Control control, Color tint)
    {
        if (control is CanvasItem item)
        {
            item.Modulate = tint;
        }

        if (control is ManualRpsIconGlyph glyph)
        {
            glyph.Tint = tint;
        }
    }
}
