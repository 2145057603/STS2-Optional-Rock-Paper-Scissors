using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Rock.HandMarkers;

internal static class PlayerHandMarkerNameResolver
{
    private static readonly string[] CandidatePropertyNames =
    {
        "DisplayName",
        "PlayerName",
        "UserName",
        "Username",
        "Name",
        "Nickname",
        "NickName"
    };

    private static readonly string[] CandidateFieldNames =
    {
        "_displayName",
        "_playerName",
        "_userName",
        "_username",
        "_name",
        "_nickname"
    };

    public static string Resolve(Player player, int playerIndex)
    {
        foreach (string propertyName in CandidatePropertyNames)
        {
            if (AccessTools.Property(typeof(Player), propertyName) is not { } property)
            {
                continue;
            }

            if (property.GetValue(player) is string propertyValue && !string.IsNullOrWhiteSpace(propertyValue))
            {
                return propertyValue.Trim();
            }
        }

        foreach (string fieldName in CandidateFieldNames)
        {
            if (AccessTools.Field(typeof(Player), fieldName) is not { } field)
            {
                continue;
            }

            if (field.GetValue(player) is string fieldValue && !string.IsNullOrWhiteSpace(fieldValue))
            {
                return fieldValue.Trim();
            }
        }

        return $"P{playerIndex + 1}";
    }

    public static string ResolveMarkerText(Player player, int playerIndex)
    {
        string resolved = Resolve(player, playerIndex).Trim();
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return $"P{playerIndex + 1}";
        }

        if (resolved.All(char.IsDigit))
        {
            string suffix = resolved.Length <= 4 ? resolved : resolved[^4..];
            return $"#{suffix}";
        }

        string collapsed = string.Concat(resolved.Where(character => !char.IsWhiteSpace(character)));
        if (collapsed.Length <= 4)
        {
            return collapsed;
        }

        bool hasAsciiLetterOrDigit = collapsed.Any(character => character <= 127 && char.IsLetterOrDigit(character));
        return hasAsciiLetterOrDigit
            ? collapsed[..Math.Min(6, collapsed.Length)].ToUpperInvariant()
            : collapsed[..Math.Min(4, collapsed.Length)];
    }
}
