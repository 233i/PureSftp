using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public async Task UploadFileAsync(string localPath, string remoteDirectory, IProgress<TransferProgress>? progress = null)
    {
        await Task.Run(() =>
        {
            var client = GetClient();
            var remotePath = Utilities.RemotePathHelper.Combine(remoteDirectory, Path.GetFileName(localPath));
            var totalBytes = new FileInfo(localPath).Length;

            using var fileStream = File.OpenRead(localPath);
            client.UploadFile(fileStream, remotePath, true, bytes =>
            {
                progress?.Report(new TransferProgress((long)bytes, totalBytes));
            });
        });
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
        };
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
