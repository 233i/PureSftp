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

    public string Permissions { get; init; } = string.Empty;

    public int UserId { get; init; }

    public int GroupId { get; init; }

    public bool IsHidden => !IsParentShortcut && Name.StartsWith(".", StringComparison.Ordinal);

    public string PermissionsText => IsParentShortcut ? string.Empty : Permissions;

    public string OwnerText => IsParentShortcut ? string.Empty : UserId.ToString();

    public string GroupText => IsParentShortcut ? string.Empty : GroupId.ToString();

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
            ? "↑"
            : IsDirectory
                ? "■"
                : "●";

    public bool ShowFolderIcon => IsDirectory && !IsParentShortcut;

    public bool ShowFileIcon => !IsDirectory && !IsParentShortcut;

    public bool ShowParentIcon => IsParentShortcut;

    public string TypeDotColor =>
        IsParentShortcut
            ? "#8A9AAC"
            : IsDirectory
                ? "#D99625"
                : "#2C7BE5";

    public string TypeBadgeBackground =>
        IsParentShortcut
            ? "#EEF2F6"
            : IsDirectory
                ? "#FFF4DF"
                : "#EAF2FF";

    public string TypeBadgeForeground =>
        IsParentShortcut
            ? "#6A7F93"
            : IsDirectory
                ? "#8A5A0A"
                : "#1F5FBF";

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
            Permissions = string.Empty,
            UserId = 0,
            GroupId = 0,
        };
    }
}
