using Avalonia.Controls;
using PureSFTP.Models;
using PureSFTP.ViewModels;

namespace PureSFTP.Views;

public partial class NewConnectionWindow : Window
{
    public NewConnectionWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is NewConnectionViewModel viewModel)
        {
            viewModel.SaveRequested += OnSaveRequested;
        }
    }

    private void OnSaveRequested(object? sender, System.EventArgs e)
    {
        if (DataContext is NewConnectionViewModel { Result: SavedConnection result })
        {
            Close(result);
        }
    }
}
