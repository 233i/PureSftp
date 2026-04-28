namespace PureSFTP.Models;

public sealed class ThemeOption
{
    public ThemeOption(AppThemeMode mode, string displayName)
    {
        Mode = mode;
        DisplayName = displayName;
    }

    public AppThemeMode Mode { get; }

    public string DisplayName { get; }
}
