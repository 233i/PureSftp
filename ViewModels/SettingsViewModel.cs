using System.Collections.ObjectModel;
using PureSFTP.Models;
using PureSFTP.Services;

namespace PureSFTP.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ILocalizationService _localizationService;
    private readonly IAppSettingsService _appSettingsService;
    private LanguageOption? _selectedLanguageOption;

    public SettingsViewModel(ILocalizationService localizationService, IAppSettingsService appSettingsService)
    {
        _localizationService = localizationService;
        _appSettingsService = appSettingsService;

        LanguageOptions =
        [
            new LanguageOption(AppLanguage.English, "English"),
            new LanguageOption(AppLanguage.ChineseSimplified, "简体中文"),
        ];

        _selectedLanguageOption = FindOption(_localizationService.CurrentLanguage);
        _localizationService.LanguageChanged += (_, _) => SyncSelectedLanguage();
    }

    public ObservableCollection<LanguageOption> LanguageOptions { get; }

    public LanguageOption? SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (SetProperty(ref _selectedLanguageOption, value) && value is not null)
            {
                _localizationService.SetLanguage(value.Language);
                _appSettingsService.Save(new AppSettings
                {
                    Language = value.Language,
                });

                OnPropertyChanged(nameof(SelectedLanguageDisplayName));
            }
        }
    }

    public string SelectedLanguageDisplayName => SelectedLanguageOption?.DisplayName ?? string.Empty;

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
}
