using System;
using System.Globalization;
using Microsoft.Data.Sqlite;
using PureSFTP.Models;

namespace PureSFTP.Services;

public sealed class SqliteAppSettingsService : IAppSettingsService
{
    private const string LanguageKey = "language";
    private readonly string _databasePath;

    public SqliteAppSettingsService(string databasePath)
    {
        _databasePath = databasePath;
    }

    public AppSettings Load()
    {
        var languageValue = ReadValue(LanguageKey);
        if (Enum.TryParse<AppLanguage>(languageValue, out var language))
        {
            return new AppSettings
            {
                Language = language,
            };
        }

        return CreateDefaultSettings();
    }

    public void Save(AppSettings settings)
    {
        WriteValue(LanguageKey, settings.Language.ToString());
    }

    private string? ReadValue(string key)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM app_settings WHERE key = $key LIMIT 1;";
        command.Parameters.AddWithValue("$key", key);

        return command.ExecuteScalar() as string;
    }

    private void WriteValue(string key, string value)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO app_settings (key, value)
            VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private static AppSettings CreateDefaultSettings()
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.ChineseSimplified
            : AppLanguage.English;

        return new AppSettings
        {
            Language = language,
        };
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }
}
