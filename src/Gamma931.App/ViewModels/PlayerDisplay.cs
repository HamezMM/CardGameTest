namespace Gamma931.App.ViewModels;

/// <summary>Read-only snapshot of a Player for display; rebuilt after every engine step.</summary>
public sealed record PlayerDisplay(
    string Name,
    string CharacterName,
    int CurrentHp,
    int MaxHp,
    string Position,
    int HandCount,
    bool IsAlive,
    bool IsUpNext);
