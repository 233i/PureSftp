using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PureSFTP.Models;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace PureSFTP.Services;

public sealed class SftpClientService : ISftpClientService
{
    private SftpClient? _client;

    public bool IsConnected => _client?.IsConnected == true;

    public async Task ConnectAsync(SftpConnectionOptions options)
    {
        await DisconnectAsync();

        var client = new SftpClient(options.Host, options.Port, options.Username, options.Password);

        try
        {
            await Task.Run(client.Connect);
            _client = client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public async Task TestConnectionAsync(SftpConnectionOptions options)
    {
        await Task.Run(() =>
        {
            using var client = new SftpClient(options.Host, options.Port, options.Username, options.Password);
            client.Connect();
            client.Disconnect();
        });
    }

    public async Task DisconnectAsync()
    {
        if (_client is null)
        {
            return;
        }

        var client = _client;
        _client = null;

        await Task.Run(() =>
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }

            client.Dispose();
        });
    }

    public async Task<string> GetWorkingDirectoryAsync()
    {
        return await Task.Run(() => GetClient().WorkingDirectory);
    }

    public async Task<IReadOnlyList<RemoteItem>> ListDirectoryAsync(string remotePath)
    {
        return await Task.Run(() =>
        {
            var client = GetClient();

            return client.ListDirectory(remotePath)
                .Where(item => item.Name is not "." and not "..")
                .Select(item => MapRemoteItem(item))
                .OrderBy(item => item.IsDirectory ? 0 : 1)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });
    }

    public async Task<bool> ExistsAsync(string remotePath)
    {
        return await Task.Run(() => GetClient().Exists(remotePath));
    }

    public async Task<long> GetSizeAsync(RemoteItem item)
    {
        return await Task.Run(() =>
        {
            var client = GetClient();

            if (item.IsDirectory)
            {
                return GetDirectorySize(client, item.FullPath);
            }

            return client.Get(item.FullPath).Attributes.Size;
        });
    }

    public async Task UploadFileAsync(string localPath, string remoteDirectory, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var client = GetClient();
            var remotePath = Utilities.RemotePathHelper.Combine(remoteDirectory, Path.GetFileName(localPath));
            var tempRemotePath = CreateUploadTempPath(remotePath);
            var totalBytes = new FileInfo(localPath).Length;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var fileStream = File.OpenRead(localPath);
                client.UploadFile(fileStream, tempRemotePath, true, bytes =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(new TransferProgress((long)bytes, totalBytes));
                });

