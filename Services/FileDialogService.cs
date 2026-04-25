using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PureSFTP.Services;

public sealed class FileDialogService : IFileDialogService
{
    private IStorageProvider? _storageProvider;

    public void AttachStorageProvider(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async Task<string?> PickUploadFileAsync()
    {
        if (_storageProvider is null)
        {
            return null;
        }

        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a local file to upload",
            AllowMultiple = false,
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PickDownloadTargetFileAsync(string suggestedFileName)
    {
        if (_storageProvider is null)
        {
            return null;
        }

        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Choose where to save the download",
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true,
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickLocalFolderAsync(string title)
    {
        if (_storageProvider is null)
        {
            return null;
        }

        var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.FirstOrDefault()?.TryGetLocalPath();
    }
}
