using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;

[System.Serializable]
public class ConfigData
{
    public string _advert = "Hackers feel free to modify whatever you want at your own risk"; // Just a message for hackers

    // Lobby preferences
    public int gamemode = 1; // 1 = 1v1, 2 = 2v2, 3 = 3v3, 4 = 4v4

    // Gameplay settings
    public int language = -1; // -1 = Unsetted, 0 = Spanish, 1 = English
    public bool toggleAim = false;
    public bool autoReload = false;
    public bool dynamicFOV = true;
    public bool showDamageNumbers = true;

    // Audio settings
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
    public bool muteWhenUnfocused = false;

    // Video settings
    public int fullscreenMode = 0; // 0 = Fullscreen, 1 = Exclusive Fullscreen, 2 = Maximized Window, 3 = Windowed
    public int resolutionWidth = -1; // -1 = Native/Unsetted
    public int resolutionHeight = -1; // -1 = Native/Unsetted
    public int graphicsQuality = 3; // 0 = Low, 1 = Medium, 2 = High, 3 = Ultra
    public bool vSync = false;
    public int limitFPS = 0; // 0 = Unlimited, 1 = 30, 2 = 60, 3 = 120, 4 = 144, 5 = 240, 6 = 360

    // Controls settings
    public float mouseSensitivity = 0.2f;
    public float controllerSensitivity = 280f;
    public bool invertY = false;

    // Crosshair settings
    public int crosshairStyle = 0; // 0 = Default(006), 1 = Dot(001), 2 = Plus(002), 3 = DefaultBig(005), 4 = Dots(008), 5 = Eye(140), 6 = Mounth(143), 7 = MounthSecond(144),  8 = Cross(161)
    public int crosshairColor = 0; // 0 = Red, 1 = Green, 2 = Blue, 3 = Yellow, 4 = Cyan, 5 = Pink, 6 = White
}

public class ConfigManager : Singleton<ConfigManager>
{
    private string configDirectory;
    private string configPath;
    public ConfigData Data { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        configDirectory = Path.Combine(Application.persistentDataPath, "data");
        configPath = Path.Combine(configDirectory, "config.json");

        LoadConfig();
    }

    private void LoadConfig()
    {
        if (!Directory.Exists(configDirectory))
            Directory.CreateDirectory(configDirectory);

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);

            JObject currentJson = JObject.Parse(json);

            JObject defaultJson = JObject.FromObject(new ConfigData());

            defaultJson.Merge(currentJson, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Merge
            });

            Data = defaultJson.ToObject<ConfigData>();

            File.WriteAllText(configPath, JsonConvert.SerializeObject(Data, Formatting.Indented));

            LogManager.LogDebugOnly("Config loaded and merged with defaults.", LogType.System);
        }
        else
        {
            Data = new ConfigData();
            SaveConfig();
            LogManager.LogDebugOnly("No config found. Created default config.", LogType.System);
        }
    }

    public void SaveConfig()
    {
        string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
        File.WriteAllText(configPath, json);
        LogManager.LogDebugOnly("Config saved.", LogType.System);
    }
}