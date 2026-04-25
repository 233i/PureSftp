using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using PureSFTP.Models;
using PureSFTP.Utilities;

namespace PureSFTP.ViewModels;

public partial class RemoteBrowserViewModel : ViewModelBase
{
    public RemoteBrowserViewModel()
    {
        Items = new ObservableCollection<RemoteItem>();
    }

    public ObservableCollection<RemoteItem> Items { get; }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string currentPath = "/";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private RemoteItem? selectedItem;

    public void ReplaceItems(IEnumerable<RemoteItem> items)
    {
        Items.Clear();
        SelectedItem = null;

        if (!string.Equals(CurrentPath, "/", StringComparison.Ordinal))
        {
            Items.Add(RemoteItem.CreateParentShortcut(RemotePathHelper.GetParent(CurrentPath)));
        }

        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public void Reset()
    {
        CurrentPath = "/";
        ReplaceItems([]);
    }
}
