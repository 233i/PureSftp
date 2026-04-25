namespace PureSFTP.Models;

public sealed class LanguageOption
{
    public LanguageOption(AppLanguage language, string displayName)
    {
        Language = language;
        DisplayName = displayName;
    }

    public AppLanguage Language { get; }

    public string DisplayName { get; }
}
