using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PureSFTP.Services;
using PureSFTP.ViewModels;
using PureSFTP.Views;

namespace PureSFTP;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Name = "PureSftp";
    }

    public override void OnFrameworkInitializationCompleted()
    {
        MacApplicationIconService.ApplyDockIcon();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var databasePath = DatabasePathProvider.GetDatabasePath();
            var databaseInitializer = new SqliteDatabaseInitializer(databasePath);
            databaseInitializer.Initialize();

            var appSettingsService = new SqliteAppSettingsService(databasePath);
            var appSettings = appSettingsService.Load();
            var fileDialogService = new FileDialogService();
            var newConnectionDialogService = new NewConnectionDialogService();
            var textInputDialogService = new TextInputDialogService();
            var choiceDialogService = new ChoiceDialogService();
            var sftpClientService = new SftpClientService();
            var connectionRepository = new SqliteConnectionRepository(databasePath);
            var credentialStore = new SystemCredentialStore();
            var themeService = new ThemeService();
            themeService.SetTheme(appSettings.ThemeMode);
            var localizationService = new LocalizationService(appSettings.Language);
            var settingsViewModel = new SettingsViewModel(localizationService, appSettingsService, themeService);
            var mainWindowViewModel = new MainWindowViewModel(
                sftpClientService,
                fileDialogService,
                localizationService,
                settingsViewModel,
                connectionRepository,
                credentialStore,
                newConnectionDialogService,
                textInputDialogService,
                choiceDialogService);

            desktop.MainWindow = new MainWindow(fileDialogService, newConnectionDialogService, textInputDialogService, choiceDialogService)
            {
                DataContext = mainWindowViewModel,
            };
            MacApplicationIconService.ApplyDockIcon();

            desktop.Exit += async (_, _) => await sftpClientService.DisposeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
