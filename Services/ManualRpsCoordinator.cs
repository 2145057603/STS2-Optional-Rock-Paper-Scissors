using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using Rock.HandMarkers;
using Rock.Infrastructure;
using Rock.Models;
using Rock.Protocol;
using Rock.Ui;

namespace Rock.Services;

internal sealed class ManualRpsCoordinator
{
    private readonly ManualRpsRoundAnimator _roundAnimator = new();
    private ManualRpsSession? _currentSession;
    private PlayerChoiceSynchronizer? _choiceSynchronizer;
    private TreasureRoomRelicSynchronizer? _pendingAwardSynchronizer;
    private PendingManualRpsFight? _pendingFight;
    private bool _isFinalizingPendingAward;
    private ManualRpsMove? _localDraftMove;
    private readonly List<ManualRpsRoundHistoryEntry> _roundHistoryEntries = new();
    private bool _isRoundHistoryContentVisible;

    public ManualRpsSession? CurrentSession => _currentSession;
    public bool HasActiveSession => _currentSession != null;
    public bool HasPendingAward => _pendingAwardSynchronizer != null;
    public bool IsAwaitingManualResolution => _currentSession != null && _pendingAwardSynchronizer != null && !_isFinalizingPendingAward;
    public bool ShouldShowRoundHistoryPanel =>
        (_currentSession != null && _pendingAwardSynchronizer != null) ||
        _roundHistoryEntries.Count > 0;
    public bool ShouldShowRoundHistory => _roundHistoryEntries.Count > 0 && _isRoundHistoryContentVisible;
    public IReadOnlyList<Player> GetHandMarkerPlayers() => _currentSession?.Players ?? Array.Empty<Player>();
    public IReadOnlyList<ManualRpsRoundHistoryEntry> GetRoundHistoryEntries() => _roundHistoryEntries;
    public string DescribeRoundHistoryDebugState()
    {
        return $"lines={_roundHistoryEntries.Count} panel={ShouldShowRoundHistoryPanel} contentReady={ShouldShowRoundHistory} visibleFlag={_isRoundHistoryContentVisible}";
    }

    public void AttachChoiceSynchronizer(PlayerChoiceSynchronizer synchronizer)
    {
        if (ReferenceEquals(_choiceSynchronizer, synchronizer))
        {
            return;
        }

        if (_choiceSynchronizer != null)
        {
            _choiceSynchronizer.PlayerChoiceReceived -= OnPlayerChoiceReceived;
        }

        _choiceSynchronizer = synchronizer;
        _choiceSynchronizer.PlayerChoiceReceived += OnPlayerChoiceReceived;
        RockLog.Debug("Attached player choice synchronizer.");
    }

    public void BeginSession(IReadOnlyList<RelicModel>? offeredRelics, IReadOnlyList<Player>? players)
    {
        if (offeredRelics == null || players == null)
        {
            _currentSession = null;
            _pendingAwardSynchronizer = null;
            _pendingFight = null;
            _isFinalizingPendingAward = false;
            _localDraftMove = null;
            _roundHistoryEntries.Clear();
            _isRoundHistoryContentVisible = false;
            TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
            TreasureRoomRelicUiAccessor.RefreshManualOverlay();
            TreasureRoomRelicUiAccessor.RefreshRoundHistory();
            return;
        }

        _pendingAwardSynchronizer = null;
        _pendingFight = null;
        _isFinalizingPendingAward = false;
        _currentSession = new ManualRpsSession(offeredRelics, players);
        _localDraftMove = null;
        _roundHistoryEntries.Clear();
        _isRoundHistoryContentVisible = false;
        RockLog.Info($"Started shared relic session with {offeredRelics.Count} relic option(s).");
        RockLog.Trace("Coordinator", $"BeginSession relicCount={offeredRelics.Count} players=[{string.Join(",", players.Select(player => player.NetId))}].");
        RockLog.Info("Press 1/2/3 during shared relic picking to choose Rock/Paper/Scissors.");
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        TreasureRoomRelicUiAccessor.RefreshRoundHistory();
    }

