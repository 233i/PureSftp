using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PureSFTP.Models;
using PureSFTP.Services;
using PureSFTP.Utilities;

namespace PureSFTP.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISftpClientService _sftpClientService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ILocalizationService _localizationService;
    private readonly IConnectionRepository _connectionRepository;
    private readonly INewConnectionDialogService _newConnectionDialogService;
    private readonly ITextInputDialogService _textInputDialogService;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToParentCommand))]
    [NotifyCanExecuteChangedFor(nameof(UploadCommand))]
    [NotifyCanExecuteChangedFor(nameof(UploadFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateFolderCommand))]
    private bool isConnected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateFolderCommand))]
    private string newFolderName = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private AppPage currentPage = AppPage.Workspace;

    public MainWindowViewModel()
        : this(CreateDesignServices())
    {
    }

    private MainWindowViewModel(
        (ISftpClientService SftpClientService,
        IFileDialogService FileDialogService,
        ILocalizationService LocalizationService,
        SettingsViewModel SettingsViewModel,
        IConnectionRepository ConnectionRepository,
        INewConnectionDialogService NewConnectionDialogService,
        ITextInputDialogService TextInputDialogService) services)
        : this(
            services.SftpClientService,
            services.FileDialogService,
            services.LocalizationService,
            services.SettingsViewModel,
            services.ConnectionRepository,
            services.NewConnectionDialogService,
            services.TextInputDialogService)
    {
    }

    public MainWindowViewModel(
        ISftpClientService sftpClientService,
        IFileDialogService fileDialogService,
        ILocalizationService localizationService,
        SettingsViewModel settings,
        IConnectionRepository connectionRepository,
        INewConnectionDialogService newConnectionDialogService,
        ITextInputDialogService textInputDialogService)
    {
        _sftpClientService = sftpClientService;
        _fileDialogService = fileDialogService;
        _localizationService = localizationService;
        _connectionRepository = connectionRepository;
        _newConnectionDialogService = newConnectionDialogService;
        _textInputDialogService = textInputDialogService;

        Texts = new LocalizedTextViewModel(_localizationService);
        Settings = settings;
        Connection = new ConnectionSettingsViewModel();
        Browser = new RemoteBrowserViewModel();
        SavedConnections = new ObservableCollection<SavedConnectionViewModel>();
        Toasts = new ObservableCollection<ToastMessageViewModel>();
        StatusMessage = T("StatusDefault");

        Connection.PropertyChanged += OnConnectionChanged;
        Browser.PropertyChanged += OnBrowserChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;
        LoadSavedConnections();
    }

    public LocalizedTextViewModel Texts { get; }

    public SettingsViewModel Settings { get; }

    public ConnectionSettingsViewModel Connection { get; }

    public RemoteBrowserViewModel Browser { get; }

    public ObservableCollection<SavedConnectionViewModel> SavedConnections { get; }

    public ObservableCollection<ToastMessageViewModel> Toasts { get; }

    public bool HasSavedConnections => SavedConnections.Count > 0;

    public bool HasNoSavedConnections => !HasSavedConnections;

    public bool IsWorkspacePageSelected
    {
        get => CurrentPage == AppPage.Workspace;
        set
        {
            if (value)
            {
                CurrentPage = AppPage.Workspace;
            }
        }
    }

    public bool IsSettingsPageSelected
    {
        get => CurrentPage == AppPage.Settings;
        set
        {
            if (value)
            {
                CurrentPage = AppPage.Settings;
            }
        }
    }

    public string ConnectionStateText => IsBusy ? T("StateWorking") : IsConnected ? T("StateConnected") : T("StateOffline");

    public string ConnectionSummary =>
        IsConnected
            ? T("ConnectionSummaryOnline", ActiveConnection?.Connection.Host ?? Connection.Host, ActiveConnection?.Connection.Port ?? Connection.Port, ActiveConnection?.Connection.Username ?? Connection.Username)
            : T("ConnectionSummaryOffline");

    public SavedConnectionViewModel? ActiveConnection { get; private set; }

    public string SelectionSummary
    {
        get
        {
            if (Browser.SelectedItem is null)
            {
                return T("SelectionSummaryNone");
            }

            if (Browser.SelectedItem.IsParentShortcut)
            {
                return T("SelectionSummaryParent");
            }

            return T(
                "SelectionSummaryItem",
                Browser.SelectedItem.IsDirectory ? T("DirectoryType") : T("FileType"),
                Browser.SelectedItem.FullPath);
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(ConnectionStateText));
    }

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ConnectionStateText));
        OnPropertyChanged(nameof(ConnectionSummary));
    }

    partial void OnCurrentPageChanged(AppPage value)
    {
        OnPropertyChanged(nameof(IsWorkspacePageSelected));
        OnPropertyChanged(nameof(IsSettingsPageSelected));
    }

    [RelayCommand]
    private void ShowWorkspacePage()
    {
        CurrentPage = AppPage.Workspace;
    }

    [RelayCommand]
    private void ShowSettingsPage()
    {
        CurrentPage = AppPage.Settings;
    }

    [RelayCommand]
    private async Task ShowNewConnectionAsync()
    {
        var dialogViewModel = new NewConnectionViewModel(_sftpClientService, _localizationService);
        var connection = await _newConnectionDialogService.ShowAsync(dialogViewModel);
        if (connection is null)
        {
            return;
        }

        var savedConnection = _connectionRepository.Add(connection);
        var viewModel = new SavedConnectionViewModel(savedConnection);
        SavedConnections.Insert(0, viewModel);
        OnConnectionsChanged();
    }

    [RelayCommand]
    private async Task ConnectSavedConnectionAsync(SavedConnectionViewModel? savedConnection)
    {
        if (savedConnection is null)
        {
            return;
        }

        await RunBusyAsync(T("StatusConnecting"), async () =>
        {
            await _sftpClientService.ConnectAsync(savedConnection.Connection.ToOptions());
            SetActiveConnection(savedConnection);
            IsConnected = true;
            Connection.Host = savedConnection.Connection.Host;
            Connection.PortText = savedConnection.Connection.Port.ToString();
            Connection.Username = savedConnection.Connection.Username;
            Connection.Password = savedConnection.Connection.Password;

            var workingDirectory = await _sftpClientService.GetWorkingDirectoryAsync();
            await LoadDirectoryCoreAsync(workingDirectory);
            StatusMessage = T("StatusConnected", savedConnection.Connection.Host);
        },
        async error =>
        {
            IsConnected = false;
            SetActiveConnection(null);
            await _sftpClientService.DisconnectAsync();
            StatusMessage = error.Message;
        });
    }

    private bool CanConnect() => !IsConnected && Connection.IsReady;

    private bool CanDisconnect() => IsConnected;

    private bool CanRefresh() => IsConnected;

    private bool CanOpenSelected() => IsConnected && Browser.SelectedItem is not null;

    private bool CanGoToParent() =>
        IsConnected && !string.Equals(Browser.CurrentPath, "/", StringComparison.Ordinal);

    private bool CanUpload() => IsConnected;

    private bool CanDownload() =>
        IsConnected && Browser.SelectedItem is { IsParentShortcut: false };

    private bool CanDeleteSelected() =>
        IsConnected && Browser.SelectedItem is { IsParentShortcut: false };

    private bool CanCreateFolder() => IsConnected && !string.IsNullOrWhiteSpace(NewFolderName);

    private bool CanShowCreateFolderDialog() => IsConnected;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        await RunBusyAsync(T("StatusConnecting"), async () =>
        {
            await _sftpClientService.ConnectAsync(Connection.ToOptions());
            IsConnected = true;

            var workingDirectory = await _sftpClientService.GetWorkingDirectoryAsync();
            var requestedPath = RemotePathHelper.Resolve(workingDirectory, Connection.StartupPath);

            try
            {
                await LoadDirectoryCoreAsync(requestedPath);
            }
            catch
            {
                await LoadDirectoryCoreAsync(workingDirectory);
            }

            StatusMessage = T("StatusConnected", Connection.Host);
        },
        async error =>
        {
            IsConnected = false;
            await _sftpClientService.DisconnectAsync();
            StatusMessage = error.Message;
        });
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        await RunBusyAsync(T("StatusDisconnecting"), async () =>
        {
            await _sftpClientService.DisconnectAsync();
            IsConnected = false;
            SetActiveConnection(null);
            Browser.Reset();
            StatusMessage = T("StatusDisconnected");
        });
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(T("StatusRefreshing"), async () =>
        {
            await LoadDirectoryCoreAsync(Browser.CurrentPath);
            StatusMessage = T("StatusLoaded", Browser.CurrentPath);
        });
    }

    [RelayCommand(CanExecute = nameof(CanOpenSelected))]
    private async Task OpenSelectedAsync()
    {
        var selectedItem = Browser.SelectedItem;
        if (selectedItem is null)
        {
            return;
        }

        if (selectedItem.IsParentShortcut)
        {
            await GoToParentAsync();
            return;
        }

        if (!selectedItem.IsDirectory)
        {
            StatusMessage = T("StatusFileReadyToDownload", selectedItem.Name);
            return;
        }

        await RunBusyAsync(T("StatusOpening", selectedItem.Name), async () =>
        {
            await LoadDirectoryCoreAsync(selectedItem.FullPath);
            StatusMessage = T("StatusOpened", selectedItem.FullPath);
        });
    }

    [RelayCommand(CanExecute = nameof(CanGoToParent))]
    private async Task GoToParentAsync()
    {
        var parentPath = RemotePathHelper.GetParent(Browser.CurrentPath);

        await RunBusyAsync(T("StatusLoadingParent"), async () =>
        {
            await LoadDirectoryCoreAsync(parentPath);
            StatusMessage = T("StatusOpened", parentPath);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUpload))]
    private async Task UploadAsync()
    {
        var localPath = await _fileDialogService.PickUploadFileAsync();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            StatusMessage = T("StatusUploadCancelled");
            return;
        }

        var fileName = Path.GetFileName(localPath);
        using var cancellationTokenSource = new CancellationTokenSource();
        var toast = ShowProgressToast(
            T("StatusUploading", fileName),
            T("CancelButton"),
            cancellationTokenSource.Cancel);
        var progress = new Progress<TransferProgress>(transferProgress =>
        {
            toast.Progress = transferProgress.Percent;
        });

        await RunBusyAsync(T("StatusUploading", fileName), async () =>
        {
            await _sftpClientService.UploadFileAsync(localPath, Browser.CurrentPath, progress, cancellationTokenSource.Token);
            await LoadDirectoryCoreAsync(Browser.CurrentPath);
            StatusMessage = T("StatusUploaded", fileName);
            CompleteProgressToast(toast, T("ToastUploadSuccess"));
        },
        error =>
        {
            StatusMessage = error is OperationCanceledException ? T("StatusUploadCancelled") : error.Message;
            CompleteProgressToast(toast, StatusMessage);
            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(CanUpload))]
    private async Task UploadFolderAsync()
    {
        var localFolderPath = await _fileDialogService.PickUploadFolderAsync(T("DialogUploadFolderTitle"));
        if (string.IsNullOrWhiteSpace(localFolderPath))
        {
            StatusMessage = T("StatusUploadCancelled");
            return;
        }

        var folderName = new DirectoryInfo(localFolderPath).Name;
        using var cancellationTokenSource = new CancellationTokenSource();
        var toast = ShowProgressToast(
            T("StatusUploadingFolder", folderName),
            T("CancelButton"),
            cancellationTokenSource.Cancel);
        var progress = new Progress<TransferProgress>(transferProgress =>
        {
            toast.Progress = transferProgress.Percent;
        });

        await RunBusyAsync(T("StatusUploadingFolder", folderName), async () =>
        {
            await _sftpClientService.UploadDirectoryAsync(localFolderPath, Browser.CurrentPath, progress, cancellationTokenSource.Token);
            await LoadDirectoryCoreAsync(Browser.CurrentPath);
            StatusMessage = T("StatusUploadedFolder", folderName);
            CompleteProgressToast(toast, T("ToastUploadSuccess"));
        },
        error =>
        {
            StatusMessage = error is OperationCanceledException ? T("StatusUploadCancelled") : error.Message;
            CompleteProgressToast(toast, StatusMessage);
            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        var selectedItem = Browser.SelectedItem;
        if (selectedItem is null || selectedItem.IsParentShortcut)
        {
            return;
        }

        if (selectedItem.IsDirectory)
        {
            var localFolder = await _fileDialogService.PickLocalFolderAsync(T("DialogDownloadFolderTitle"));
            if (string.IsNullOrWhiteSpace(localFolder))
            {
                StatusMessage = T("StatusDownloadCancelled");
                return;
            }

            await RunBusyAsync(T("StatusDownloading", selectedItem.Name), async () =>
            {
                var targetFolder = Path.Combine(localFolder, selectedItem.Name);
                await _sftpClientService.DownloadDirectoryAsync(selectedItem.FullPath, targetFolder);
                StatusMessage = T("StatusDownloadedFolder", selectedItem.Name);
                ShowToast(T("ToastDownloadSuccess"));
            });

            return;
        }

        var localFilePath = await _fileDialogService.PickDownloadTargetFileAsync(selectedItem.Name);
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            StatusMessage = T("StatusDownloadCancelled");
            return;
        }

        await RunBusyAsync(T("StatusDownloading", selectedItem.Name), async () =>
        {
            await _sftpClientService.DownloadFileAsync(selectedItem.FullPath, localFilePath);
            StatusMessage = T("StatusDownloadedFile", selectedItem.Name);
            ShowToast(T("ToastDownloadSuccess"));
        });
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        var selectedItem = Browser.SelectedItem;
        if (selectedItem is null || selectedItem.IsParentShortcut)
        {
            return;
        }

        await RunBusyAsync(T("StatusDeleting", selectedItem.Name), async () =>
        {
            await _sftpClientService.DeleteAsync(selectedItem);
            await LoadDirectoryCoreAsync(Browser.CurrentPath);
            StatusMessage = T("StatusDeleted", selectedItem.Name);
            ShowToast(StatusMessage);
        });
    }

    [RelayCommand(CanExecute = nameof(CanCreateFolder))]
    private async Task CreateFolderAsync()
    {
        var folderName = NewFolderName.Trim();
        var targetPath = RemotePathHelper.Combine(Browser.CurrentPath, folderName);

        await RunBusyAsync(T("StatusCreatingFolder", folderName), async () =>
        {
            await _sftpClientService.CreateDirectoryAsync(targetPath);
            NewFolderName = string.Empty;
            await LoadDirectoryCoreAsync(Browser.CurrentPath);
            StatusMessage = T("StatusCreatedFolder", folderName);
            ShowToast(StatusMessage);
        });
    }

    [RelayCommand(CanExecute = nameof(CanShowCreateFolderDialog))]
    private async Task ShowCreateFolderDialogAsync()
    {
        var folderName = await _textInputDialogService.ShowAsync(T("CreateFolderButton"), T("CreateFolderPlaceholder"));
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        NewFolderName = folderName;
        await CreateFolderAsync();
    }

    private async Task LoadDirectoryCoreAsync(string remotePath)
    {
        var normalizedPath = RemotePathHelper.Normalize(remotePath);
        Browser.CurrentPath = normalizedPath;
        var items = await _sftpClientService.ListDirectoryAsync(normalizedPath);
        Browser.ReplaceItems(items);
    }

    private async Task RunBusyAsync(string busyMessage, Func<Task> action, Func<Exception, Task>? onError = null)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = busyMessage;
            await action();
        }
        catch (Exception exception)
        {
            if (onError is not null)
            {
                await onError(exception);
            }
            else
            {
                StatusMessage = exception.Message;
            }

            if (onError is null)
            {
                ShowToast(exception.Message);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string T(string key, params object[] args) => _localizationService.Get(key, args);

    private void LoadSavedConnections()
    {
        SavedConnections.Clear();
        foreach (var connection in _connectionRepository.GetAll())
        {
            SavedConnections.Add(new SavedConnectionViewModel(connection));
        }

        OnConnectionsChanged();
    }

    private void OnConnectionsChanged()
    {
        OnPropertyChanged(nameof(HasSavedConnections));
        OnPropertyChanged(nameof(HasNoSavedConnections));
    }

    private void SetActiveConnection(SavedConnectionViewModel? activeConnection)
    {
        if (ActiveConnection is not null)
        {
            ActiveConnection.IsActive = false;
        }

        ActiveConnection = activeConnection;
        if (ActiveConnection is not null)
        {
            ActiveConnection.IsActive = true;
        }

        OnPropertyChanged(nameof(ActiveConnection));
        OnPropertyChanged(nameof(ConnectionSummary));
    }

    private void ShowToast(string message)
    {
        var toast = AddToast(message, false);
        ScheduleToastClose(toast);
    }

    private ToastMessageViewModel ShowProgressToast(string message, string cancelText, Action cancelAction)
    {
        return AddToast(message, true, cancelText, cancelAction);
    }

    private ToastMessageViewModel AddToast(string message, bool isProgressVisible, string cancelText = "", Action? cancelAction = null)
    {
        var toast = new ToastMessageViewModel(message, isProgressVisible, cancelText, cancelAction);
        Toasts.Insert(0, toast);

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(20);
            toast.IsShown = true;
        });

        return toast;
    }

    private void CompleteProgressToast(ToastMessageViewModel toast, string message)
    {
        toast.Message = message;
        toast.Progress = 100;
        toast.IsProgressVisible = false;
        toast.IsCancelable = false;
        ScheduleToastClose(toast);
    }

    private void ScheduleToastClose(ToastMessageViewModel toast)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(3000);
            toast.IsClosing = true;
            await Task.Delay(260);
            Toasts.Remove(toast);
        });
    }

    private void OnConnectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConnectionSettingsViewModel.IsReady))
        {
            ConnectCommand.NotifyCanExecuteChanged();
        }

        OnPropertyChanged(nameof(ConnectionSummary));
    }

    private void OnBrowserChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RemoteBrowserViewModel.SelectedItem) or nameof(RemoteBrowserViewModel.CurrentPath))
        {
            OpenSelectedCommand.NotifyCanExecuteChanged();
            GoToParentCommand.NotifyCanExecuteChanged();
            DownloadCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        StatusMessage = T("StatusLanguageChanged", Settings.SelectedLanguageDisplayName);
        OnPropertyChanged(nameof(ConnectionStateText));
        OnPropertyChanged(nameof(ConnectionSummary));
        OnPropertyChanged(nameof(SelectionSummary));
    }

    private static (
        ISftpClientService SftpClientService,
        IFileDialogService FileDialogService,
        ILocalizationService LocalizationService,
        SettingsViewModel SettingsViewModel,
        IConnectionRepository ConnectionRepository,
        INewConnectionDialogService NewConnectionDialogService,
        ITextInputDialogService TextInputDialogService)
        CreateDesignServices()
    {
        var localizationService = new LocalizationService(AppLanguage.English);
        var databasePath = DatabasePathProvider.GetDatabasePath();
        new SqliteDatabaseInitializer(databasePath).Initialize();
        var appSettingsService = new SqliteAppSettingsService(databasePath);

        return (
            new SftpClientService(),
            new FileDialogService(),
            localizationService,
            new SettingsViewModel(localizationService, appSettingsService),
            new SqliteConnectionRepository(databasePath),
            new NewConnectionDialogService(),
            new TextInputDialogService());
    }
}
