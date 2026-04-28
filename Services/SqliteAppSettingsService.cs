using System;
using System.Globalization;
using Microsoft.Data.Sqlite;
using PureSFTP.Models;

namespace PureSFTP.Services;

public sealed class SqliteAppSettingsService : IAppSettingsService
{
    private const string LanguageKey = "language";
    private const string ThemeModeKey = "theme_mode";
    private const string CustomGradientStartColorKey = "custom_gradient_start_color";
    private const string CustomGradientEndColorKey = "custom_gradient_end_color";
    private readonly string _databasePath;

    public SqliteAppSettingsService(string databasePath)
    {
        _databasePath = databasePath;
    }

    public AppSettings Load()
    {
        var settings = CreateDefaultSettings();
        var languageValue = ReadValue(LanguageKey);
        var themeModeValue = ReadValue(ThemeModeKey);
        var customGradientStartColorValue = ReadValue(CustomGradientStartColorKey);
        var customGradientEndColorValue = ReadValue(CustomGradientEndColorKey);

        return new AppSettings
        {
            Language = Enum.TryParse<AppLanguage>(languageValue, out var language)
                ? language
                : settings.Language,
            ThemeMode = Enum.TryParse<AppThemeMode>(themeModeValue, out var themeMode)
                ? themeMode
                : settings.ThemeMode,
            CustomGradientStartColor = string.IsNullOrWhiteSpace(customGradientStartColorValue)
                ? settings.CustomGradientStartColor
                : customGradientStartColorValue,
            CustomGradientEndColor = string.IsNullOrWhiteSpace(customGradientEndColorValue)
                ? settings.CustomGradientEndColor
                : customGradientEndColorValue,
        };
    }

    public void Save(AppSettings settings)
    {
        WriteValue(LanguageKey, settings.Language.ToString());
        WriteValue(ThemeModeKey, settings.ThemeMode.ToString());
        WriteValue(CustomGradientStartColorKey, settings.CustomGradientStartColor);
        WriteValue(CustomGradientEndColorKey, settings.CustomGradientEndColor);
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
            ThemeMode = AppThemeMode.System,
            CustomGradientStartColor = "#0F172A",
            CustomGradientEndColor = "#2563EB",
        };
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }
}
