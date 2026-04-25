using System;
using PureSFTP.Utilities;

namespace PureSFTP.Models;

public sealed class RemoteItem
{
    public required string Name { get; init; }

    public required string FullPath { get; init; }

    public bool IsDirectory { get; init; }

    public bool IsParentShortcut { get; init; }

    public long Size { get; init; }

    public DateTimeOffset ModifiedAt { get; init; }

    public string ItemType =>
        IsParentShortcut
            ? ".."
            : IsDirectory
                ? "DIR"
                : "FILE";

    public string SizeText => IsDirectory || IsParentShortcut ? "--" : FileSizeFormatter.Format(Size);

    public string ModifiedText => IsParentShortcut ? string.Empty : ModifiedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string IconText =>
        IsParentShortcut
            ? ".."
            : IsDirectory
                ? "DIR"
                : "FILE";

    public static RemoteItem CreateParentShortcut(string parentPath)
    {
        return new RemoteItem
        {
            Name = "..",
            FullPath = parentPath,
            IsDirectory = true,
            IsParentShortcut = true,
            Size = 0,
            ModifiedAt = DateTimeOffset.MinValue,
        };
    }
}
