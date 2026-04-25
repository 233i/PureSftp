using System;
using PureSFTP.Models;

namespace PureSFTP.Services;

public interface ILocalizationService
{
    AppLanguage CurrentLanguage { get; }

    event EventHandler? LanguageChanged;

    void SetLanguage(AppLanguage language);

    string Get(string key);

    string Get(string key, params object[] args);
}
