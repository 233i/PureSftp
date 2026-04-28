namespace PureSFTP.Models;

public sealed class AppSettings
{
    public AppLanguage Language { get; init; } = AppLanguage.English;

    public AppThemeMode ThemeMode { get; init; } = AppThemeMode.System;
}
