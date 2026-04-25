using Avalonia.Controls;
using Avalonia.Input;

namespace PureSFTP.Views;

public partial class TextInputWindow : Window
{
    public TextInputWindow()
    {
        InitializeComponent();
        Opened += (_, _) => InputTextBox.Focus();
    }

    public TextInputWindow(string title, string placeholder)
        : this()
    {
        Title = title;
        InputTextBox.PlaceholderText = placeholder;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Submit();
            e.Handled = true;
        }

        if (e.Key == Key.Escape)
        {
            Close(null);
            e.Handled = true;
        }
    }

    private void Submit()
    {
        Close(string.IsNullOrWhiteSpace(InputTextBox.Text) ? null : InputTextBox.Text.Trim());
    }
}
