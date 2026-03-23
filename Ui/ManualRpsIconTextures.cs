using Godot;
using Rock.Infrastructure;
using Rock.Models;

namespace Rock.Ui;

internal static class ManualRpsIconTextures
{
    private static readonly Dictionary<ManualRpsMove, Texture2D?> Cache = new();

    public static Texture2D? Get(ManualRpsMove move)
    {
        if (Cache.TryGetValue(move, out Texture2D? texture))
        {
            RockLog.Trace("Icons", $"Cache hit move={move} found={texture != null}.");
            return texture;
        }

        RockLog.Trace("Icons", $"Cache miss move={move}; starting icon lookup.");
        texture = Load(move);
        Cache[move] = texture;
        RockLog.Trace("Icons", $"Cache store move={move} found={texture != null}.");
        return texture;
    }

    private static Texture2D? Load(ManualRpsMove move)
    {
        string fileName = move switch
        {
            ManualRpsMove.Rock => "rock.png",
            ManualRpsMove.Paper => "paper.png",
            ManualRpsMove.Scissors => "scissors.png",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        string[] resourceCandidates =
        [
            $"res://Assets/RpsIcons/{fileName}",
            $"res://Ui/Assets/RpsIcons/{fileName}"
        ];

        foreach (string resourcePath in resourceCandidates)
        {
            bool exists = ResourceLoader.Exists(resourcePath);
            RockLog.Trace("Icons", $"Trying resource path move={move} path={resourcePath} exists={exists}.");
            Texture2D? resourceTexture = ResourceLoader.Load<Texture2D>(resourcePath);
            if (resourceTexture != null)
            {
                RockLog.Trace(
                    "Icons",
                    $"Loaded resource icon move={move} path={resourcePath} size={resourceTexture.GetSize()}.");
                return resourceTexture;
            }
        }

        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Ui", "Assets", "RpsIcons", fileName),
            Path.Combine(AppContext.BaseDirectory, "mods", "Rock", "Ui", "Assets", "RpsIcons", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Ui", "Assets", "RpsIcons", fileName)
        ];

        foreach (string path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
            {
                RockLog.Trace("Icons", $"Disk path missing move={move} path={path}.");
                continue;
            }

            Image image = Image.LoadFromFile(path);
            if (image == null || image.IsEmpty())
            {
                RockLog.Warn($"Disk icon load failed for move={move} path={path}.");
                continue;
            }

            RockLog.Trace("Icons", $"Loaded disk icon move={move} path={path} size=({image.GetWidth()}x{image.GetHeight()}).");
            return ImageTexture.CreateFromImage(image);
        }

        RockLog.Warn($"Could not find icon for move={move}. Resource and disk lookups both failed.");
        return null;
    }
}