                cancellationToken.ThrowIfCancellationRequested();
                ReplaceRemoteFile(client, tempRemotePath, remotePath);
                progress?.Report(new TransferProgress(totalBytes, totalBytes));
            }
            catch
            {
                DeleteRemoteFileIfExists(client, tempRemotePath);
                throw;
            }
        }, cancellationToken);
    }

    public async Task UploadDirectoryAsync(string localDirectoryPath, string remoteDirectory, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var client = GetClient();
            var directory = new DirectoryInfo(localDirectoryPath);
            if (!directory.Exists)
            {
                throw new DirectoryNotFoundException(localDirectoryPath);
            }

            var totalBytes = GetLocalDirectorySize(directory);
            var uploadedBytes = 0L;
            var remoteRootPath = Utilities.RemotePathHelper.Combine(remoteDirectory, directory.Name);
            var remoteRootExisted = client.Exists(remoteRootPath);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnsureRemoteDirectory(client, remoteRootPath);
                UploadDirectoryCore(client, directory, remoteRootPath, totalBytes, ref uploadedBytes, progress, cancellationToken);
                progress?.Report(new TransferProgress(uploadedBytes, totalBytes));
            }
            catch (OperationCanceledException) when (!remoteRootExisted)
            {
                DeleteDirectoryIfExists(client, remoteRootPath);
                throw;
            }
        }, cancellationToken);
    }

    public async Task DownloadFileAsync(string remoteFilePath, string localPath, IProgress<TransferProgress>? progress = null)
    {
        await Task.Run(() =>
        {
            var client = GetClient();
            var targetDirectory = Path.GetDirectoryName(localPath);
            var remoteFile = client.Get(remoteFilePath);
            var totalBytes = remoteFile.Attributes.Size;

            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            using var outputStream = File.Create(localPath);
            client.DownloadFile(remoteFilePath, outputStream, bytes =>
            {
                progress?.Report(new TransferProgress((long)bytes, totalBytes));
            });
        });
    }

    public async Task DownloadDirectoryAsync(string remoteDirectoryPath, string localDirectoryPath, IProgress<TransferProgress>? progress = null)
    {
        await Task.Run(() =>
        {
            var client = GetClient();
            var totalBytes = GetDirectorySize(client, remoteDirectoryPath);
            var downloadedBytes = 0L;
            DownloadDirectoryCore(client, remoteDirectoryPath, localDirectoryPath, totalBytes, ref downloadedBytes, progress);
        });
    }

    public async Task DeleteAsync(RemoteItem item)
    {
        await Task.Run(() =>
        {
            var client = GetClient();

            if (item.IsDirectory)
            {
                DeleteDirectoryCore(client, item.FullPath);
                return;
            }

            client.DeleteFile(item.FullPath);
        });
    }

    public async Task CreateDirectoryAsync(string remoteDirectoryPath)
    {
        await Task.Run(() =>
        {
            var client = GetClient();

            if (!client.Exists(remoteDirectoryPath))
            {
                client.CreateDirectory(remoteDirectoryPath);
            }
        });
    }

    public async Task RenameAsync(string currentRemotePath, string newRemotePath)
    {
        await Task.Run(() => GetClient().RenameFile(currentRemotePath, newRemotePath));
    }

    public async Task ChangePermissionsAsync(string remotePath, short permissions)
    {
        await Task.Run(() => GetClient().ChangePermissions(remotePath, permissions));
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    private static RemoteItem MapRemoteItem(ISftpFile item)
    {
        return new RemoteItem
        {
            Name = item.Name,
            FullPath = item.FullName,
            IsDirectory = item.IsDirectory,
            Size = item.IsDirectory ? 0 : item.Attributes.Size,
            ModifiedAt = new DateTimeOffset(item.LastWriteTimeUtc),
            Permissions = FormatPermissions(item),
            UserId = item.UserId,
            GroupId = item.GroupId,
        };
    }

    private static string FormatPermissions(ISftpFile item)
    {
        var type = item.IsDirectory ? 'd' : item.IsSymbolicLink ? 'l' : '-';
        return string.Create(10, (type, item.Attributes), static (buffer, state) =>
        {
            buffer[0] = state.type;
            buffer[1] = state.Attributes.OwnerCanRead ? 'r' : '-';
            buffer[2] = state.Attributes.OwnerCanWrite ? 'w' : '-';
            buffer[3] = state.Attributes.OwnerCanExecute ? 'x' : '-';
            buffer[4] = state.Attributes.GroupCanRead ? 'r' : '-';
            buffer[5] = state.Attributes.GroupCanWrite ? 'w' : '-';
            buffer[6] = state.Attributes.GroupCanExecute ? 'x' : '-';
            buffer[7] = state.Attributes.OthersCanRead ? 'r' : '-';
            buffer[8] = state.Attributes.OthersCanWrite ? 'w' : '-';
            buffer[9] = state.Attributes.OthersCanExecute ? 'x' : '-';
        });
    }

    private static long GetDirectorySize(SftpClient client, string remoteDirectoryPath)
    {
        var totalBytes = 0L;

        foreach (var item in client.ListDirectory(remoteDirectoryPath).Where(entry => entry.Name is not "." and not ".."))
        {
            totalBytes += item.IsDirectory
                ? GetDirectorySize(client, item.FullName)
                : item.Attributes.Size;
        }

        return totalBytes;
    }

    private static long GetLocalDirectorySize(DirectoryInfo directory)
    {
        var totalBytes = 0L;

        foreach (var file in directory.EnumerateFiles().Where(file => !file.Attributes.HasFlag(FileAttributes.ReparsePoint)))
        {
            totalBytes += file.Length;
        }

        foreach (var childDirectory in directory.EnumerateDirectories().Where(child => !child.Attributes.HasFlag(FileAttributes.ReparsePoint)))
        {
            totalBytes += GetLocalDirectorySize(childDirectory);
        }

        return totalBytes;
    }

    private static void UploadDirectoryCore(
        SftpClient client,
        DirectoryInfo localDirectory,
        string remoteDirectoryPath,
        long totalBytes,
        ref long uploadedBytes,
        IProgress<TransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureRemoteDirectory(client, remoteDirectoryPath);

        foreach (var childDirectory in localDirectory.EnumerateDirectories().Where(child => !child.Attributes.HasFlag(FileAttributes.ReparsePoint)))
        {
            var childRemotePath = Utilities.RemotePathHelper.Combine(remoteDirectoryPath, childDirectory.Name);
            UploadDirectoryCore(client, childDirectory, childRemotePath, totalBytes, ref uploadedBytes, progress, cancellationToken);
        }

        foreach (var file in localDirectory.EnumerateFiles().Where(file => !file.Attributes.HasFlag(FileAttributes.ReparsePoint)))
        {
            var remoteFilePath = Utilities.RemotePathHelper.Combine(remoteDirectoryPath, file.Name);
            var tempRemoteFilePath = CreateUploadTempPath(remoteFilePath);
            var fileStartBytes = uploadedBytes;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var fileStream = file.OpenRead();
                client.UploadFile(fileStream, tempRemoteFilePath, true, bytes =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(new TransferProgress(fileStartBytes + (long)bytes, totalBytes));
                });

                cancellationToken.ThrowIfCancellationRequested();
                ReplaceRemoteFile(client, tempRemoteFilePath, remoteFilePath);
            }
            catch
            {
                DeleteRemoteFileIfExists(client, tempRemoteFilePath);
                throw;
            }

            uploadedBytes += file.Length;
            progress?.Report(new TransferProgress(uploadedBytes, totalBytes));
        }
    }

    private static string CreateUploadTempPath(string remotePath) => $"{remotePath}.puresftp-uploading-{Guid.NewGuid():N}";

    private static void ReplaceRemoteFile(SftpClient client, string tempRemotePath, string remotePath)
    {
        if (client.Exists(remotePath))
        {
            client.DeleteFile(remotePath);
        }

        client.RenameFile(tempRemotePath, remotePath);
    }

    private static void DeleteRemoteFileIfExists(SftpClient client, string remotePath)
    {
        if (client.Exists(remotePath))
        {
            client.DeleteFile(remotePath);
        }
    }

    private static void DeleteDirectoryIfExists(SftpClient client, string remoteDirectoryPath)
    {
        if (client.Exists(remoteDirectoryPath))
        {
            DeleteDirectoryCore(client, remoteDirectoryPath);
        }
    }

    private static void EnsureRemoteDirectory(SftpClient client, string remoteDirectoryPath)
    {
        if (!client.Exists(remoteDirectoryPath))
        {
            client.CreateDirectory(remoteDirectoryPath);
        }
    }

    private static void DownloadDirectoryCore(
        SftpClient client,
        string remoteDirectoryPath,
        string localDirectoryPath,
        long totalBytes,
        ref long downloadedBytes,
        IProgress<TransferProgress>? progress)
    {
        // Mirror the remote tree locally so the lightweight UI can stay focused on orchestration.
        Directory.CreateDirectory(localDirectoryPath);

        foreach (var item in client.ListDirectory(remoteDirectoryPath).Where(entry => entry.Name is not "." and not ".."))
        {
            var targetPath = Path.Combine(localDirectoryPath, item.Name);

            if (item.IsDirectory)
            {
                DownloadDirectoryCore(client, item.FullName, targetPath, totalBytes, ref downloadedBytes, progress);
                continue;
            }

            using var outputStream = File.Create(targetPath);
            var fileStartBytes = downloadedBytes;
            client.DownloadFile(item.FullName, outputStream, bytes =>
            {
                progress?.Report(new TransferProgress(fileStartBytes + (long)bytes, totalBytes));
            });
            downloadedBytes += item.Attributes.Size;
            progress?.Report(new TransferProgress(downloadedBytes, totalBytes));
        }
    }

    private static void DeleteDirectoryCore(SftpClient client, string remoteDirectoryPath)
    {
        // Recursively remove nested content first because many servers reject non-empty directory deletion.
        foreach (var item in client.ListDirectory(remoteDirectoryPath).Where(entry => entry.Name is not "." and not ".."))
        {
            if (item.IsDirectory)
            {
                DeleteDirectoryCore(client, item.FullName);
            }
            else
            {
                client.DeleteFile(item.FullName);
            }
        }

        client.DeleteDirectory(remoteDirectoryPath);
    }

    private SftpClient GetClient()
    {
        return _client?.IsConnected == true
            ? _client
            : throw new InvalidOperationException("Not connected to an SFTP server.");
    }
}
