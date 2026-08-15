using Gamma931.Core.Models;

namespace Gamma931.App.ViewModels;

/// <summary>One row of the pre-game setup form: a player's name and chosen character.</summary>
public sealed class PlayerSetupRow : ObservableObject
{
    private string _name;
    private CharacterCard? _character;

    public PlayerSetupRow(string name, CharacterCard? character)
    {
        _name = name;
        _character = character;
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public CharacterCard? Character
    {
        get => _character;
        set => Set(ref _character, value);
    }
}
