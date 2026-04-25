using PureSFTP.Models;

namespace PureSFTP.Services;

public interface IAppSettingsService
{
    AppSettings Load();

    void Save(AppSettings settings);
}