    public void EndSession()
    {
        _pendingAwardSynchronizer = null;
        _pendingFight = null;
        _isFinalizingPendingAward = false;

        if (_currentSession != null)
        {
            RockLog.Info("Ended shared relic session.");
        }

        RockLog.Trace("Coordinator", "EndSession clearing pending fight/session state.");
        _localDraftMove = null;
        ManualRpsModalManager.Hide();
        TreasureRoomRelicUiAccessor.ResetManualFightUi();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        TreasureRoomRelicUiAccessor.RefreshRoundHistory();
        _currentSession = null;
    }

    public bool SetLocalDraftMove(ManualRpsMove move)
    {
        if (_currentSession == null)
        {
            RockLog.Debug($"Ignored manual RPS draft {move} because no shared relic session is active.");
            return false;
        }

        _localDraftMove = move;
        RockLog.Info($"Drafted {move}. Click Confirm to submit.");
        ManualRpsModalManager.ShowOrRefresh();
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        return true;
    }

    public bool TryGetLocalDraftMove(out ManualRpsMove move)
    {
        if (_localDraftMove.HasValue)
        {
            move = _localDraftMove.Value;
            return true;
        }

        move = default;
        return false;
    }

    public bool CommitLocalDraftMove()
    {
        if (!_localDraftMove.HasValue)
        {
            RockLog.Debug("Ignored manual RPS confirm because no draft move is selected.");
            return false;
        }

        bool published = PublishLocalMove(_localDraftMove.Value);
        if (published)
        {
            RockLog.Info($"Confirmed {_localDraftMove.Value}.");
        }

        ManualRpsModalManager.ShowOrRefresh();
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        return published;
    }

    public bool ToggleLocalDraftConfirmation()
    {
        if (TryGetLocalMove(out ManualRpsMove committedMove) &&
            _localDraftMove.HasValue &&
            committedMove == _localDraftMove.Value)
        {
            return CancelLocalCommittedMove();
        }

        return CommitLocalDraftMove();
    }

    public bool CancelLocalCommittedMove()
    {
        ManualRpsSession? sessionAtStart = _currentSession;
        if (sessionAtStart == null)
        {
            RockLog.Debug("Ignored manual RPS cancel because no shared relic session is active.");
            return false;
        }

        if (_choiceSynchronizer == null)
        {
            RockLog.Warn("Ignored manual RPS cancel because PlayerChoiceSynchronizer is not attached.");
            return false;
        }

        Player? localPlayer = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
        if (localPlayer == null)
        {
            RockLog.Warn("Ignored manual RPS cancel because local player could not be resolved.");
            return false;
        }

        if (!sessionAtStart.TryGetMove(localPlayer.NetId, out _))
        {
            RockLog.Debug("Ignored manual RPS cancel because no committed move exists.");
            _localDraftMove = null;
            ManualRpsModalManager.ShowOrRefresh();
            TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
            return false;
        }

        uint choiceId = _choiceSynchronizer.ReserveChoiceId(localPlayer);
        _choiceSynchronizer.SyncLocalChoice(localPlayer, choiceId, ManualMoveChoiceCodec.EncodeCancel());

        if (!ReferenceEquals(_currentSession, sessionAtStart))
        {
            RockLog.Trace(
                "Coordinator",
                $"CancelLocalCommittedMove detected session change during SyncLocalChoice for player={localPlayer.NetId}; skipping post-sync UI refresh.");
            return true;
        }

        _currentSession.RemoveMove(localPlayer.NetId);
        _localDraftMove = null;
        RockLog.Info("Cancelled confirmed move.");
        RockLog.Trace("Coordinator", $"CancelLocalCommittedMove player={localPlayer.NetId} choiceId={choiceId}.");
        ManualRpsModalManager.ShowOrRefresh();
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        return true;
    }

