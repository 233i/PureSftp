using System.Linq;
using System.Reflection;
using PureSFTP.Services;

namespace PureSFTP.ViewModels;

public sealed class LocalizedTextViewModel : ViewModelBase
{
    private static readonly string[] RefreshPropertyNames =
        typeof(LocalizedTextViewModel)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetMethod?.GetParameters().Length == 0)
            .Select(property => property.Name)
            .ToArray();

    private readonly ILocalizationService _localizationService;

    public LocalizedTextViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _localizationService.LanguageChanged += (_, _) => Refresh();
    }

    public string WorkspaceHeroTitle => _localizationService.Get("WorkspaceHeroTitle");

    public string SettingsHeroTitle => _localizationService.Get("SettingsHeroTitle");

    public string StatusLabel => _localizationService.Get("StatusLabel");

    public string WorkspaceNavButton => _localizationService.Get("WorkspaceNavButton");

    public string SettingsNavButton => _localizationService.Get("SettingsNavButton");

    public string ConnectionMenu => _localizationService.Get("ConnectionMenu");

    public string FileMenu => _localizationService.Get("FileMenu");

    public string BrowserMenu => _localizationService.Get("BrowserMenu");

    public string ViewMenu => _localizationService.Get("ViewMenu");

    public string CancelButton => _localizationService.Get("CancelButton");

    public string ConnectionsTitle => _localizationService.Get("ConnectionsTitle");

    public string NewConnectionButton => _localizationService.Get("NewConnectionButton");

    public string EditConnectionButton => _localizationService.Get("EditConnectionButton");

    public string DeleteConnectionButton => _localizationService.Get("DeleteConnectionButton");

    public string NoConnectionsHint => _localizationService.Get("NoConnectionsHint");

    public string TaskCenterTitle => _localizationService.Get("TaskCenterTitle");

    public string ConnectionTitle => _localizationService.Get("ConnectionTitle");

    public string HostLabel => _localizationService.Get("HostLabel");

    public string PortLabel => _localizationService.Get("PortLabel");

    public string UsernameLabel => _localizationService.Get("UsernameLabel");

    public string PasswordLabel => _localizationService.Get("PasswordLabel");

    public string StartupPathLabel => _localizationService.Get("StartupPathLabel");

    public string HostPlaceholder => _localizationService.Get("HostPlaceholder");

    public string UsernamePlaceholder => _localizationService.Get("UsernamePlaceholder");

    public string PasswordPlaceholder => _localizationService.Get("PasswordPlaceholder");

    public string StartupPathPlaceholder => _localizationService.Get("StartupPathPlaceholder");

    public string ConnectButton => _localizationService.Get("ConnectButton");

    public string DisconnectButton => _localizationService.Get("DisconnectButton");

    public string ActionsTitle => _localizationService.Get("ActionsTitle");

    public string UploadButton => _localizationService.Get("UploadButton");

    public string UploadFolderButton => _localizationService.Get("UploadFolderButton");

    public string DownloadButton => _localizationService.Get("DownloadButton");

    public string DeleteButton => _localizationService.Get("DeleteButton");

    public string CreateFolderHint => _localizationService.Get("CreateFolderHint");

    public string CreateFolderPlaceholder => _localizationService.Get("CreateFolderPlaceholder");

    public string CreateFolderButton => _localizationService.Get("CreateFolderButton");

    public string ExplorerTitle => _localizationService.Get("ExplorerTitle");

    public string UpButton => _localizationService.Get("UpButton");

    public string OpenButton => _localizationService.Get("OpenButton");

    public string RefreshButton => _localizationService.Get("RefreshButton");

    public string NameHeader => _localizationService.Get("NameHeader");

    public string TypeHeader => _localizationService.Get("TypeHeader");

    public string SizeHeader => _localizationService.Get("SizeHeader");

    public string ModifiedHeader => _localizationService.Get("ModifiedHeader");

    public string ActivityTitle => _localizationService.Get("ActivityTitle");

    public string SettingsTitle => _localizationService.Get("SettingsTitle");

    public string SettingsDescription => _localizationService.Get("SettingsDescription");

    public string LanguageTitle => _localizationService.Get("LanguageTitle");

    public string LanguageDescription => _localizationService.Get("LanguageDescription");

    public string LanguageLabel => _localizationService.Get("LanguageLabel");

    public string LanguagePreviewLabel => _localizationService.Get("LanguagePreviewLabel");

    public string AppInfoTitle => _localizationService.Get("AppInfoTitle");

    public string AppInfoDescription => _localizationService.Get("AppInfoDescription");

    public void Refresh()
    {
        foreach (var propertyName in RefreshPropertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
