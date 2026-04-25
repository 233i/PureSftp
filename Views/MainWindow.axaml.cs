using Avalonia.Controls;
using Avalonia.Input;
using PureSFTP.Services;
using PureSFTP.ViewModels;

namespace PureSFTP.Views;

public partial class MainWindow : Window
{
    private readonly IFileDialogService _fileDialogService;
    private readonly INewConnectionDialogService _newConnectionDialogService;
    private readonly ITextInputDialogService _textInputDialogService;

    public MainWindow()
        : this(new FileDialogService(), new NewConnectionDialogService(), new TextInputDialogService())
    {
    }

    public MainWindow(
        IFileDialogService fileDialogService,
        INewConnectionDialogService newConnectionDialogService,
        ITextInputDialogService textInputDialogService)
    {
        _fileDialogService = fileDialogService;
        _newConnectionDialogService = newConnectionDialogService;
        _textInputDialogService = textInputDialogService;
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, System.EventArgs e)
    {
        _fileDialogService.AttachStorageProvider(StorageProvider);
        _newConnectionDialogService.AttachOwner(this);
        _textInputDialogService.AttachOwner(this);
    }

    private async void OnRemoteItemsDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.OpenSelectedCommand.CanExecute(null))
        {
            await viewModel.OpenSelectedCommand.ExecuteAsync(null);
        }
    }

    private async void OnSavedConnectionsDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            SavedConnectionsListBox.SelectedItem is not SavedConnectionViewModel savedConnection)
        {
            return;
        }

        if (viewModel.ConnectSavedConnectionCommand.CanExecute(savedConnection))
        {
            await viewModel.ConnectSavedConnectionCommand.ExecuteAsync(savedConnection);
        }
    }
}