    public bool PublishLocalMove(ManualRpsMove move)
    {
        ManualRpsSession? sessionAtStart = _currentSession;
        if (sessionAtStart == null)
        {
            RockLog.Debug($"Ignored manual RPS hotkey {move} because no shared relic session is active.");
            return false;
        }

        if (_choiceSynchronizer == null)
        {
            RockLog.Warn($"Ignored manual RPS hotkey {move} because PlayerChoiceSynchronizer is not attached.");
            return false;
        }

        Player? localPlayer = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
        if (localPlayer == null)
        {
            RockLog.Warn($"Ignored manual RPS hotkey {move} because local player could not be resolved.");
            return false;
        }

        if (sessionAtStart.TryGetMove(localPlayer.NetId, out ManualRpsMove currentMove) && currentMove == move)
        {
            RockLog.Trace("Coordinator", $"PublishLocalMove ignored duplicate move={move} for player={localPlayer.NetId}.");
            ManualRpsModalManager.ShowOrRefresh();
            return true;
        }

        uint choiceId = _choiceSynchronizer.ReserveChoiceId(localPlayer);
        _choiceSynchronizer.SyncLocalChoice(localPlayer, choiceId, ManualMoveChoiceCodec.Encode(move));

        // SyncLocalChoice can synchronously bounce back through OnPlayerChoiceReceived,
        // finalize the contested relic, and end the session before this method resumes.
        // If we continue unconditionally, we reopen the modal/backstop after the fight
        // already finished, which is exactly the stale black-screen state we observed.
        if (!ReferenceEquals(_currentSession, sessionAtStart))
        {
            RockLog.Trace(
                "Coordinator",
                $"PublishLocalMove detected session change during SyncLocalChoice for player={localPlayer.NetId} move={move}; skipping post-sync UI refresh.");
            return true;
        }

        RockLog.Info($"Selected {move}.");
        RockLog.Trace("Coordinator", $"PublishLocalMove player={localPlayer.NetId} move={move} choiceId={choiceId}.");
        _localDraftMove = move;
        ManualRpsModalManager.ShowOrRefresh();
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        TryFinalizePendingAward();
        return true;
    }

    public bool ShouldInterceptAutomaticAward(TreasureRoomRelicSynchronizer synchronizer)
    {
        if (_isFinalizingPendingAward)
        {
            return false;
        }

        if (_currentSession == null)
        {
            return false;
        }

        List<Player> contestedPlayers = GetContestedPlayers(synchronizer);
        if (contestedPlayers.Count == 0)
        {
            _pendingAwardSynchronizer = null;
            _pendingFight = null;
            return false;
        }

        if (_pendingFight == null || !_pendingFight.Matches(contestedPlayers, GetContestedRelic(synchronizer, contestedPlayers)))
        {
            _pendingFight = new PendingManualRpsFight(
                contestedPlayers,
                GetContestedRelic(synchronizer, contestedPlayers));
        }

        _pendingAwardSynchronizer = synchronizer;
        RockLog.Info(
            $"Intercepted automatic relic fight for {contestedPlayers.Count} player(s); waiting for manual R/P/S input.");
        RockLog.Trace(
            "Coordinator",
            $"Intercepted contested relic {GetContestedRelic(synchronizer, contestedPlayers)} for players=[{string.Join(",", contestedPlayers.Select(player => player.NetId))}].");
        ManualRpsModalManager.ShowOrRefresh();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        TryFinalizePendingAward();
        return true;
    }

    public bool ShouldDelayEndRelicVoting(TreasureRoomRelicSynchronizer synchronizer)
    {
        if (_isFinalizingPendingAward)
        {
            return false;
        }

        return _pendingAwardSynchronizer != null;
    }

    public bool TryResolveFight(
        IReadOnlyList<Player> players,
        RelicModel relic,
        out RelicPickingResult? result)
    {
        result = null;

        if (_pendingFight == null || !_pendingFight.IsResolved || !_pendingFight.Matches(players, relic))
        {
            return false;
        }

        result = new RelicPickingResult
        {
            type = RelicPickingResultType.FoughtOver,
            player = _pendingFight.Winner!,
            relic = relic,
            fight = _pendingFight.Fight
        };

        RockLog.Info($"Resolved manual relic fight after {_pendingFight.Fight.rounds.Count} round(s).");
        return true;
    }

