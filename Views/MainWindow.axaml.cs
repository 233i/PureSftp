using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using PureSFTP.Models;
using PureSFTP.Services;
using PureSFTP.ViewModels;

namespace PureSFTP.Views;

public partial class MainWindow : Window
{
    private readonly IFileDialogService _fileDialogService;
    private readonly INewConnectionDialogService _newConnectionDialogService;
    private readonly ITextInputDialogService _textInputDialogService;
    private readonly IChoiceDialogService _choiceDialogService;

    public MainWindow()
        : this(new FileDialogService(), new NewConnectionDialogService(), new TextInputDialogService(), new ChoiceDialogService())
    {
    }

    public MainWindow(
        IFileDialogService fileDialogService,
        INewConnectionDialogService newConnectionDialogService,
        ITextInputDialogService textInputDialogService,
        IChoiceDialogService choiceDialogService)
    {
        _fileDialogService = fileDialogService;
        _newConnectionDialogService = newConnectionDialogService;
        _textInputDialogService = textInputDialogService;
        _choiceDialogService = choiceDialogService;
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, System.EventArgs e)
    {
        _fileDialogService.AttachStorageProvider(StorageProvider);
        _newConnectionDialogService.AttachOwner(this);
        _textInputDialogService.AttachOwner(this);
        _choiceDialogService.AttachOwner(this);
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

    private void OnRemoteItemsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Browser.ReplaceSelectedItems(RemoteItemsListBox.SelectedItems?.OfType<RemoteItem>() ?? []);
        }
    }

    private void OnRemoteItemsDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DataContext is MainWindowViewModel { IsConnected: true } &&
            e.DataTransfer.Contains(DataFormat.File)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnRemoteItemsDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || !viewModel.IsConnected)
        {
            return;
        }

        var storageItems = e.DataTransfer.TryGetFiles();
        if (storageItems is null)
        {
            return;
        }

        var localPaths = storageItems
            .Select(item => item.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToList();

        if (localPaths.Count > 0)
        {
            await viewModel.UploadDroppedPathsAsync(localPaths);
        }
    }

    private void OnSavedConnectionsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        if (e.Source is not Avalonia.Visual visual)
        {
            SavedConnectionsListBox.SelectedItem = null;
            return;
        }

        var listBoxItem = visual.FindAncestorOfType<ListBoxItem>();
        SavedConnectionsListBox.SelectedItem = listBoxItem?.DataContext;
    }

    private async void OnEditSavedConnectionMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            SavedConnectionsListBox.SelectedItem is not SavedConnectionViewModel savedConnection)
        {
            return;
        }

        if (viewModel.EditSavedConnectionCommand.CanExecute(savedConnection))
        {
            await viewModel.EditSavedConnectionCommand.ExecuteAsync(savedConnection);
        }
    }

    private async void OnDeleteSavedConnectionMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            SavedConnectionsListBox.SelectedItem is not SavedConnectionViewModel savedConnection)
        {
            return;
        }

        if (viewModel.DeleteSavedConnectionCommand.CanExecute(savedConnection))
        {
            await viewModel.DeleteSavedConnectionCommand.ExecuteAsync(savedConnection);
        }
    }
}
