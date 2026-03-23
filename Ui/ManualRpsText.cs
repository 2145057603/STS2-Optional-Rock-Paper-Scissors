using Rock.Models;

namespace Rock.Ui;

internal static class ManualRpsText
{
    public static string GetMoveName(ManualRpsMove move)
    {
        return move switch
        {
            ManualRpsMove.Rock => "石头",
            ManualRpsMove.Paper => "布",
            ManualRpsMove.Scissors => "剪刀",
            _ => move.ToString()
        };
    }
}
