using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using PureSFTP.Models;
using PureSFTP.Utilities;

namespace PureSFTP.ViewModels;

public partial class RemoteBrowserViewModel : ViewModelBase
{
    private IReadOnlyList<RemoteItem> currentItems = [];

    public RemoteBrowserViewModel()
    {
        Items = new ObservableCollection<RemoteItem>();
        SelectedItems = new ObservableCollection<RemoteItem>();
    }

    public ObservableCollection<RemoteItem> Items { get; }

    public ObservableCollection<RemoteItem> SelectedItems { get; }

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string currentPath = "/";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private RemoteItem? selectedItem;

    public RemoteSortColumn SortColumn { get; private set; } = RemoteSortColumn.Name;

    public bool IsSortAscending { get; private set; } = true;

    public void ReplaceItems(IEnumerable<RemoteItem> items)
    {
        currentItems = items.ToList();
        ApplyItems();
    }

    public void ReplaceSelectedItems(IEnumerable<RemoteItem> items)
    {
        SelectedItems.Clear();
        foreach (var item in items.Where(item => !item.IsParentShortcut))
        {
            SelectedItems.Add(item);
        }

        OnPropertyChanged(nameof(SelectedItems));
        OnPropertyChanged(nameof(SelectedItemCount));
    }

    public void SortBy(RemoteSortColumn sortColumn)
    {
        if (SortColumn == sortColumn)
        {
            IsSortAscending = !IsSortAscending;
        }
        else
        {
            SortColumn = sortColumn;
            IsSortAscending = true;
        }

        ApplyItems();
        OnPropertyChanged(nameof(SortColumn));
        OnPropertyChanged(nameof(IsSortAscending));
    }

    public void Reset()
    {
        CurrentPath = "/";
        currentItems = [];
        ApplyItems();
    }

    public int SelectedItemCount => SelectedItems.Count;

    private void ApplyItems()
    {
        Items.Clear();
        SelectedItem = null;
        ReplaceSelectedItems([]);

        if (!string.Equals(CurrentPath, "/", StringComparison.Ordinal))
        {
            Items.Add(RemoteItem.CreateParentShortcut(RemotePathHelper.GetParent(CurrentPath)));
        }

        foreach (var item in SortItems())
        {
            Items.Add(item);
        }
    }

    private IEnumerable<RemoteItem> SortItems()
    {
        var baseQuery = currentItems.OrderBy(item => item.IsDirectory ? 0 : 1);

        return SortColumn switch
        {
            RemoteSortColumn.Size => IsSortAscending
                ? baseQuery.ThenBy(item => item.Size).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                : baseQuery.ThenByDescending(item => item.Size).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            RemoteSortColumn.ModifiedAt => IsSortAscending
                ? baseQuery.ThenBy(item => item.ModifiedAt).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                : baseQuery.ThenByDescending(item => item.ModifiedAt).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase),
            _ => IsSortAscending
                ? baseQuery.ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                : baseQuery.ThenByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase),
        };
    }
}
