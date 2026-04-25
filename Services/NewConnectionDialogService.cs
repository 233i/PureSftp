using System.Threading.Tasks;
using Avalonia.Controls;
using PureSFTP.Models;
using PureSFTP.ViewModels;
using PureSFTP.Views;

namespace PureSFTP.Services;

public sealed class NewConnectionDialogService : INewConnectionDialogService
{
    private Window? _owner;

    public void AttachOwner(Window owner)
    {
        _owner = owner;
    }

    public async Task<SavedConnection?> ShowAsync(NewConnectionViewModel viewModel)
    {
        if (_owner is null)
        {
            return null;
        }

        var window = new NewConnectionWindow
        {
            DataContext = viewModel,
        };

        return await window.ShowDialog<SavedConnection?>(_owner);
    }
}
