using System;
using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    ES,
    EN
}

public class LocalitzationManager : Singleton<LocalitzationManager>
{
    [Header("CSV Localization File (placed in Resources folder)")]
    public TextAsset localizationCSV;

    private Dictionary<string, Dictionary<Language, string>> localizedText = new Dictionary<string, Dictionary<Language, string>>();

    public Language CurrentLanguage { get; private set; } = Language.EN;

    public static event Action OnLanguageChanged;

    protected override void Awake()
    {
        base.Awake();

        SetLanguageFromSystem();

        if (localizationCSV != null)
            LoadCSV(localizationCSV);
        else
            LogManager.Log("CSV file not assigned!", LogType.Localitzation);
    }

    private void SetLanguageFromSystem()
    {
        if (ConfigManager.Instance != null)
        {
            int langIndex = ConfigManager.Instance.Data.language;

            if (langIndex >= 0 && Enum.IsDefined(typeof(Language), langIndex))
            {
                CurrentLanguage = (Language)langIndex;
                LogManager.LogDebugOnly($"Language set from config: {CurrentLanguage}", LogType.Localitzation);
                return;
            }
        }

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Spanish:
                CurrentLanguage = Language.ES;
                break;
            case SystemLanguage.English:
                CurrentLanguage = Language.EN;
                break;
            default:
                CurrentLanguage = Language.EN;
                break;
        }

        LogManager.LogDebugOnly($"System language detected: {Application.systemLanguage}, using {CurrentLanguage}", LogType.Localitzation);

        ConfigManager.Instance.Data.language = (int)CurrentLanguage;
        ConfigManager.Instance.SaveConfig();
    }

    private void LoadCSV(TextAsset csvFile)
    {
        localizedText.Clear();

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            LogManager.Log("CSV is empty or too short.", LogType.Localitzation);
            return;
        }

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');

            if (fields.Length != headers.Length)
            {
                LogManager.Log($"Line {i} has incorrect number of columns.", LogType.Localitzation);
                continue;
            }

            string key = fields[0].Trim();
            var translations = new Dictionary<Language, string>();

            for (int j = 1; j < headers.Length; j++)
            {
                if (Enum.TryParse<Language>(headers[j].Trim(), out var lang))
                    translations[lang] = fields[j].Trim();
            }

            localizedText[key] = translations;
        }

        LogManager.LogDebugOnly("CSV loaded, " + localizedText.Count + " keys.", LogType.Localitzation);
    }

    public string GetKey(string key)
    {
        if (localizedText.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(CurrentLanguage, out var text))
                return text.Replace(";", ",");
        }

        LogManager.Log($"Key '{key}' not found for language '{CurrentLanguage}'", LogType.Localitzation);
        return key;
    }

    public void SetLanguage(Language newLang)
    {
        CurrentLanguage = newLang;

        OnLanguageChanged?.Invoke();

        LogManager.LogDebugOnly($"Language manually set to {CurrentLanguage}", LogType.Localitzation);
    }
}
