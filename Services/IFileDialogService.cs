using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PureSFTP.Services;

public interface IFileDialogService
{
    void AttachStorageProvider(IStorageProvider storageProvider);

    Task<string?> PickUploadFileAsync();

    Task<string?> PickDownloadTargetFileAsync(string suggestedFileName);

    Task<string?> PickLocalFolderAsync(string title);
}
