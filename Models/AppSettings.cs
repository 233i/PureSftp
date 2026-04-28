namespace PureSFTP.Models;

public sealed class AppSettings
{
    public AppLanguage Language { get; init; } = AppLanguage.English;

    public AppThemeMode ThemeMode { get; init; } = AppThemeMode.System;

    public string CustomGradientStartColor { get; init; } = "#0F172A";

    public string CustomGradientEndColor { get; init; } = "#2563EB";
}
