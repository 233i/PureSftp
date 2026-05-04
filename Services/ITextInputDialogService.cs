using System.Threading.Tasks;
using Avalonia.Controls;

namespace PureSFTP.Services;

public interface ITextInputDialogService
{
    void AttachOwner(Window owner);

    Task<string?> ShowAsync(string title, string placeholder);

    Task<string?> ShowAsync(string title, string placeholder, string initialValue);
}
