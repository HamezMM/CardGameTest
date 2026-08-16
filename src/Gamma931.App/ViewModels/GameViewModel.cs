using System.Collections.ObjectModel;
using System.Windows.Input;
using Gamma931.Core.Data;
using Gamma931.Core.Game;
using Gamma931.Core.Models;

namespace Gamma931.App.ViewModels;

/// <summary>
/// Drives one game via <see cref="RoundEngine"/> for the Play tab. Every command wraps its engine
/// call in a try/catch and surfaces failures through <see cref="StatusMessage"/> instead of
/// crashing — the engine throws on out-of-phase calls, which is expected while the UI is only
/// showing the buttons valid for the current phase (belt-and-braces).
/// </summary>
public sealed class GameViewModel : ObservableObject
{
    private readonly CardDatabase _db;
    private readonly RoundEngine _engine = new();
    private GameState? _state;
    private string _statusMessage = string.Empty;
    private DifficultyLevel _selectedDifficulty = DifficultyLevel.Normal;

    public GameViewModel(CardDatabase db)
    {
        _db = db;
        SetupRows = new ObservableCollection<PlayerSetupRow>();
        SetPlayerCount(4);

        StartGameCommand = new RelayCommand(StartGame, () => IsSetupPhase);
        NewGameCommand = new RelayCommand(() => { _state = null; RefreshAll(); });

        RevealLocationCommand = new RelayCommand(() => Run(() => _engine.RevealNextLocation(State)),
            () => _state?.Phase == GamePhase.LocationReveal);
        DrawEquipmentCommand = new RelayCommand(() => Run(() => _engine.DrawEquipmentForAllPlayers(State)),
            () => _state?.Phase == GamePhase.EquipmentDraw);
        DrawBossCommand = new RelayCommand(() => Run(() => _engine.DrawBoss(State)),
            () => _state?.Phase == GamePhase.BossReveal);
        SetAsideCrabActionsCommand = new RelayCommand(() => Run(() => _engine.SetAsideCrabActions(State)),
            () => _state?.Phase == GamePhase.CrabActionSetAside);
        DrawMinionsCommand = new RelayCommand(() => Run(() => _engine.DrawMinions(State)),
            () => _state?.Phase == GamePhase.MinionReveal);
        DrawCrabActionCommand = new RelayCommand(() => Run(() => _engine.DrawCrabAction(State)),
            () => IsCombatCyclePhase && _state!.PendingEquipmentTurns.Count == 0);
        AdvanceRoundCommand = new RelayCommand(() => Run(() => _engine.AdvanceToNextRound(State)),
            () => _state?.Phase == GamePhase.RoundEnd);

        TakeCrabHitCommand = new RelayCommand(() => Run(() => _engine.ResolveCrabAction(State)),
            () => HasPendingCrabAction);
        BlockCrabActionCommand = new RelayCommand(p => Run(() => _engine.ResolveCrabAction(State, (EquipmentCard)p!)),
            () => HasPendingCrabAction);

        HitBossCommand = new RelayCommand(p => Run(() => _engine.PlayEquipmentFromHand(State, (EquipmentCard)p!, CombatTarget.Boss)),
            () => IsCombatCyclePhase && _state!.PendingEquipmentTurns.Count > 0);
        HitMinionsCommand = new RelayCommand(p => Run(() => _engine.PlayEquipmentFromHand(State, (EquipmentCard)p!, CombatTarget.Minions)),
            () => IsCombatCyclePhase && _state!.PendingEquipmentTurns.Count > 0);
        HealSelfCommand = new RelayCommand(p => Run(() => _engine.PlayEquipmentFromHand(State, (EquipmentCard)p!, healTarget: CurrentTurnPlayer)),
            () => IsCombatCyclePhase && _state!.PendingEquipmentTurns.Count > 0);
        HealTeammateCommand = new RelayCommand(p =>
        {
            var args = (object[])p!;
            Run(() => _engine.PlayEquipmentFromHand(State, (EquipmentCard)args[0], healTarget: (Player)args[1]));
        }, () => IsCombatCyclePhase && _state!.PendingEquipmentTurns.Count > 0);

        EndPhaseDrawCommand = new RelayCommand(() => Run(() =>
            _engine.EndPhaseDrawAndResolve(State, CombatTarget.Boss, State.PendingEquipmentTurns.Count > 0 ? State.PendingEquipmentTurns.Peek() : null)),
            () => IsEndPhaseDraw && _state!.PendingEquipmentTurns.Count > 0);
        EndPhaseAttackMeleeBossCommand = new RelayCommand(() => Run(() => _engine.EndPhaseAttackWithDefaultMeleeWeapon(State, CombatTarget.Boss)),
            () => IsEndPhaseDraw && _state!.PendingEquipmentTurns.Count > 0);
        EndPhaseAttackMeleeMinionsCommand = new RelayCommand(() => Run(() => _engine.EndPhaseAttackWithDefaultMeleeWeapon(State, CombatTarget.Minions)),
            () => IsEndPhaseDraw && _state!.PendingEquipmentTurns.Count > 0);

        CrabAttackTakeHitCommand = new RelayCommand(() => Run(() => _engine.ResolveNextCrabAttack(State)),
            () => IsEndPhaseCrabAttacks && _state!.PendingCrabAttacks.Count > 0);
        CrabAttackBlockCommand = new RelayCommand(p => Run(() => _engine.ResolveNextCrabAttack(State, (EquipmentCard)p!)),
            () => IsEndPhaseCrabAttacks && _state!.PendingCrabAttacks.Count > 0);
    }

