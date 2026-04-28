using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
    private readonly ICredentialStore _credentialStore;
    private readonly INewConnectionDialogService _newConnectionDialogService;
    private readonly ITextInputDialogService _textInputDialogService;
    private readonly IChoiceDialogService _choiceDialogService;

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
        ICredentialStore CredentialStore,
        INewConnectionDialogService NewConnectionDialogService,
        ITextInputDialogService TextInputDialogService,
        IChoiceDialogService ChoiceDialogService) services)
        : this(
            services.SftpClientService,
            services.FileDialogService,
            services.LocalizationService,
            services.SettingsViewModel,
            services.ConnectionRepository,
            services.CredentialStore,
            services.NewConnectionDialogService,
            services.TextInputDialogService,
            services.ChoiceDialogService)
    {
    }

    public MainWindowViewModel(
        ISftpClientService sftpClientService,
        IFileDialogService fileDialogService,
        ILocalizationService localizationService,
        SettingsViewModel settings,
        IConnectionRepository connectionRepository,
        ICredentialStore credentialStore,
        INewConnectionDialogService newConnectionDialogService,
        ITextInputDialogService textInputDialogService,
        IChoiceDialogService choiceDialogService)
    {
        _sftpClientService = sftpClientService;
        _fileDialogService = fileDialogService;
        _localizationService = localizationService;
        _connectionRepository = connectionRepository;
        _credentialStore = credentialStore;
        _newConnectionDialogService = newConnectionDialogService;
        _textInputDialogService = textInputDialogService;
        _choiceDialogService = choiceDialogService;

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
            if (Browser.SelectedItemCount > 1)
            {
                return T("SelectionSummaryMany", Browser.SelectedItemCount);
            }

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

    public string SortNameHeader => GetSortHeader(T("NameHeader"), RemoteSortColumn.Name);

    public string SortSizeHeader => GetSortHeader(T("SizeHeader"), RemoteSortColumn.Size);

    public string SortModifiedHeader => GetSortHeader(T("ModifiedHeader"), RemoteSortColumn.ModifiedAt);

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

        var savedConnection = SaveConnectionWithCredential(connection);
        var viewModel = new SavedConnectionViewModel(savedConnection);
        SavedConnections.Insert(0, viewModel);
        OnConnectionsChanged();
        StatusMessage = T("StatusConnectionSaved", savedConnection.Name);
        ShowToast(StatusMessage);
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
            var password = GetSavedPassword(savedConnection.Connection);
            await _sftpClientService.ConnectAsync(savedConnection.Connection.ToOptions(password));
            SetActiveConnection(savedConnection);
            IsConnected = true;
            Connection.Host = savedConnection.Connection.Host;
            Connection.PortText = savedConnection.Connection.Port.ToString();
            Connection.Username = savedConnection.Connection.Username;
            Connection.Password = password;

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

    [RelayCommand]
    private async Task EditSavedConnectionAsync(SavedConnectionViewModel? savedConnection)
    {
        if (savedConnection is null)
        {
            return;
        }

        var oldConnection = savedConnection.Connection;
        var password = GetSavedPassword(oldConnection);
        var dialogViewModel = new NewConnectionViewModel(_sftpClientService, _localizationService, oldConnection, password);
        var editedConnection = await _newConnectionDialogService.ShowAsync(dialogViewModel);
        if (editedConnection is null)
        {
            return;
        }

        var shouldDisconnect = savedConnection.IsActive && HasConnectionTargetChanged(oldConnection, editedConnection);
        if (shouldDisconnect)
        {
            await DisconnectAsync();
        }

        var updatedConnection = UpdateConnectionWithCredential(editedConnection);
        savedConnection.UpdateConnection(updatedConnection);
        StatusMessage = T("StatusConnectionUpdated", updatedConnection.Name);
        ShowToast(StatusMessage);
        OnPropertyChanged(nameof(ConnectionSummary));
    }

    [RelayCommand]
    private async Task DeleteSavedConnectionAsync(SavedConnectionViewModel? savedConnection)
    {
        if (savedConnection is null)
        {
            return;
        }

        if (savedConnection.IsActive)
        {
            await DisconnectAsync();
        }

        TryDeletePassword(savedConnection.Connection);
        _connectionRepository.Delete(savedConnection.Id);
        SavedConnections.Remove(savedConnection);
        OnConnectionsChanged();
        StatusMessage = T("StatusConnectionDeleted", savedConnection.Name);
        ShowToast(StatusMessage);
    }

    [RelayCommand]
    private void SortByName()
    {
        SortBy(RemoteSortColumn.Name);
    }

    [RelayCommand]
    private void SortBySize()
    {
        SortBy(RemoteSortColumn.Size);
    }

    [RelayCommand]
    private void SortByModifiedAt()
    {
        SortBy(RemoteSortColumn.ModifiedAt);
    }

    private bool CanConnect() => !IsConnected && Connection.IsReady;

    private bool CanDisconnect() => IsConnected;

    private bool CanRefresh() => IsConnected;

    private bool CanOpenSelected() => IsConnected && Browser.SelectedItem is not null;

    private bool CanGoToParent() =>
        IsConnected && !string.Equals(Browser.CurrentPath, "/", StringComparison.Ordinal);

    private bool CanUpload() => IsConnected;

    private bool CanDownload() =>
        IsConnected && GetSelectedTransferItems().Count > 0;

    private bool CanDeleteSelected() =>
        IsConnected && GetSelectedTransferItems().Count > 0;

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
        var localPaths = await _fileDialogService.PickUploadFilesAsync();
        if (localPaths.Count == 0)
        {
            StatusMessage = T("StatusUploadCancelled");
            return;
        }

        await UploadPathsAsync(localPaths);
    }

    [RelayCommand(CanExecute = nameof(CanUpload))]
    private async Task UploadFolderAsync()
    {
        var localFolderPaths = await _fileDialogService.PickUploadFoldersAsync(T("DialogUploadFolderTitle"));
        if (localFolderPaths.Count == 0)
        {
            StatusMessage = T("StatusUploadCancelled");
            return;
        }

        await UploadPathsAsync(localFolderPaths);
    }

    public async Task UploadDroppedPathsAsync(IReadOnlyList<string> localPaths)
    {
        if (!IsConnected || localPaths.Count == 0)
        {
            return;
        }

        await UploadPathsAsync(localPaths);
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        var selectedItems = GetSelectedTransferItems();
        if (selectedItems.Count == 0)
        {
            return;
        }

        if (selectedItems.Count == 1 && !selectedItems[0].IsDirectory)
        {
            var selectedItem = selectedItems[0];
            var localFilePath = await _fileDialogService.PickDownloadTargetFileAsync(selectedItem.Name);
            if (string.IsNullOrWhiteSpace(localFilePath))
            {
                StatusMessage = T("StatusDownloadCancelled");
                return;
            }

            await DownloadItemsAsync(selectedItems, new Dictionary<string, string>
            {
                [selectedItem.FullPath] = localFilePath,
            });

            return;
        }

        var localFolder = await _fileDialogService.PickLocalFolderAsync(T("DialogDownloadFolderTitle"));
        if (string.IsNullOrWhiteSpace(localFolder))
        {
            StatusMessage = T("StatusDownloadCancelled");
            return;
        }

        await DownloadItemsAsync(selectedItems, selectedItems.ToDictionary(
            item => item.FullPath,
            item => Path.Combine(localFolder, item.Name)));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        var selectedItems = GetSelectedTransferItems();
        if (selectedItems.Count == 0)
        {
            return;
        }

        var confirmChoice = await _choiceDialogService.ShowAsync(
            T("DeleteConfirmTitle"),
            T("DeleteConfirmMessage", selectedItems.Count),
            T("DeleteConfirmButton"),
            T("CancelButton"));

        if (confirmChoice != DialogChoice.Primary)
        {
            return;
        }

        await DeleteItemsAsync(selectedItems);
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

    private async Task UploadPathsAsync(IReadOnlyList<string> localPaths)
    {
        var uploadPaths = NormalizeExistingLocalPaths(localPaths);
        if (uploadPaths.Count == 0)
        {
            StatusMessage = T("StatusNoUploadableItems");
            ShowToast(StatusMessage);
            return;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        var toast = ShowProgressToast(
            T("StatusUploadingBatch", uploadPaths.Count),
            T("CancelButton"),
            cancellationTokenSource.Cancel);

        await RunBusyAsync(T("StatusUploadingBatch", uploadPaths.Count), async () =>
        {
            var totalBytes = uploadPaths.Sum(GetLocalPathSize);
            var completedBytes = 0L;

            foreach (var localPath in uploadPaths)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                var localName = GetLocalPathName(localPath);
                var localSize = GetLocalPathSize(localPath);
                var remoteTargetPath = RemotePathHelper.Combine(Browser.CurrentPath, localName);
                var shouldUpload = await ConfirmRemoteOverwriteAsync(remoteTargetPath);
                if (!shouldUpload)
                {
                    completedBytes += localSize;
                    toast.Progress = CalculatePercent(completedBytes, totalBytes);
                    continue;
                }

                var progress = CreateAggregateProgress(toast, completedBytes, totalBytes);
                if (File.Exists(localPath))
                {
                    await _sftpClientService.UploadFileAsync(localPath, Browser.CurrentPath, progress, cancellationTokenSource.Token);
                }
                else
                {
                    await _sftpClientService.UploadDirectoryAsync(localPath, Browser.CurrentPath, progress, cancellationTokenSource.Token);
                }

                completedBytes += localSize;
                toast.Progress = CalculatePercent(completedBytes, totalBytes);
            }

            await LoadDirectoryCoreAsync(Browser.CurrentPath);
            StatusMessage = T("StatusUploadedBatch", uploadPaths.Count);
            CompleteProgressToast(toast, T("ToastUploadSuccess"));
        },
        error =>
        {
            StatusMessage = error is OperationCanceledException
                ? T("StatusUploadCancelled")
                : T("StatusUploadFailed", error.Message);
            CompleteProgressToast(toast, StatusMessage);

            if (error is not OperationCanceledException)
            {
                ShowRetryToast(StatusMessage, () => UploadPathsAsync(uploadPaths));
            }

            return Task.CompletedTask;
        });
    }

    private async Task DownloadItemsAsync(IReadOnlyList<RemoteItem> items, IReadOnlyDictionary<string, string> targetPaths)
    {
        var toast = ShowProgressToast(T("StatusDownloadingBatch", items.Count));

        await RunBusyAsync(T("StatusPreparingDownload"), async () =>
        {
            var itemSizes = new Dictionary<string, long>();
            foreach (var item in items)
            {
                itemSizes[item.FullPath] = await _sftpClientService.GetSizeAsync(item);
            }

            var totalBytes = itemSizes.Values.Sum();
            var completedBytes = 0L;

            foreach (var item in items)
            {
                if (!targetPaths.TryGetValue(item.FullPath, out var targetPath))
                {
                    continue;
                }

                var itemSize = itemSizes[item.FullPath];
                var shouldDownload = await ConfirmLocalOverwriteAsync(targetPath);
                if (!shouldDownload)
                {
                    completedBytes += itemSize;
                    toast.Progress = CalculatePercent(completedBytes, totalBytes);
                    continue;
                }

                var progress = CreateAggregateProgress(toast, completedBytes, totalBytes);
                if (item.IsDirectory)
                {
                    await _sftpClientService.DownloadDirectoryAsync(item.FullPath, targetPath, progress);
                }
                else
                {
                    await _sftpClientService.DownloadFileAsync(item.FullPath, targetPath, progress);
                }

                completedBytes += itemSize;
                toast.Progress = CalculatePercent(completedBytes, totalBytes);
            }

            StatusMessage = T("StatusDownloadedBatch", items.Count);
            CompleteProgressToast(toast, T("ToastDownloadSuccess"));
        },
        error =>
        {
            StatusMessage = error is OperationCanceledException
                ? T("StatusDownloadCancelled")
                : T("StatusDownloadFailed", error.Message);
            CompleteProgressToast(toast, StatusMessage);

            if (error is not OperationCanceledException)
            {
                ShowRetryToast(StatusMessage, () => DownloadItemsAsync(items, targetPaths));
            }

            return Task.CompletedTask;
        });
    }

    private async Task DeleteItemsAsync(IReadOnlyList<RemoteItem> items)
    {
        var toast = ShowProgressToast(T("StatusDeletingBatch", items.Count));

        await RunBusyAsync(T("StatusDeletingBatch", items.Count), async () =>
        {
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (await _sftpClientService.ExistsAsync(item.FullPath))
                {
                    await _sftpClientService.DeleteAsync(item);
                }

                toast.Progress = CalculatePercent(index + 1, items.Count);
            }

            await LoadDirectoryCoreAsync(Browser.CurrentPath);
            StatusMessage = T("StatusDeletedBatch", items.Count);
            CompleteProgressToast(toast, StatusMessage);
        },
        error =>
        {
            StatusMessage = T("StatusDeleteFailed", error.Message);
            CompleteProgressToast(toast, StatusMessage);
            ShowRetryToast(StatusMessage, () => DeleteItemsAsync(items));
            return Task.CompletedTask;
        });
    }

    private async Task<bool> ConfirmRemoteOverwriteAsync(string remotePath)
    {
        if (!await _sftpClientService.ExistsAsync(remotePath))
        {
            return true;
        }

        var choice = await _choiceDialogService.ShowAsync(
            T("OverwriteTitle"),
            T("OverwriteRemoteMessage", remotePath),
            T("OverwriteButton"),
            T("CancelButton"),
            T("SkipButton"));

        return choice switch
        {
            DialogChoice.Primary => true,
            DialogChoice.Secondary => false,
            _ => throw new OperationCanceledException(),
        };
    }

    private async Task<bool> ConfirmLocalOverwriteAsync(string localPath)
    {
        if (!File.Exists(localPath) && !Directory.Exists(localPath))
        {
            return true;
        }

        var choice = await _choiceDialogService.ShowAsync(
            T("OverwriteTitle"),
            T("OverwriteLocalMessage", localPath),
            T("OverwriteButton"),
            T("CancelButton"),
            T("SkipButton"));

        return choice switch
        {
            DialogChoice.Primary => true,
            DialogChoice.Secondary => false,
            _ => throw new OperationCanceledException(),
        };
    }

    private IReadOnlyList<RemoteItem> GetSelectedTransferItems()
    {
        if (Browser.SelectedItems.Count > 0)
        {
            return Browser.SelectedItems
                .Where(item => !item.IsParentShortcut)
                .ToList();
        }

        return Browser.SelectedItem is { IsParentShortcut: false } selectedItem
            ? [selectedItem]
            : [];
    }

    private static IReadOnlyList<string> NormalizeExistingLocalPaths(IEnumerable<string> localPaths)
    {
        return localPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string GetLocalPathName(string localPath)
    {
        if (Directory.Exists(localPath))
        {
            return new DirectoryInfo(localPath).Name;
        }

        return Path.GetFileName(localPath);
    }

    private static long GetLocalPathSize(string localPath)
    {
        if (File.Exists(localPath))
        {
            return new FileInfo(localPath).Length;
        }

        return Directory.Exists(localPath)
            ? GetLocalDirectorySize(new DirectoryInfo(localPath))
            : 0;
    }

    private static long GetLocalDirectorySize(DirectoryInfo directory)
    {
        var totalBytes = 0L;

        foreach (var file in directory.EnumerateFiles().Where(file => !file.Attributes.HasFlag(FileAttributes.ReparsePoint)))
        {
            totalBytes += file.Length;
        }

        foreach (var childDirectory in directory.EnumerateDirectories().Where(child => !child.Attributes.HasFlag(FileAttributes.ReparsePoint)))
        {
            totalBytes += GetLocalDirectorySize(childDirectory);
        }

        return totalBytes;
    }

    private static IProgress<TransferProgress> CreateAggregateProgress(
        ToastMessageViewModel toast,
        long completedBytes,
        long totalBytes)
    {
        return new Progress<TransferProgress>(transferProgress =>
        {
            toast.Progress = CalculatePercent(completedBytes + transferProgress.BytesTransferred, totalBytes);
        });
    }

    private static double CalculatePercent(long completed, long total)
    {
        return total <= 0
            ? 100
            : Math.Clamp(completed * 100d / total, 0, 100);
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
            SavedConnections.Add(new SavedConnectionViewModel(MigrateStoredPassword(connection)));
        }

        OnConnectionsChanged();
    }

    private SavedConnection SaveConnectionWithCredential(SavedConnection connection)
    {
        var password = connection.Password;
        var savedConnection = _connectionRepository.Add(connection.WithPassword(string.Empty));

        if (!string.IsNullOrEmpty(password) && !TrySavePassword(savedConnection, password))
        {
            savedConnection = _connectionRepository.Update(savedConnection.WithPassword(password));
        }

        return savedConnection;
    }

    private SavedConnection UpdateConnectionWithCredential(SavedConnection connection)
    {
        var password = connection.Password;
        var savedConnection = _connectionRepository.Update(connection.WithPassword(string.Empty));

        if (string.IsNullOrEmpty(password))
        {
            TryDeletePassword(savedConnection);
            return savedConnection;
        }

        if (!TrySavePassword(savedConnection, password))
        {
            savedConnection = _connectionRepository.Update(savedConnection.WithPassword(password));
        }

        return savedConnection;
    }

    private SavedConnection MigrateStoredPassword(SavedConnection connection)
    {
        if (string.IsNullOrEmpty(connection.Password))
        {
            return connection;
        }

        if (!TrySavePassword(connection, connection.Password))
        {
            return connection;
        }

        _connectionRepository.ClearPassword(connection.Id);
        return connection.WithPassword(string.Empty);
    }

    private string GetSavedPassword(SavedConnection connection)
    {
        try
        {
            return _credentialStore.ReadPassword(connection) ?? connection.Password;
        }
        catch
        {
            return connection.Password;
        }
    }

    private bool TrySavePassword(SavedConnection connection, string password)
    {
        try
        {
            return _credentialStore.SavePassword(connection, password);
        }
        catch
        {
            return false;
        }
    }

    private void TryDeletePassword(SavedConnection connection)
    {
        try
        {
            _credentialStore.DeletePassword(connection);
        }
        catch
        {
            // Credential cleanup is best-effort; the SQLite row remains the source of truth.
        }
    }

    private void SortBy(RemoteSortColumn sortColumn)
    {
        Browser.SortBy(sortColumn);
        RefreshSortHeaders();
    }

    private void RefreshSortHeaders()
    {
        OnPropertyChanged(nameof(SortNameHeader));
        OnPropertyChanged(nameof(SortSizeHeader));
        OnPropertyChanged(nameof(SortModifiedHeader));
    }

    private string GetSortHeader(string title, RemoteSortColumn sortColumn)
    {
        if (Browser.SortColumn != sortColumn)
        {
            return title;
        }

        return $"{title} {(Browser.IsSortAscending ? "^" : "v")}";
    }

    private static bool HasConnectionTargetChanged(SavedConnection oldConnection, SavedConnection newConnection)
    {
        return !string.Equals(oldConnection.Host, newConnection.Host, StringComparison.OrdinalIgnoreCase) ||
            oldConnection.Port != newConnection.Port ||
            !string.Equals(oldConnection.Username, newConnection.Username, StringComparison.Ordinal);
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

    private void ShowRetryToast(string message, Func<Task> retryAction)
    {
        var toast = AddToast(message, false, T("RetryButton"), () =>
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () => await retryAction());
        });
        ScheduleToastClose(toast, 8000);
    }

    private ToastMessageViewModel ShowProgressToast(string message)
    {
        return AddToast(message, true);
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

    private void ScheduleToastClose(ToastMessageViewModel toast, int delayMilliseconds = 3000)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(delayMilliseconds);
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
        if (e.PropertyName is nameof(RemoteBrowserViewModel.SelectedItem) or nameof(RemoteBrowserViewModel.SelectedItems) or nameof(RemoteBrowserViewModel.CurrentPath))
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
        RefreshSortHeaders();
    }

    private static (
        ISftpClientService SftpClientService,
        IFileDialogService FileDialogService,
        ILocalizationService LocalizationService,
        SettingsViewModel SettingsViewModel,
        IConnectionRepository ConnectionRepository,
        ICredentialStore CredentialStore,
        INewConnectionDialogService NewConnectionDialogService,
        ITextInputDialogService TextInputDialogService,
        IChoiceDialogService ChoiceDialogService)
        CreateDesignServices()
    {
        var localizationService = new LocalizationService(AppLanguage.English);
        var databasePath = DatabasePathProvider.GetDatabasePath();
        new SqliteDatabaseInitializer(databasePath).Initialize();
        var appSettingsService = new SqliteAppSettingsService(databasePath);
        var themeService = new ThemeService();
        var appSettings = appSettingsService.Load();
        themeService.SetCustomGradient(appSettings.CustomGradientStartColor, appSettings.CustomGradientEndColor);
        themeService.SetTheme(appSettings.ThemeMode);

        return (
            new SftpClientService(),
            new FileDialogService(),
            localizationService,
            new SettingsViewModel(localizationService, appSettingsService, themeService),
            new SqliteConnectionRepository(databasePath),
            new SystemCredentialStore(),
            new NewConnectionDialogService(),
            new TextInputDialogService(),
            new ChoiceDialogService());
    }
}
