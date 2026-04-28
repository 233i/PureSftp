using System;
using PureSFTP.Models;

namespace PureSFTP.Services;

public interface IThemeService
{
    AppThemeMode CurrentMode { get; }

    string CustomGradientStartColor { get; }

    string CustomGradientEndColor { get; }

    event EventHandler? ThemeChanged;

    void SetTheme(AppThemeMode mode);

    void SetCustomGradient(string startColor, string endColor);
}
