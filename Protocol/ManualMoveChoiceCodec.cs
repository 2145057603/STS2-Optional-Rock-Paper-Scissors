using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using Rock.Models;

namespace Rock.Protocol;

internal static class ManualMoveChoiceCodec
{
    private const int Header = 7023101;
    private const int CancelValue = -1;

    public static PlayerChoiceResult Encode(ManualRpsMove move)
    {
        return PlayerChoiceResult.FromIndexes(new List<int> { Header, (int)move });
    }

    public static PlayerChoiceResult EncodeCancel()
    {
        return PlayerChoiceResult.FromIndexes(new List<int> { Header, CancelValue });
    }

    public static bool TryDecode(NetPlayerChoiceResult result, out ManualRpsMove move, out bool isCancel)
    {
        move = default;
        isCancel = false;

        if (result.type != MegaCrit.Sts2.Core.Entities.Models.PlayerChoiceType.Index ||
            result.indexes == null ||
            result.indexes.Count != 2 ||
            result.indexes[0] != Header)
        {
            return false;
        }

        if (result.indexes[1] == CancelValue)
        {
            isCancel = true;
            return true;
        }

        if (!Enum.IsDefined(typeof(ManualRpsMove), result.indexes[1]))
        {
            return false;
        }

        move = (ManualRpsMove)result.indexes[1];
        return true;
    }
}
