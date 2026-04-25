using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PureSFTP.ViewModels;

public sealed partial class ToastMessageViewModel : ViewModelBase
{
    public ToastMessageViewModel(string message)
    {
        Id = Guid.NewGuid();
        Message = message;
    }

    [ObservableProperty]
    private bool isShown;

    [ObservableProperty]
    private bool isClosing;

    public Guid Id { get; }

    public string Message { get; }

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
    }
}
