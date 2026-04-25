using System.Threading.Tasks;
using Avalonia.Controls;
using PureSFTP.Models;
using PureSFTP.ViewModels;

namespace PureSFTP.Services;

public interface INewConnectionDialogService
{
    void AttachOwner(Window owner);

    Task<SavedConnection?> ShowAsync(NewConnectionViewModel viewModel);
}