    // ---- setup ----

    public ObservableCollection<PlayerSetupRow> SetupRows { get; }
    public IReadOnlyList<CharacterCard> AvailableCharacters => _db.Characters;
    public IReadOnlyList<DifficultyLevel> DifficultyOptions { get; } = Enum.GetValues<DifficultyLevel>();
    public IReadOnlyList<int> PlayerCountOptions { get; } = new[] { 2, 3, 4, 5 };

    public DifficultyLevel SelectedDifficulty
    {
        get => _selectedDifficulty;
        set => Set(ref _selectedDifficulty, value);
    }

    public int PlayerCount
    {
        get => SetupRows.Count;
        set => SetPlayerCount(Math.Clamp(value, 2, 5));
    }

    private void SetPlayerCount(int count)
    {
        while (SetupRows.Count < count)
        {
            var index = SetupRows.Count + 1;
            var character = _db.Characters.Count > 0 ? _db.Characters[(index - 1) % _db.Characters.Count] : null;
            SetupRows.Add(new PlayerSetupRow($"Player {index}", character));
        }

        while (SetupRows.Count > count)
        {
            SetupRows.RemoveAt(SetupRows.Count - 1);
        }

        OnPropertyChanged(nameof(PlayerCount));
    }

    // ---- state ----

    public bool IsSetupPhase => _state is null;
    public bool IsGameActive => _state is not null;

    private GameState State => _state ?? throw new InvalidOperationException("No game in progress.");

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public string PhaseText => _state?.Phase.ToString() ?? "Not started";
    public string RoundText => _state is null ? string.Empty : $"Round {_state.RoundNumber}";
    public string LocationText => _state?.CurrentLocation is { } loc ? $"{loc.Name} ({loc.Biome})" : "—";
    public string BossText => _state?.CurrentBoss is { } boss
        ? $"{boss.Name} — {_state.CurrentBossHp}/{boss.HpFor(_state.Players.Count)} HP " +
          $"(+{boss.MinionCountFor(_state.Players.Count)} minion(s) @ {_state.Players.Count}p)"
        : "—";
    public string CrabActionText => _state?.CurrentCrabAction is { } action ? $"{action.Name}: {action.EffectText}" : "—";

    /// <summary>Current location/boss card objects, exposed alongside the *Text summaries above so CardFaceView can look up their art.</summary>
    public LocationCard? CurrentLocationCard => _state?.CurrentLocation;
    public CrabBossCard? CurrentBossCard => _state?.CurrentBoss;

    public string PendingAttackText => _state?.PendingCrabActionTarget is { } target
        ? $"The crab action is attacking {target.Name} — block or take the hit."
        : string.Empty;

    public bool HasPendingCrabAction => _state?.PendingCrabActionTarget is not null;

    public ObservableCollection<CrabMinionCard> MinionsDisplay { get; } = new();
    public ObservableCollection<PlayerDisplay> PlayersDisplay { get; } = new();
    public ObservableCollection<EquipmentCard> CurrentHand { get; } = new();
    public ObservableCollection<EquipmentCard> BlockableCards { get; } = new();
    public ObservableCollection<string> LogLines { get; } = new();

    /// <summary>Alive teammates (excluding the current turn player) a Healing card can be
    /// redirected to. Only populated when a Medic is alive in the crew — RoundEngine.ResolveHeal
    /// rejects a non-self heal target otherwise, matching the Medic's passive.</summary>
    public ObservableCollection<Player> HealableTeammates { get; } = new();

    public Player? CurrentTurnPlayer => _state is { } s && s.PendingEquipmentTurns.Count > 0 ? s.PendingEquipmentTurns.Peek() : null;
    public string CurrentTurnText => CurrentTurnPlayer is { } p ? $"{p.Name}'s turn ({p.Character.Name})" : "—";

    public bool IsCombatCyclePhase => _state?.Phase == GamePhase.CombatCycle && !HasPendingCrabAction;
    public bool IsEndPhaseDraw => _state?.Phase == GamePhase.EndPhaseCombat;
    public bool IsEndPhaseCrabAttacks => _state?.Phase == GamePhase.EndPhaseCrabAttacks;

    // ---- commands ----

