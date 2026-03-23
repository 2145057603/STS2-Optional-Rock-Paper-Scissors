namespace Rock.Models;

internal sealed record ManualRpsRoundHistoryMove(string PlayerName, ManualRpsMove Move);

internal sealed record ManualRpsRoundHistoryEntry(
    int RoundNumber,
    IReadOnlyList<ManualRpsRoundHistoryMove> Moves,
    string OutcomeText);
