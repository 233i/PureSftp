using System.Threading.Tasks;
using Avalonia.Controls;
using PureSFTP.Models;

namespace PureSFTP.Services;

public interface IChoiceDialogService
{
    void AttachOwner(Window owner);

    Task<DialogChoice> ShowAsync(
        string title,
        string message,
        string primaryText,
        string cancelText,
        string secondaryText = "");
}
