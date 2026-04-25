using System;
using System.IO;

namespace PureSFTP.Services;

public static class DatabasePathProvider
{
    public static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "PureSFTP", "puresftp.db");
    }
}
