using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PureSFTP.Services;

public interface IFileDialogService
{
    void AttachStorageProvider(IStorageProvider storageProvider);

    Task<string?> PickUploadFileAsync();

    Task<IReadOnlyList<string>> PickUploadFilesAsync();

    Task<string?> PickUploadFolderAsync(string title);

    Task<IReadOnlyList<string>> PickUploadFoldersAsync(string title);

    Task<string?> PickDownloadTargetFileAsync(string suggestedFileName);

    Task<string?> PickLocalFolderAsync(string title);
}
