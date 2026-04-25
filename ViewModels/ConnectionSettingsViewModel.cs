using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PureSFTP.Models;

namespace PureSFTP.ViewModels;

public partial class ConnectionSettingsViewModel : ViewModelBase
{
    private string portText = "22";

    [ObservableProperty]
    private string host = string.Empty;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string startupPath = string.Empty;

    public string PortText
    {
        get => portText;
        set
        {
            var sanitizedValue = SanitizePort(value);
            if (SetProperty(ref portText, sanitizedValue))
            {
                OnPropertyChanged(nameof(Port));
                OnPropertyChanged(nameof(IsReady));
            }
        }
    }

    public int Port => int.TryParse(PortText, out var port) ? port : 0;

    public bool IsReady =>
        !string.IsNullOrWhiteSpace(Host) &&
        Port is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(Username);

    public SftpConnectionOptions ToOptions()
    {
        return new SftpConnectionOptions
        {
            Host = Host.Trim(),
            Port = Port,
            Username = Username.Trim(),
            Password = Password,
            StartupPath = StartupPath.Trim(),
        };
    }

    private static string SanitizePort(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var digits = new string(value.Where(char.IsDigit).Take(5).ToArray());
        if (!int.TryParse(digits, out var port))
        {
            return string.Empty;
        }

        return port > 65535 ? "65535" : digits;
    }

    partial void OnHostChanged(string value) => OnPropertyChanged(nameof(IsReady));

    partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(IsReady));

    partial void OnPasswordChanged(string value) => OnPropertyChanged(nameof(IsReady));

    partial void OnStartupPathChanged(string value) => OnPropertyChanged(nameof(IsReady));
}