    private void OnPlayerChoiceReceived(Player player, uint _, NetPlayerChoiceResult result)
    {
        if (_currentSession == null || !ManualMoveChoiceCodec.TryDecode(result, out ManualRpsMove move, out bool isCancel))
        {
            return;
        }

        Player? localPlayer = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());

        if (isCancel)
        {
            _currentSession.RemoveMove(player.NetId);
            if (localPlayer != null && localPlayer.NetId == player.NetId)
            {
                _localDraftMove = null;
            }

            RockLog.Debug($"Received manual RPS cancel from player {player.NetId}.");
            RockLog.Trace("Coordinator", $"OnPlayerChoiceReceived player={player.NetId} cancel=True.");
            ManualRpsModalManager.ShowOrRefresh();
            TreasureRoomRelicUiAccessor.RefreshManualOverlay();
            TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
            return;
        }

        _currentSession.SetMove(player.NetId, move);
        if (localPlayer != null && localPlayer.NetId == player.NetId)
        {
            _localDraftMove = move;
        }

        RockLog.Debug($"Received manual RPS move {move} from player {player.NetId}.");
        RockLog.Trace("Coordinator", $"OnPlayerChoiceReceived player={player.NetId} move={move}.");
        ManualRpsModalManager.ShowOrRefresh();
        TreasureRoomRelicUiAccessor.RefreshManualOverlay();
        TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
        TryFinalizePendingAward();
    }

    public bool TryGetLocalMove(out ManualRpsMove move)
    {
        move = default;

        if (_currentSession == null)
        {
            return false;
        }

        Player? localPlayer = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
        return localPlayer != null && _currentSession.TryGetMove(localPlayer.NetId, out move);
    }

    public bool TryGetMoveForPlayer(ulong playerId, out ManualRpsMove move)
    {
        move = default;
        return _currentSession != null && _currentSession.TryGetMove(playerId, out move);
    }

    private void TryFinalizePendingAward()
    {
        if (_currentSession == null || _pendingAwardSynchronizer == null || _pendingFight == null || _isFinalizingPendingAward)
        {
            RockLog.Trace(
                "Coordinator",
                $"TryFinalizePendingAward skipped session={_currentSession != null} pendingSync={_pendingAwardSynchronizer != null} pendingFight={_pendingFight != null} finalizing={_isFinalizingPendingAward}.");
            return;
        }

        if (!_pendingFight.AreAllActivePlayersReady(playerId => _currentSession.TryGetMove(playerId, out _)))
        {
            RockLog.Trace(
                "Coordinator",
                $"TryFinalizePendingAward waiting for moves from active players [{string.Join(",", _pendingFight.ActivePlayers.Select(player => player.NetId))}].");
            return;
        }

        ManualRoundAdvanceResult roundResult = _pendingFight.AdvanceRound(
            playerId => _currentSession.TryGetMove(playerId, out ManualRpsMove move) ? move : null);

        RockLog.Trace(
            "Coordinator",
            $"AdvanceRound didAdvance={roundResult.DidAdvance} isTie={roundResult.IsTie} isResolved={roundResult.IsResolved} winner={roundResult.Winner?.NetId.ToString() ?? "<none>"} next=[{string.Join(",", roundResult.NextPlayers.Select(player => player.NetId))}] losers=[{string.Join(",", roundResult.LosingPlayers.Select(player => player.NetId))}].");

        if (!roundResult.DidAdvance)
        {
            return;
        }

        if (!roundResult.IsResolved)
        {
            _currentSession.ClearMoves(roundResult.NextPlayers.Select(player => player.NetId));
            _localDraftMove = null;
            AppendRoundHistory(roundResult);
            if (roundResult.Round != null)
            {
                TaskHelper.RunSafely(_roundAnimator.PlayIntermediateRoundAsync(
                    _pendingFight,
                    roundResult.Round,
                    roundResult.LosingPlayers));
            }

            string message = roundResult.IsTie
                ? $"Round {_pendingFight.CompletedRounds} tied. Choose again."
                : $"Round {_pendingFight.CompletedRounds} resolved. Remaining players choose again.";
            RockLog.Info(message);
            ManualRpsModalManager.ShowOrRefresh();
            TreasureRoomRelicUiAccessor.RefreshPlayerHandMarkers();
            TreasureRoomRelicUiAccessor.RefreshRoundHistory();
            return;
        }

        AppendRoundHistory(roundResult);
        TreasureRoomRelicSynchronizer synchronizer = _pendingAwardSynchronizer;
        _isFinalizingPendingAward = true;
        _pendingAwardSynchronizer = null;

        try
        {
            RockLog.Info("Collected all manual R/P/S inputs. Continuing relic award flow.");
            RockLog.Trace("Coordinator", "Invoking original AwardRelics and EndRelicVoting.");
            ManualRpsModalManager.Hide();
            TreasureRoomRelicUiAccessor.RefreshRoundHistory();
            TreasureRoomRelicSynchronizerAccessor.InvokeAwardRelics(synchronizer);
            TreasureRoomRelicSynchronizerAccessor.InvokeEndRelicVoting(synchronizer);
        }
        finally
        {
            _isFinalizingPendingAward = false;
            RockLog.Trace("Coordinator", "Finalizing pending award finished.");
        }
    }

    private static List<Player> GetContestedPlayers(TreasureRoomRelicSynchronizer synchronizer)
    {
        IReadOnlyList<RelicModel>? relics = TreasureRoomRelicSynchronizerAccessor.GetCurrentRelics(synchronizer);
        if (relics == null || relics.Count == 0)
        {
            return new List<Player>();
        }

        IReadOnlyList<int?> votes = TreasureRoomRelicSynchronizerAccessor.GetVotes(synchronizer);
        IReadOnlyList<Player> players = TreasureRoomRelicSynchronizerAccessor.GetPlayers(synchronizer);
        Dictionary<int, List<Player>> picksByIndex = new();

        for (int i = 0; i < votes.Count && i < players.Count; i++)
        {
            int? vote = votes[i];
            if (!vote.HasValue)
            {
                continue;
            }

            if (!picksByIndex.TryGetValue(vote.Value, out List<Player>? pickedPlayers))
            {
                pickedPlayers = new List<Player>();
                picksByIndex[vote.Value] = pickedPlayers;
            }

            pickedPlayers.Add(players[i]);
        }

        return picksByIndex
            .Where(entry => entry.Value.Count > 1)
            .SelectMany(entry => entry.Value)
            .Distinct()
            .ToList();
    }

    private static RelicModel GetContestedRelic(TreasureRoomRelicSynchronizer synchronizer, IReadOnlyList<Player> contestedPlayers)
    {
        IReadOnlyList<RelicModel>? relics = TreasureRoomRelicSynchronizerAccessor.GetCurrentRelics(synchronizer);
        IReadOnlyList<int?> votes = TreasureRoomRelicSynchronizerAccessor.GetVotes(synchronizer);
        IReadOnlyList<Player> players = TreasureRoomRelicSynchronizerAccessor.GetPlayers(synchronizer);
        HashSet<ulong> contestedIds = contestedPlayers.Select(player => player.NetId).ToHashSet();

        for (int i = 0; i < votes.Count && i < players.Count; i++)
        {
            if (contestedIds.Contains(players[i].NetId) && votes[i].HasValue && relics != null)
            {
                return relics[votes[i]!.Value];
            }
        }

        throw new InvalidOperationException("Could not determine contested relic for manual RPS fight.");
    }

    private void AppendRoundHistory(ManualRoundAdvanceResult roundResult)
    {
        if (_pendingFight == null || roundResult.Round == null)
        {
            return;
        }

        ManualRpsRoundHistoryEntry summary = CreateRoundHistoryEntry(_pendingFight, roundResult);
        _roundHistoryEntries.Add(summary);
        _isRoundHistoryContentVisible = roundResult.IsResolved;
        RockLog.Trace(
            "History",
            $"AppendRoundHistory resolved={roundResult.IsResolved} tie={roundResult.IsTie} round={summary.RoundNumber} {DescribeRoundHistoryDebugState()}.");
    }

    public void RevealRoundHistoryAfterAnimation()
    {
        if (_roundHistoryEntries.Count == 0)
        {
            RockLog.Trace("History", "RevealRoundHistoryAfterAnimation skipped because no history lines exist.");
            return;
        }

        _isRoundHistoryContentVisible = true;
        RockLog.Trace("History", $"RevealRoundHistoryAfterAnimation {DescribeRoundHistoryDebugState()}.");
        TreasureRoomRelicUiAccessor.RefreshRoundHistory();
    }

    private static ManualRpsRoundHistoryEntry CreateRoundHistoryEntry(PendingManualRpsFight fight, ManualRoundAdvanceResult roundResult)
    {
        List<ManualRpsRoundHistoryMove> parts = new();
        for (int i = 0; i < fight.Players.Count && i < roundResult.Round!.moves.Count; i++)
        {
            RelicPickingFightMove? move = roundResult.Round.moves[i];
            if (!move.HasValue)
            {
                continue;
            }

            string name = PlayerHandMarkerNameResolver.ResolveMarkerText(fight.Players[i], i);
            parts.Add(new ManualRpsRoundHistoryMove(name, (ManualRpsMove)(int)move.Value));
        }

        if (roundResult.IsTie)
        {
            return new ManualRpsRoundHistoryEntry(fight.CompletedRounds, parts, "Tied");
        }

        if (roundResult.IsResolved && roundResult.Winner != null)
        {
            int winnerIndex = fight.Players
                .Select((player, index) => (player, index))
                .First(tuple => tuple.player.NetId == roundResult.Winner.NetId)
                .index;
            string winner = PlayerHandMarkerNameResolver.ResolveMarkerText(roundResult.Winner, winnerIndex);
            return new ManualRpsRoundHistoryEntry(fight.CompletedRounds, parts, $"Winner: {winner}");
        }

        if (roundResult.LosingPlayers.Count > 0)
        {
            List<string> losers = roundResult.LosingPlayers
                .Select(player =>
                {
                    int loserIndex = fight.Players
                        .Select((entry, index) => (entry, index))
                        .First(tuple => tuple.entry.NetId == player.NetId)
                        .index;
                    return PlayerHandMarkerNameResolver.ResolveMarkerText(player, loserIndex);
                })
                .ToList();
            return new ManualRpsRoundHistoryEntry(fight.CompletedRounds, parts, $"Out: {string.Join(", ", losers)}");
        }

        return new ManualRpsRoundHistoryEntry(fight.CompletedRounds, parts, "Pending");
    }

    private static string FormatRoundSummary(PendingManualRpsFight fight, ManualRoundAdvanceResult roundResult)
    {
        List<string> parts = new();
        for (int i = 0; i < fight.Players.Count && i < roundResult.Round!.moves.Count; i++)
        {
            RelicPickingFightMove? move = roundResult.Round.moves[i];
            if (!move.HasValue)
            {
                continue;
            }

            string name = PlayerHandMarkerNameResolver.ResolveMarkerText(fight.Players[i], i);
            parts.Add($"{name} {move.Value}");
        }

        string prefix = $"R{fight.CompletedRounds}: {string.Join(", ", parts)}";
        if (roundResult.IsTie)
        {
            return $"{prefix} -> 平局";
        }

        if (roundResult.IsResolved && roundResult.Winner != null)
        {
            int winnerIndex = fight.Players
                .Select((player, index) => (player, index))
                .First(tuple => tuple.player.NetId == roundResult.Winner.NetId)
                .index;
            string winner = PlayerHandMarkerNameResolver.ResolveMarkerText(roundResult.Winner, winnerIndex);
            return $"{prefix} -> {winner} 获胜";
        }

        if (roundResult.LosingPlayers.Count > 0)
        {
            List<string> losers = roundResult.LosingPlayers
                .Select(player =>
                {
                    int loserIndex = fight.Players
                        .Select((entry, index) => (entry, index))
                        .First(tuple => tuple.entry.NetId == player.NetId)
                        .index;
                    return PlayerHandMarkerNameResolver.ResolveMarkerText(player, loserIndex);
                })
                .ToList();
            return $"{prefix} -> 淘汰: {string.Join(", ", losers)}";
        }

        return prefix;
    }
}
