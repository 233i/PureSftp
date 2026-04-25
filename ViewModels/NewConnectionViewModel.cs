using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PureSFTP.Models;
using PureSFTP.Services;

namespace PureSFTP.ViewModels;

public partial class NewConnectionViewModel : ViewModelBase
{
    private readonly ISftpClientService _sftpClientService;
    private readonly ILocalizationService _localizationService;
    private string portText = "22";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string host = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isTesting;

    [ObservableProperty]
    private string testMessage = string.Empty;

    public NewConnectionViewModel(ISftpClientService sftpClientService, ILocalizationService localizationService)
    {
        _sftpClientService = sftpClientService;
        _localizationService = localizationService;
    }

    public event EventHandler? SaveRequested;

    public SavedConnection? Result { get; private set; }

    public string TitleText => T("NewConnectionTitle");

    public string DescriptionText => T("NewConnectionDescription");

    public string NameLabel => T("ConnectionNameLabel");

    public string HostLabel => T("HostLabel");

    public string PortLabel => T("PortLabel");

    public string UsernameLabel => T("UsernameLabel");

    public string PasswordLabel => T("PasswordLabel");

    public string TestButtonText => T("TestConnectionButton");

    public string SaveButtonText => T("SaveConnectionButton");

    public string PortText
    {
        get => portText;
        set
        {
            var sanitizedValue = SanitizePort(value);
            if (SetProperty(ref portText, sanitizedValue))
            {
                OnPropertyChanged(nameof(Port));
                TestConnectionCommand.NotifyCanExecuteChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int Port => int.TryParse(PortText, out var port) ? port : 0;

    private bool IsReady =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Host) &&
        Port is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(Username);

    private bool CanTestConnection() => !IsTesting && IsReady;

    private bool CanSave() => IsReady;

    [RelayCommand(CanExecute = nameof(CanTestConnection))]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsTesting = true;
            TestMessage = T("StatusTestingConnection");
            await _sftpClientService.TestConnectionAsync(ToOptions());
            TestMessage = T("StatusTestSucceeded");
        }
        catch (Exception exception)
        {
            TestMessage = exception.Message;
        }
        finally
        {
            IsTesting = false;
            TestConnectionCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        Result = new SavedConnection
        {
            Name = Name.Trim(),
            Host = Host.Trim(),
            Port = Port,
            Username = Username.Trim(),
            Password = Password,
        };
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private SftpConnectionOptions ToOptions()
    {
        return new SftpConnectionOptions
        {
            Host = Host.Trim(),
            Port = Port,
            Username = Username.Trim(),
            Password = Password,
        };
    }

    private string T(string key, params object[] args) => _localizationService.Get(key, args);

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
}
