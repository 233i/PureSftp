using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PureSFTP.Models;

namespace PureSFTP.Services;

public interface ISftpClientService : IAsyncDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(SftpConnectionOptions options);

    Task TestConnectionAsync(SftpConnectionOptions options);

    Task DisconnectAsync();

    Task<string> GetWorkingDirectoryAsync();

    Task<IReadOnlyList<RemoteItem>> ListDirectoryAsync(string remotePath);

    Task<bool> ExistsAsync(string remotePath);

    Task<long> GetSizeAsync(RemoteItem item);

    Task UploadFileAsync(string localPath, string remoteDirectory, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default);

    Task UploadDirectoryAsync(string localDirectoryPath, string remoteDirectory, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default);

    Task DownloadFileAsync(string remoteFilePath, string localPath, IProgress<TransferProgress>? progress = null);

    Task DownloadDirectoryAsync(string remoteDirectoryPath, string localDirectoryPath, IProgress<TransferProgress>? progress = null);

    Task DeleteAsync(RemoteItem item);

    Task CreateDirectoryAsync(string remoteDirectoryPath);

    Task RenameAsync(string currentRemotePath, string newRemotePath);

    Task ChangePermissionsAsync(string remotePath, short permissions);
}
