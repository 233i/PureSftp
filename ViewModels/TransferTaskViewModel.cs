using CommunityToolkit.Mvvm.ComponentModel;
using PureSFTP.Models;
using PureSFTP.Utilities;

namespace PureSFTP.ViewModels;

public partial class TransferTaskViewModel : ViewModelBase
{
    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private TransferTaskStatus status = TransferTaskStatus.Running;

    [ObservableProperty]
    private string? errorMessage;

    public TransferTaskViewModel(TransferTaskType type, string fileName)
    {
        Type = type;
        FileName = fileName;
    }

    public TransferTaskType Type { get; }

    public string FileName { get; }

    public string TypeText => Type == TransferTaskType.Upload ? "UPLOAD" : "DOWNLOAD";

    public string StatusText =>
        Status switch
        {
            TransferTaskStatus.Completed => "Done",
            TransferTaskStatus.Failed => "Failed",
            _ => "Running",
        };

    public string ProgressText => $"{Progress:0}%";

    public void ApplyProgress(TransferProgress transferProgress)
    {
        Progress = transferProgress.Percent;
        OnPropertyChanged(nameof(ProgressText));
    }

    public void Complete()
    {
        Progress = 100;
        Status = TransferTaskStatus.Completed;
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(StatusText));
    }

    public void Fail(string message)
    {
        Status = TransferTaskStatus.Failed;
        ErrorMessage = message;
        OnPropertyChanged(nameof(StatusText));
    }
}