    public RelayCommand StartGameCommand { get; }
    public RelayCommand NewGameCommand { get; }
    public RelayCommand RevealLocationCommand { get; }
    public RelayCommand DrawEquipmentCommand { get; }
    public RelayCommand DrawBossCommand { get; }
    public RelayCommand SetAsideCrabActionsCommand { get; }
    public RelayCommand DrawMinionsCommand { get; }
    public RelayCommand DrawCrabActionCommand { get; }
    public RelayCommand AdvanceRoundCommand { get; }
    public RelayCommand TakeCrabHitCommand { get; }
    public RelayCommand BlockCrabActionCommand { get; }
    public RelayCommand HitBossCommand { get; }
    public RelayCommand HitMinionsCommand { get; }
    public RelayCommand HealSelfCommand { get; }
    public RelayCommand HealTeammateCommand { get; }
    public RelayCommand EndPhaseDrawCommand { get; }
    public RelayCommand EndPhaseAttackMeleeBossCommand { get; }
    public RelayCommand EndPhaseAttackMeleeMinionsCommand { get; }
    public RelayCommand CrabAttackTakeHitCommand { get; }
    public RelayCommand CrabAttackBlockCommand { get; }

    private void StartGame()
    {
        if (SetupRows.Any(r => string.IsNullOrWhiteSpace(r.Name)))
        {
            StatusMessage = "Every player needs a name.";
            return;
        }

        if (SetupRows.Any(r => r.Character is null))
        {
            StatusMessage = "Every player needs a character.";
            return;
        }

        var setup = SetupRows.Select(r => (r.Name, r.Character!)).ToList();
        Run(() => _state = _engine.StartNewGame(_db, setup, SelectedDifficulty));
    }

    private void Run(Action action)
    {
        try
        {
            action();
            StatusMessage = string.Empty;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (ArgumentException ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            RefreshAll();
        }
    }

    private void RefreshAll()
    {
        OnPropertyChanged(nameof(IsSetupPhase));
        OnPropertyChanged(nameof(IsGameActive));
        OnPropertyChanged(nameof(PhaseText));
        OnPropertyChanged(nameof(RoundText));
        OnPropertyChanged(nameof(LocationText));
        OnPropertyChanged(nameof(BossText));
        OnPropertyChanged(nameof(CurrentLocationCard));
        OnPropertyChanged(nameof(CurrentBossCard));
        OnPropertyChanged(nameof(CrabActionText));
        OnPropertyChanged(nameof(PendingAttackText));
        OnPropertyChanged(nameof(HasPendingCrabAction));
        OnPropertyChanged(nameof(CurrentTurnPlayer));
        OnPropertyChanged(nameof(CurrentTurnText));
        OnPropertyChanged(nameof(IsCombatCyclePhase));
        OnPropertyChanged(nameof(IsEndPhaseDraw));
        OnPropertyChanged(nameof(IsEndPhaseCrabAttacks));

        MinionsDisplay.Clear();
        PlayersDisplay.Clear();
        CurrentHand.Clear();
        BlockableCards.Clear();
        HealableTeammates.Clear();
        LogLines.Clear();

        if (_state is null)
        {
            CommandManager.InvalidateRequerySuggested();
            return;
        }

        foreach (var minion in _state.CurrentMinions)
        {
            MinionsDisplay.Add(minion);
        }

        var upNext = CurrentTurnPlayer;
        foreach (var player in _state.Players)
        {
            PlayersDisplay.Add(new PlayerDisplay(
                player.Name,
                player.Character.Name,
                player.CurrentHp,
                Player.MaxHp,
                player.Position.ToString(),
                player.Hand.Count,
                player.IsAlive,
                ReferenceEquals(player, upNext)));
        }

        var medicAlive = _state.Players.Any(p => p.IsAlive && string.Equals(p.Character.Name, "Medic", StringComparison.OrdinalIgnoreCase));
        if (upNext is not null && medicAlive)
        {
            foreach (var teammate in _state.Players.Where(p => p.IsAlive && !ReferenceEquals(p, upNext)))
            {
                HealableTeammates.Add(teammate);
            }
        }

        if (upNext is not null && _state.Phase == GamePhase.CombatCycle)
        {
            foreach (var card in upNext.Hand)
            {
                CurrentHand.Add(card);
            }
        }

        if (_state.PendingCrabActionTarget is { } blockTarget)
        {
            foreach (var card in blockTarget.Hand.Where(c => c.EquipmentType == EquipmentType.Protection))
            {
                BlockableCards.Add(card);
            }
        }
        else if (_state.Phase == GamePhase.EndPhaseCrabAttacks && _state.PendingCrabAttacks.Count > 0)
        {
            var previewTarget = _engine.PeekNextCrabAttackTarget(_state);
            foreach (var card in previewTarget.Hand.Where(c => c.EquipmentType == EquipmentType.Protection))
            {
                BlockableCards.Add(card);
            }
        }

        foreach (var entry in _state.Log.AsEnumerable().Reverse().Take(200))
        {
            LogLines.Add(entry.Message);
        }

        CommandManager.InvalidateRequerySuggested();
    }
}
