using Godot;
using Rock.Infrastructure;
using Rock.Models;

namespace Rock.Ui;

internal sealed partial class ManualRpsIconGlyph : Control
{
    private ManualRpsMove _move;
    private Color _tint = Colors.White;
    private bool _hasLoggedDraw;

    public ManualRpsMove Move
    {
        get => _move;
        set
        {
            _move = value;
            QueueRedraw();
        }
    }

    public Color Tint
    {
        get => _tint;
        set
        {
            _tint = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        Rect2 rect = GetRect();
        if (!_hasLoggedDraw)
        {
            _hasLoggedDraw = true;
            RockLog.Trace("Icons", $"Drawing fallback glyph move={_move} rect={rect} visible={Visible}.");
        }

        Texture2D? texture = ManualRpsIconTextures.Get(_move);
        if (texture != null)
        {
            Vector2 textureSize = texture.GetSize();
            if (textureSize.X > 0f && textureSize.Y > 0f)
            {
                float scale = Mathf.Min(rect.Size.X / textureSize.X, rect.Size.Y / textureSize.Y);
                Vector2 drawSize = textureSize * scale;
                Rect2 drawRect = new((rect.Size - drawSize) * 0.5f, drawSize);
                DrawTextureRect(texture, drawRect, tile: false, modulate: _tint);
                return;
            }
        }

        Vector2 center = rect.Size * 0.5f;
        float size = Mathf.Min(rect.Size.X, rect.Size.Y);
        Color stroke = _tint;
        Color fill = new(_tint.R, _tint.G, _tint.B, 0.18f);

        switch (_move)
        {
            case ManualRpsMove.Rock:
                DrawRock(center, size, fill, stroke);
                break;
            case ManualRpsMove.Paper:
                DrawPaper(center, size, fill, stroke);
                break;
            case ManualRpsMove.Scissors:
                DrawScissors(center, size, stroke);
                break;
        }
    }

    private void DrawRock(Vector2 center, float size, Color fill, Color stroke)
    {
        float radius = size * 0.2f;
        Vector2[] points =
        [
            center + new Vector2(-size * 0.18f, size * 0.06f),
            center + new Vector2(-size * 0.02f, -size * 0.1f),
            center + new Vector2(size * 0.18f, -size * 0.03f),
            center + new Vector2(size * 0.1f, size * 0.16f)
        ];

        foreach (Vector2 point in points)
        {
            DrawCircle(point, radius, fill);
            DrawArc(point, radius, 0f, Mathf.Tau, 24, stroke, 3f);
        }
    }

    private void DrawPaper(Vector2 center, float size, Color fill, Color stroke)
    {
        Vector2 topLeft = center + new Vector2(-size * 0.22f, -size * 0.28f);
        Vector2 paperSize = new(size * 0.42f, size * 0.56f);
        Rect2 paper = new(topLeft, paperSize);
        Vector2 foldA = paper.Position + new Vector2(paperSize.X * 0.72f, 0f);
        Vector2 foldB = paper.Position + new Vector2(paperSize.X, paperSize.Y * 0.18f);
        Vector2 foldC = paper.Position + new Vector2(paperSize.X, 0f);

        DrawRect(paper, fill, filled: true);
        DrawRect(paper, stroke, filled: false, width: 3f);
        DrawLine(foldA, foldB, stroke, 3f);
        DrawLine(foldA, foldC, stroke, 3f);

        for (int i = 0; i < 3; i++)
        {
            float y = paper.Position.Y + paperSize.Y * (0.3f + i * 0.18f);
            DrawLine(
                new Vector2(paper.Position.X + paperSize.X * 0.16f, y),
                new Vector2(paper.Position.X + paperSize.X * 0.76f, y),
                stroke,
                2f);
        }
    }

    private void DrawScissors(Vector2 center, float size, Color stroke)
    {
        float loopRadius = size * 0.1f;
        Vector2 leftLoop = center + new Vector2(-size * 0.16f, size * 0.16f);
        Vector2 rightLoop = center + new Vector2(size * 0.02f, size * 0.16f);
        Vector2 pivot = center + new Vector2(-size * 0.02f, size * 0.02f);
        Vector2 topBlade = center + new Vector2(size * 0.24f, -size * 0.22f);
        Vector2 bottomBlade = center + new Vector2(size * 0.24f, size * 0.02f);

        DrawArc(leftLoop, loopRadius, 0f, Mathf.Tau, 24, stroke, 3f);
        DrawArc(rightLoop, loopRadius, 0f, Mathf.Tau, 24, stroke, 3f);
        DrawLine(leftLoop + new Vector2(loopRadius * 0.9f, -loopRadius * 0.5f), pivot, stroke, 4f);
        DrawLine(rightLoop + new Vector2(0f, -loopRadius), pivot, stroke, 4f);
        DrawLine(pivot, topBlade, stroke, 4f);
        DrawLine(pivot, bottomBlade, stroke, 4f);
        DrawCircle(pivot, 4f, stroke);
    }
}
