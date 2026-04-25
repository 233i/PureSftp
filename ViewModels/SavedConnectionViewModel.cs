using CommunityToolkit.Mvvm.ComponentModel;
using PureSFTP.Models;

namespace PureSFTP.ViewModels;

public partial class SavedConnectionViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isActive;

    public SavedConnectionViewModel(SavedConnection connection)
    {
        Connection = connection;
    }

    public SavedConnection Connection { get; }

    public long Id => Connection.Id;

    public string Name => Connection.Name;

    public string Endpoint => $"{Connection.Host}:{Connection.Port}";

    public string Username => Connection.Username;
}
