using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using PureSFTP.Models;

namespace PureSFTP.Services;

public sealed class ThemeService : IThemeService
{
    private const string DefaultCustomGradientStartColor = "#0F172A";
    private const string DefaultCustomGradientEndColor = "#2563EB";

    public ThemeService()
    {
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged += (_, _) =>
            {
                if (CurrentMode == AppThemeMode.System)
                {
                    ApplyPalette();
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            };
        }
    }

    public AppThemeMode CurrentMode { get; private set; } = AppThemeMode.System;

    public string CustomGradientStartColor { get; private set; } = DefaultCustomGradientStartColor;

    public string CustomGradientEndColor { get; private set; } = DefaultCustomGradientEndColor;

    public event EventHandler? ThemeChanged;

    public void SetTheme(AppThemeMode mode)
    {
        CurrentMode = mode;

        if (Application.Current is not { } application)
        {
            return;
        }

        application.RequestedThemeVariant = mode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            AppThemeMode.Custom => IsCustomGradientDark() ? ThemeVariant.Dark : ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };

        ApplyPalette();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetCustomGradient(string startColor, string endColor)
    {
        if (TryNormalizeColor(startColor, out var normalizedStartColor))
        {
            CustomGradientStartColor = normalizedStartColor;
        }

        if (TryNormalizeColor(endColor, out var normalizedEndColor))
        {
            CustomGradientEndColor = normalizedEndColor;
        }

        if (CurrentMode == AppThemeMode.Custom)
        {
            SetTheme(AppThemeMode.Custom);
        }
    }

    private void ApplyPalette()
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        var isDark = IsDark(application);
        var palette = isDark ? CreateDarkPalette() : CreateLightPalette();
        if (CurrentMode == AppThemeMode.Custom)
        {
            palette = CreateCustomPalette(palette, isDark);
            SetGradientBrush(application, "AppBackgroundBrush", CustomGradientStartColor, CustomGradientEndColor);
        }
        else
        {
            SetBrush(application, "AppBackgroundBrush", palette.AppBackground);
        }

