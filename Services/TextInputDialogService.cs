using System.Threading.Tasks;
using Avalonia.Controls;
using PureSFTP.Views;

namespace PureSFTP.Services;

public sealed class TextInputDialogService : ITextInputDialogService
{
    private Window? _owner;

    public void AttachOwner(Window owner)
    {
        _owner = owner;
    }

    public async Task<string?> ShowAsync(string title, string placeholder)
    {
        if (_owner is null)
        {
            return null;
        }

        var window = new TextInputWindow(title, placeholder);
        return await window.ShowDialog<string?>(_owner);
    }
}
