using Avalonia.Controls;
using Avalonia.Interactivity;
using PureSFTP.Models;

namespace PureSFTP.Views;

public partial class ChoiceDialogWindow : Window
{
    public ChoiceDialogWindow()
    {
        InitializeComponent();
    }

    public ChoiceDialogWindow(
        string title,
        string message,
        string primaryText,
        string cancelText,
        string secondaryText = "")
        : this()
    {
        Title = title;
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
        PrimaryButton.Content = primaryText;
        CancelButton.Content = cancelText;
        SecondaryButton.Content = secondaryText;
        SecondaryButton.IsVisible = !string.IsNullOrWhiteSpace(secondaryText);
    }

    private void OnPrimaryClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogChoice.Primary);
    }

    private void OnSecondaryClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogChoice.Secondary);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogChoice.Cancel);
    }
}
