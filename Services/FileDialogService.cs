using System.Collections.Generic;
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
        return (await PickUploadFilesAsync()).FirstOrDefault();
    }

    public async Task<IReadOnlyList<string>> PickUploadFilesAsync()
    {
        if (_storageProvider is null)
        {
            return [];
        }

        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a local file to upload",
            AllowMultiple = true,
        });

        return files
            .Select(file => file.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToList();
    }

    public async Task<string?> PickUploadFolderAsync(string title)
    {
        return (await PickUploadFoldersAsync(title)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<string>> PickUploadFoldersAsync(string title)
    {
        if (_storageProvider is null)
        {
            return [];
        }

        var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
        });

        return folders
            .Select(folder => folder.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToList();
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
