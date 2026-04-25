using System;

namespace PureSFTP.Utilities;

public static class RemotePathHelper
{
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim().Replace('\\', '/');

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized.TrimStart('/');
        }

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
        }

        return string.IsNullOrWhiteSpace(normalized) ? "/" : normalized;
    }

    public static string Combine(string directory, string name)
    {
        var normalizedDirectory = Normalize(directory);
        return normalizedDirectory == "/"
            ? $"/{name}"
            : $"{normalizedDirectory}/{name}";
    }

    public static string GetParent(string path)
    {
        var normalized = Normalize(path);

        if (string.Equals(normalized, "/", StringComparison.Ordinal))
        {
            return "/";
        }

        var lastSlashIndex = normalized.LastIndexOf('/');
        return lastSlashIndex <= 0 ? "/" : normalized[..lastSlashIndex];
    }

    public static string Resolve(string baseDirectory, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Normalize(baseDirectory);
        }

        return path.StartsWith("/", StringComparison.Ordinal)
            ? Normalize(path)
            : Combine(baseDirectory, path);
    }
}
