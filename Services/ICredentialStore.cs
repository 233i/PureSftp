using PureSFTP.Models;

namespace PureSFTP.Services;

public interface ICredentialStore
{
    string? ReadPassword(SavedConnection connection);

    bool SavePassword(SavedConnection connection, string password);

    void DeletePassword(SavedConnection connection);
}
