using System.Threading.Tasks;
using Avalonia.Controls;
using PureSFTP.Models;
using PureSFTP.Views;

namespace PureSFTP.Services;

public sealed class ChoiceDialogService : IChoiceDialogService
{
    private Window? _owner;

    public void AttachOwner(Window owner)
    {
        _owner = owner;
    }

    public async Task<DialogChoice> ShowAsync(
        string title,
        string message,
        string primaryText,
        string cancelText,
        string secondaryText = "")
    {
        if (_owner is null)
        {
            return DialogChoice.Cancel;
        }

        var window = new ChoiceDialogWindow(title, message, primaryText, cancelText, secondaryText);
        return await window.ShowDialog<DialogChoice>(_owner);
    }
}
