namespace ScentMatch.Domain.Constants;

public static class NoteTypes
{
    public const string Top = "Top";
    public const string Middle = "Middle";
    public const string Base = "Base";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Top,
        Middle,
        Base
    };

    public static bool IsValid(string? noteType) =>
        !string.IsNullOrWhiteSpace(noteType) && All.Contains(noteType);
}
