using System.Collections.ObjectModel;
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

    public SettingsViewModel(
        ILocalizationService localizationService,
        IAppSettingsService appSettingsService,
        IThemeService themeService)
    {
        _localizationService = localizationService;
        _appSettingsService = appSettingsService;
        _themeService = themeService;
        _settings = _appSettingsService.Load();

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
                _themeService.SetTheme(value.Mode);
                _settings = new AppSettings
                {
                    Language = _settings.Language,
                    ThemeMode = value.Mode,
                };
                _appSettingsService.Save(_settings);

                OnPropertyChanged(nameof(SelectedThemeDisplayName));
            }
        }
    }

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
        _selectedThemeOption = FindThemeOption(currentMode);
        OnPropertyChanged(nameof(SelectedThemeOption));
        OnPropertyChanged(nameof(SelectedThemeDisplayName));
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
}
