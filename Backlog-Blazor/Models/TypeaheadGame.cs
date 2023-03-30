using BacklogBlazor_Shared.Models;

namespace BacklogBlazor.Models;

// This model has to exist to allow Blazored Typeahead to work politely
public class TypeaheadGame
{
    public Game Game { get; set; } = new();

    public bool DisableTypeahead { get; set; } = true;
}