        SetBrush(application, "CardBackgroundBrush", palette.CardBackground);
        SetBrush(application, "SidebarBackgroundBrush", palette.SidebarBackground);
        SetBrush(application, "PanelBackgroundBrush", palette.PanelBackground);
        SetBrush(application, "InputBackgroundBrush", palette.InputBackground);
        SetBrush(application, "TextPrimaryBrush", palette.TextPrimary);
        SetBrush(application, "TextSecondaryBrush", palette.TextSecondary);
        SetBrush(application, "TextMutedBrush", palette.TextMuted);
        SetBrush(application, "TextOnAccentBrush", palette.TextOnAccent);
        SetBrush(application, "BorderBrush", palette.Border);
        SetBrush(application, "StrongBorderBrush", palette.StrongBorder);
        SetBrush(application, "AccentBrush", palette.Accent);
        SetBrush(application, "AccentHoverBrush", palette.AccentHover);
        SetBrush(application, "AccentPressedBrush", palette.AccentPressed);
        SetBrush(application, "DisabledBackgroundBrush", palette.DisabledBackground);
        SetBrush(application, "DisabledTextBrush", palette.DisabledText);
        SetBrush(application, "HoverBackgroundBrush", palette.HoverBackground);
        SetBrush(application, "SelectedBackgroundBrush", palette.SelectedBackground);
        SetBrush(application, "MenuBackgroundBrush", palette.MenuBackground);
        SetBrush(application, "DividerBrush", palette.Divider);
        SetBrush(application, "ToastBackgroundBrush", palette.ToastBackground);
        SetBrush(application, "ToastButtonBrush", palette.ToastButton);
        SetBrush(application, "ToastButtonHoverBrush", palette.ToastButtonHover);
        SetBrush(application, "ToastBorderBrush", palette.ToastBorder);
        SetBrush(application, "HeaderBackgroundBrush", palette.HeaderBackground);
        SetBrush(application, "RowBorderBrush", palette.RowBorder);
    }

    private bool IsDark(Application application)
    {
        return CurrentMode == AppThemeMode.Custom && IsCustomGradientDark() ||
            application.RequestedThemeVariant == ThemeVariant.Dark ||
            application.RequestedThemeVariant == ThemeVariant.Default &&
            application.ActualThemeVariant == ThemeVariant.Dark;
    }

    private static void SetBrush(Application application, string key, Color color)
    {
        application.Resources[key] = new SolidColorBrush(color);
    }

    private static void SetGradientBrush(Application application, string key, string startColor, string endColor)
    {
        var start = ParseColorOrFallback(startColor, Color.Parse(DefaultCustomGradientStartColor));
        var end = ParseColorOrFallback(endColor, Color.Parse(DefaultCustomGradientEndColor));
        application.Resources[key] = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(start, 0),
                new GradientStop(end, 1),
            },
        };
    }

    private ThemePalette CreateCustomPalette(ThemePalette basePalette, bool isDark)
    {
        var accent = ParseColorOrFallback(CustomGradientEndColor, Color.Parse(DefaultCustomGradientEndColor));
        return new ThemePalette
        {
            AppBackground = basePalette.AppBackground,
            CardBackground = basePalette.CardBackground,
            SidebarBackground = basePalette.SidebarBackground,
            PanelBackground = basePalette.PanelBackground,
            InputBackground = basePalette.InputBackground,
            TextPrimary = basePalette.TextPrimary,
            TextSecondary = basePalette.TextSecondary,
            TextMuted = basePalette.TextMuted,
            TextOnAccent = Colors.White,
            Border = basePalette.Border,
            StrongBorder = basePalette.StrongBorder,
            Accent = accent,
            AccentHover = AdjustBrightness(accent, 0.12),
            AccentPressed = AdjustBrightness(accent, -0.16),
            DisabledBackground = basePalette.DisabledBackground,
            DisabledText = basePalette.DisabledText,
            HoverBackground = basePalette.HoverBackground,
            SelectedBackground = WithAlpha(accent, isDark ? 0.26 : 0.16),
            MenuBackground = basePalette.MenuBackground,
            Divider = basePalette.Divider,
            ToastBackground = basePalette.ToastBackground,
            ToastButton = basePalette.ToastButton,
            ToastButtonHover = basePalette.ToastButtonHover,
            ToastBorder = basePalette.ToastBorder,
            HeaderBackground = basePalette.HeaderBackground,
            RowBorder = basePalette.RowBorder,
        };
    }

    private bool IsCustomGradientDark()
    {
        var start = ParseColorOrFallback(CustomGradientStartColor, Color.Parse(DefaultCustomGradientStartColor));
        var end = ParseColorOrFallback(CustomGradientEndColor, Color.Parse(DefaultCustomGradientEndColor));
        return (GetLuminance(start) + GetLuminance(end)) / 2 < 0.48;
    }

    private static bool TryNormalizeColor(string value, out string normalizedColor)
    {
        normalizedColor = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();
        if (!trimmedValue.StartsWith('#'))
        {
            trimmedValue = $"#{trimmedValue}";
        }

        try
        {
            var color = Color.Parse(trimmedValue);
            normalizedColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Color ParseColorOrFallback(string value, Color fallback)
    {
        return TryNormalizeColor(value, out var normalizedColor)
            ? Color.Parse(normalizedColor)
            : fallback;
    }

    private static Color AdjustBrightness(Color color, double amount)
    {
        return Color.FromRgb(
            ClampColor(color.R + 255 * amount),
            ClampColor(color.G + 255 * amount),
            ClampColor(color.B + 255 * amount));
    }

    private static Color WithAlpha(Color color, double alpha)
    {
        return Color.FromArgb(ClampColor(255 * alpha), color.R, color.G, color.B);
    }

    private static byte ClampColor(double value)
    {
        return (byte)Math.Clamp(Math.Round(value), 0, 255);
    }

    private static double GetLuminance(Color color)
    {
        return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;
    }

    private static ThemePalette CreateLightPalette()
    {
        return new ThemePalette
        {
            AppBackground = Color.Parse("#EEF3F8"),
            CardBackground = Color.Parse("#FCFDFE"),
            SidebarBackground = Color.Parse("#F7FAFD"),
            PanelBackground = Color.Parse("#F4F8FB"),
            InputBackground = Color.Parse("#F6F9FC"),
            TextPrimary = Color.Parse("#102236"),
            TextSecondary = Color.Parse("#5E7388"),
            TextMuted = Color.Parse("#6A7F93"),
            TextOnAccent = Colors.White,
            Border = Color.Parse("#D7E1EB"),
            StrongBorder = Color.Parse("#C9D6E2"),
            Accent = Color.Parse("#102236"),
            AccentHover = Color.Parse("#18314A"),
            AccentPressed = Color.Parse("#0B1826"),
            DisabledBackground = Color.Parse("#D7E1EB"),
            DisabledText = Color.Parse("#71869A"),
            HoverBackground = Color.Parse("#EEF4FA"),
            SelectedBackground = Color.Parse("#DCE8F3"),
            MenuBackground = Color.Parse("#FCFDFE"),
            Divider = Color.Parse("#E6EDF4"),
            ToastBackground = Color.Parse("#102236"),
            ToastButton = Color.Parse("#203953"),
            ToastButtonHover = Color.Parse("#294866"),
            ToastBorder = Color.Parse("#31506E"),
            HeaderBackground = Color.Parse("#F4F7FA"),
            RowBorder = Color.Parse("#E4EAF0"),
        };
    }

    private static ThemePalette CreateDarkPalette()
    {
        return new ThemePalette
        {
            AppBackground = Color.Parse("#0F1722"),
            CardBackground = Color.Parse("#162232"),
            SidebarBackground = Color.Parse("#121D2A"),
            PanelBackground = Color.Parse("#1C2A3B"),
            InputBackground = Color.Parse("#101A26"),
            TextPrimary = Color.Parse("#EAF1F8"),
            TextSecondary = Color.Parse("#A7B6C8"),
            TextMuted = Color.Parse("#8292A6"),
            TextOnAccent = Colors.White,
            Border = Color.Parse("#2E4054"),
            StrongBorder = Color.Parse("#3A4F65"),
            Accent = Color.Parse("#2F80ED"),
            AccentHover = Color.Parse("#3D8DF4"),
            AccentPressed = Color.Parse("#1F67C2"),
            DisabledBackground = Color.Parse("#263544"),
            DisabledText = Color.Parse("#718095"),
            HoverBackground = Color.Parse("#203047"),
            SelectedBackground = Color.Parse("#28405C"),
            MenuBackground = Color.Parse("#162232"),
            Divider = Color.Parse("#26384A"),
            ToastBackground = Color.Parse("#1B2B3D"),
            ToastButton = Color.Parse("#2C4562"),
            ToastButtonHover = Color.Parse("#365577"),
            ToastBorder = Color.Parse("#43627F"),
            HeaderBackground = Color.Parse("#172638"),
            RowBorder = Color.Parse("#26384A"),
        };
    }
}
