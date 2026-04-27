using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PureSFTP.ViewModels;

public sealed partial class ToastMessageViewModel : ViewModelBase
{
    private readonly Action? _cancelAction;

    public ToastMessageViewModel(string message, bool isProgressVisible = false, string cancelText = "", Action? cancelAction = null)
    {
        Id = Guid.NewGuid();
        this.message = message;
        this.isProgressVisible = isProgressVisible;
        this.cancelText = cancelText;
        _cancelAction = cancelAction;
        isCancelable = cancelAction is not null;
    }

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isShown;

    [ObservableProperty]
    private bool isClosing;

    [ObservableProperty]
    private bool isProgressVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelTransferCommand))]
    private bool isCancelable;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string cancelText = string.Empty;

    public Guid Id { get; }

    public string ProgressText => $"{Progress:0}%";

    public bool IsActionVisible => IsCancelable && !IsProgressVisible;

    public double ToastOpacity => IsShown && !IsClosing ? 1 : 0;

    public Thickness ToastMargin => IsShown && !IsClosing ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, -360, 8);

    partial void OnIsShownChanged(bool value)
    {
        OnPropertyChanged(nameof(ToastOpacity));
        OnPropertyChanged(nameof(ToastMargin));
    }

    partial void OnIsClosingChanged(bool value)
    {
        OnPropertyChanged(nameof(ToastOpacity));
        OnPropertyChanged(nameof(ToastMargin));
        CancelTransferCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsProgressVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsActionVisible));
    }

    partial void OnIsCancelableChanged(bool value)
    {
        OnPropertyChanged(nameof(IsActionVisible));
    }

    partial void OnProgressChanged(double value)
    {
        OnPropertyChanged(nameof(ProgressText));
    }

    private bool CanCancelTransfer() => IsCancelable && !IsClosing;

    [RelayCommand(CanExecute = nameof(CanCancelTransfer))]
    private void CancelTransfer()
    {
        IsCancelable = false;
        _cancelAction?.Invoke();
    }
}
