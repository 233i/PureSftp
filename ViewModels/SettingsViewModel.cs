using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using PureSFTP.Models;
using PureSFTP.Services;

namespace PureSFTP.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ILocalizationService _localizationService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeService _themeService;
    private AppSettings _settings;
    private LanguageOption? _selectedLanguageOption;
    private ThemeOption? _selectedThemeOption;
    private string _customGradientStartColor = "#0F172A";
    private string _customGradientEndColor = "#2563EB";

    public SettingsViewModel(
        ILocalizationService localizationService,
        IAppSettingsService appSettingsService,
        IThemeService themeService)
    {
        _localizationService = localizationService;
        _appSettingsService = appSettingsService;
        _themeService = themeService;
        _settings = _appSettingsService.Load();
        _customGradientStartColor = _settings.CustomGradientStartColor;
        _customGradientEndColor = _settings.CustomGradientEndColor;

        LanguageOptions =
        [
            new LanguageOption(AppLanguage.English, "English"),
            new LanguageOption(AppLanguage.ChineseSimplified, "简体中文"),
        ];

        ThemeOptions = [];
        RefreshThemeOptions();
        _selectedLanguageOption = FindOption(_localizationService.CurrentLanguage);
        _selectedThemeOption = FindThemeOption(_themeService.CurrentMode);
        _localizationService.LanguageChanged += (_, _) =>
        {
            SyncSelectedLanguage();
            RefreshThemeOptions();
        };
    }

    public ObservableCollection<LanguageOption> LanguageOptions { get; }

    public ObservableCollection<ThemeOption> ThemeOptions { get; }

    public LanguageOption? SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (SetProperty(ref _selectedLanguageOption, value) && value is not null)
            {
                _localizationService.SetLanguage(value.Language);
                _settings = new AppSettings
                {
                    Language = value.Language,
                    ThemeMode = _settings.ThemeMode,
                    CustomGradientStartColor = _settings.CustomGradientStartColor,
                    CustomGradientEndColor = _settings.CustomGradientEndColor,
                };
                _appSettingsService.Save(_settings);

                OnPropertyChanged(nameof(SelectedLanguageDisplayName));
            }
        }
    }

    public ThemeOption? SelectedThemeOption
    {
        get => _selectedThemeOption;
        set
        {
            if (SetProperty(ref _selectedThemeOption, value) && value is not null)
            {
                if (value.Mode == AppThemeMode.Custom)
                {
                    _themeService.SetCustomGradient(CustomGradientStartColor, CustomGradientEndColor);
                }

                _themeService.SetTheme(value.Mode);
                _settings = new AppSettings
                {
                    Language = _settings.Language,
                    ThemeMode = value.Mode,
                    CustomGradientStartColor = CustomGradientStartColor,
                    CustomGradientEndColor = CustomGradientEndColor,
                };
                _appSettingsService.Save(_settings);

                OnPropertyChanged(nameof(SelectedThemeDisplayName));
                OnPropertyChanged(nameof(IsCustomThemeSelected));
            }
        }
    }

    public string CustomGradientStartColor
    {
        get => _customGradientStartColor;
        set
        {
            if (SetProperty(ref _customGradientStartColor, value))
            {
                SaveCustomGradientIfValid();
            }
        }
    }

    public string CustomGradientEndColor
    {
        get => _customGradientEndColor;
        set
        {
            if (SetProperty(ref _customGradientEndColor, value))
            {
                SaveCustomGradientIfValid();
            }
        }
    }

    public Color CustomGradientStartColorValue
    {
        get => ParseHexColorOrFallback(CustomGradientStartColor);
        set => ApplyCustomGradientStartColor(value);
    }

    public Color CustomGradientEndColorValue
    {
        get => ParseHexColorOrFallback(CustomGradientEndColor);
        set => ApplyCustomGradientEndColor(value);
    }

    public bool IsCustomThemeSelected => SelectedThemeOption?.Mode == AppThemeMode.Custom;

    public bool IsCustomGradientValid =>
        TryNormalizeHexColor(CustomGradientStartColor, out _) &&
        TryNormalizeHexColor(CustomGradientEndColor, out _);

    public string SelectedLanguageDisplayName => SelectedLanguageOption?.DisplayName ?? string.Empty;

    public string SelectedThemeDisplayName => SelectedThemeOption?.DisplayName ?? string.Empty;

    private LanguageOption FindOption(AppLanguage language)
    {
        foreach (var option in LanguageOptions)
        {
            if (option.Language == language)
            {
                return option;
            }
        }

        return LanguageOptions[0];
    }

    private void SyncSelectedLanguage()
    {
        var targetOption = FindOption(_localizationService.CurrentLanguage);
        if (_selectedLanguageOption == targetOption)
        {
            return;
        }

        _selectedLanguageOption = targetOption;
        OnPropertyChanged(nameof(SelectedLanguageOption));
        OnPropertyChanged(nameof(SelectedLanguageDisplayName));
    }

    private void RefreshThemeOptions()
    {
        var currentMode = SelectedThemeOption?.Mode ?? _themeService.CurrentMode;
        ThemeOptions.Clear();
        ThemeOptions.Add(new ThemeOption(AppThemeMode.System, _localizationService.Get("ThemeSystemOption")));
        ThemeOptions.Add(new ThemeOption(AppThemeMode.Light, _localizationService.Get("ThemeLightOption")));
        ThemeOptions.Add(new ThemeOption(AppThemeMode.Dark, _localizationService.Get("ThemeDarkOption")));
        ThemeOptions.Add(new ThemeOption(AppThemeMode.Custom, _localizationService.Get("ThemeCustomOption")));
        _selectedThemeOption = FindThemeOption(currentMode);
        OnPropertyChanged(nameof(SelectedThemeOption));
        OnPropertyChanged(nameof(SelectedThemeDisplayName));
        OnPropertyChanged(nameof(IsCustomThemeSelected));
    }

    private ThemeOption FindThemeOption(AppThemeMode mode)
    {
        foreach (var option in ThemeOptions)
        {
            if (option.Mode == mode)
            {
                return option;
            }
        }

        return ThemeOptions[0];
    }

    private void SaveCustomGradientIfValid()
    {
        OnPropertyChanged(nameof(IsCustomGradientValid));

        if (!TryNormalizeHexColor(CustomGradientStartColor, out var normalizedStartColor) ||
            !TryNormalizeHexColor(CustomGradientEndColor, out var normalizedEndColor))
        {
            return;
        }

        _customGradientStartColor = normalizedStartColor;
        _customGradientEndColor = normalizedEndColor;
        OnPropertyChanged(nameof(CustomGradientStartColor));
        OnPropertyChanged(nameof(CustomGradientEndColor));
        OnPropertyChanged(nameof(CustomGradientStartColorValue));
        OnPropertyChanged(nameof(CustomGradientEndColorValue));

        _themeService.SetCustomGradient(normalizedStartColor, normalizedEndColor);
        _settings = new AppSettings
        {
            Language = _settings.Language,
            ThemeMode = _settings.ThemeMode,
            CustomGradientStartColor = normalizedStartColor,
            CustomGradientEndColor = normalizedEndColor,
        };
        _appSettingsService.Save(_settings);
    }

    private void ApplyCustomGradientStartColor(Color color)
    {
        var normalizedColor = ToHexColor(color);
        if (_customGradientStartColor == normalizedColor)
        {
            return;
        }

        _customGradientStartColor = normalizedColor;
        OnPropertyChanged(nameof(CustomGradientStartColor));
        OnPropertyChanged(nameof(CustomGradientStartColorValue));
        SaveCustomGradientIfValid();
    }

    private void ApplyCustomGradientEndColor(Color color)
    {
        var normalizedColor = ToHexColor(color);
        if (_customGradientEndColor == normalizedColor)
        {
            return;
        }

        _customGradientEndColor = normalizedColor;
        OnPropertyChanged(nameof(CustomGradientEndColor));
        OnPropertyChanged(nameof(CustomGradientEndColorValue));
        SaveCustomGradientIfValid();
    }

    private static bool TryNormalizeHexColor(string value, out string normalizedColor)
    {
        normalizedColor = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var colorText = value.Trim().TrimStart('#');
        if (colorText.Length != 6 || !colorText.All(Uri.IsHexDigit))
        {
            return false;
        }

        normalizedColor = $"#{colorText.ToUpperInvariant()}";
        return true;
    }

    private static Color ParseHexColorOrFallback(string value)
    {
        return TryNormalizeHexColor(value, out var normalizedColor)
            ? Color.Parse(normalizedColor)
            : Color.Parse("#2563EB");
    }

    private static string ToHexColor(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
