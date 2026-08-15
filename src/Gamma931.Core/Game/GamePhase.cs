namespace Gamma931.Core.Game;

/// <summary>Explicit phase in the round state machine, mirroring RULES.md's Round Structure.
/// The UI drives the game by calling the RoundEngine method appropriate to the current phase.</summary>
public enum GamePhase
{
    NotStarted,
    LocationReveal,
    EquipmentDraw,
    BossReveal,
    CrabActionSetAside,
    MinionReveal,
    CombatCycle,
    EndPhaseCombat,
    EndPhaseCrabAttacks,
    RoundEnd,
    Won,
    Lost,
}
