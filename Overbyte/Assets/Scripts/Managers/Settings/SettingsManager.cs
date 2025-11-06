using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Main Settings")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Sections")]
    [SerializeField] private List<ButtonComponent> sectionButtons;
    [SerializeField] private List<GameObject> sectionContainers;

    [Header("Activation Buttons")]
    [SerializeField] private GameObject openSettingsButtonLobby;
    [SerializeField] private GameObject openSettingsButtonESC;
    [SerializeField] private GameObject quitSettingsButton;
    [Space(70)]

    [Header("Gameplay Settings")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Toggle toggleAim;
    [SerializeField] private Toggle toggleAutoReload;
    [SerializeField] private Toggle dynamicFov;
    [SerializeField] private Toggle renderDamage;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolume;
    [SerializeField] private TextMeshProUGUI masterVolumePercentage;
    [SerializeField] private Slider bgmVolume;
    [SerializeField] private TextMeshProUGUI bgmVolumePercentage;
    [SerializeField] private Slider sfxVolume;
    [SerializeField] private TextMeshProUGUI sfxVolumePercentage;
    [SerializeField] private Toggle muteUnfocused;

    [Header("Video Settings")]
    [SerializeField] private TMP_Dropdown screenMode;
    [SerializeField] private TMP_Dropdown screenResolution;
    [SerializeField] private TMP_Dropdown graphicsQuality;
    [SerializeField] private Toggle vsync;
    [SerializeField] private TMP_Dropdown limitFPS;

    [Header("Controls Settings")]
    [SerializeField] private Button subtractMouseSensitivity;
    [SerializeField] private TextMeshProUGUI mouseSensitivityValue;
    [SerializeField] private Button addMouseSensitivity;
    [SerializeField] private Button subtractControlSensitivity;
    [SerializeField] private TextMeshProUGUI controlSensitivityValue;
    [SerializeField] private Button addControlSensitivity;
    [SerializeField] private Toggle invertY;

    [Header("Crosshair Settings")]
    [Header("Crosshair Images")]
    [SerializeField] public GameObject crosshairHUD;
    [SerializeField] private GameObject crosshairPreview;

    [Header("Crosshair Styles")]
    [SerializeField] private List<Sprite> crosshairSprites;
    [SerializeField] private List<Color> crosshairColors;

    [Header("Crosshair Options")]
    [SerializeField] private TMP_Dropdown crosshairStyleDropdown;
    [SerializeField] private TMP_Dropdown crosshairColorDropdown;

    private PlayerInput playerInput;
    private InputAction escAction;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        playerInput = PlayerManager.Instance.PlayerInput;

        SetupDropdowns();
        SetupToggles();
        SetupSliders();
        SetupSensitivity();

        for (int i = 0; i < sectionButtons.Count; i++)
        {
            int index = i;
            sectionButtons[i].GetComponent<Button>().onClick.AddListener(() => SwitchSection(index));
        }

        openSettingsButtonLobby.GetComponent<Button>().onClick.AddListener(() => SwitchSettings(true));
        openSettingsButtonESC.GetComponent<Button>().onClick.AddListener(() => SwitchSettings(true));
        quitSettingsButton.GetComponent<Button>().onClick.AddListener(() => SwitchSettings(false));

        escAction = playerInput.actions["ESC"];
        escAction.performed += ctx => SwitchSettings(false);
        escAction.Enable();
    }

    private void SwitchSection(int index)
    {
        for (int i = 0; i < sectionContainers.Count; i++)
        {
            bool isActive = i == index;
            sectionContainers[i].SetActive(isActive);
            sectionButtons[i].SetInteractable(!isActive);
        }
    }

    private void SetupDropdowns()
    {
        // =============================
        // CROSSHAIR STYLE SETUP
        // =============================
        crosshairStyleDropdown.onValueChanged.RemoveAllListeners();
        crosshairStyleDropdown.ClearOptions();

        var crosshairOptions = new List<TMP_Dropdown.OptionData>();
        string[] crosshairKeys = new string[]
        {
            "settings_crosshair_style_default",
            "settings_crosshair_style_dot",
            "settings_crosshair_style_plus",
            "settings_crosshair_style_defaultbig",
            "settings_crosshair_style_dots",
            "settings_crosshair_style_eye",
            "settings_crosshair_style_mounth",
            "settings_crosshair_style_mounthsecond",
            "settings_crosshair_style_cross"
        };

        for (int i = 0; i < crosshairKeys.Length; i++)
        {
            string localizedText = LocalitzationManager.Instance.GetKey(crosshairKeys[i]);
            crosshairOptions.Add(new TMP_Dropdown.OptionData(localizedText));
        }

        crosshairStyleDropdown.AddOptions(crosshairOptions);
        crosshairStyleDropdown.SetValueWithoutNotify(ConfigManager.Instance.Data.crosshairStyle);
        crosshairHUD.GetComponent<Image>().sprite = crosshairSprites[ConfigManager.Instance.Data.crosshairStyle];
        crosshairPreview.GetComponent<Image>().sprite = crosshairSprites[ConfigManager.Instance.Data.crosshairStyle];
        crosshairStyleDropdown.onValueChanged.AddListener((index) =>
        {
            var config = ConfigManager.Instance.Data;
            config.crosshairStyle = index;
            ConfigManager.Instance.SaveConfig();
            crosshairHUD.GetComponent<Image>().sprite = crosshairSprites[index];
            crosshairPreview.GetComponent<Image>().sprite = crosshairSprites[index];
        });

        // =============================
        // CROSSHAIR COLOR SETUP
        // =============================
        crosshairColorDropdown.onValueChanged.RemoveAllListeners();
        crosshairColorDropdown.ClearOptions();

        var crosshairColorOptions = new List<TMP_Dropdown.OptionData>();

        string[] crosshairColorKeys = new string[]
        {
            "settings_crosshair_color_red",
            "settings_crosshair_color_green",
            "settings_crosshair_color_blue",
            "settings_crosshair_color_yellow",
            "settings_crosshair_color_cyan",
            "settings_crosshair_color_pink",
            "settings_crosshair_color_white"
        };

        for (int i = 0; i < crosshairColorKeys.Length; i++)
        {
            string localizedText = LocalitzationManager.Instance.GetKey(crosshairColorKeys[i]);
            crosshairColorOptions.Add(new TMP_Dropdown.OptionData(localizedText));
        }

        crosshairColorDropdown.AddOptions(crosshairColorOptions);
        crosshairColorDropdown.SetValueWithoutNotify(ConfigManager.Instance.Data.crosshairColor);
        crosshairHUD.GetComponent<Image>().color = crosshairColors[ConfigManager.Instance.Data.crosshairColor];
        crosshairPreview.GetComponent<Image>().color = crosshairColors[ConfigManager.Instance.Data.crosshairColor];
        crosshairColorDropdown.onValueChanged.AddListener((index) =>
        {
            var config = ConfigManager.Instance.Data;
            config.crosshairColor = index;
            ConfigManager.Instance.SaveConfig();
            crosshairHUD.GetComponent<Image>().color = crosshairColors[index];
            crosshairPreview.GetComponent<Image>().color = crosshairColors[index];
        });

        // =============================
        // LANGUAGE DROPDOWN SETUP
        // =============================
        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>();

        foreach (Language lang in System.Enum.GetValues(typeof(Language)))
        {
            string key = lang switch
            {
                Language.EN => "settings_gameplay_language_en",
                Language.ES => "settings_gameplay_language_es",
                _ => "settings_gameplay_language_en"
            };

            string localizedText = LocalitzationManager.Instance.GetKey(key);
            options.Add(new TMP_Dropdown.OptionData(localizedText));
        }

        languageDropdown.AddOptions(options);

        languageDropdown.SetValueWithoutNotify((int)LocalitzationManager.Instance.CurrentLanguage);

        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);


        // =============================
        // SCREEN MODE SETUP
        // =============================
        screenMode.onValueChanged.RemoveAllListeners();

        int[] allowedConfigValues = new int[]
        {
            0,
            2,
            3
        };

        FullScreenMode[] allowedModes = new FullScreenMode[]
        {
            FullScreenMode.FullScreenWindow,
            FullScreenMode.MaximizedWindow,
            FullScreenMode.Windowed
        };

        screenMode.options.Clear();
        for (int i = 0; i < allowedModes.Length; i++)
        {
            string key = allowedModes[i] switch
            {
                FullScreenMode.FullScreenWindow => "settings_video_fullscreenmode_fullscreen",
                FullScreenMode.MaximizedWindow => "settings_video_fullscreenmode_maxwindow",
                FullScreenMode.Windowed => "settings_video_fullscreenmode_window",
                _ => "settings_video_fullscreenmode_fullscreen"
            };
            string localizedText = LocalitzationManager.Instance.GetKey(key);
            screenMode.options.Add(new TMP_Dropdown.OptionData(localizedText));
        }

        int currentConfigValue = ConfigManager.Instance.Data.fullscreenMode;
        int dropdownIndex = 0;
        for (int i = 0; i < allowedConfigValues.Length; i++)
        {
            if (allowedConfigValues[i] == currentConfigValue)
            {
                dropdownIndex = i;
                break;
            }
        }
        screenMode.SetValueWithoutNotify(dropdownIndex);

        // =============================
        // FUNC: UpdateResolutionDropdownInteractable
        // =============================
        void UpdateResolutionDropdownInteractable()
        {
            bool isMaximized = allowedModes[dropdownIndex] == FullScreenMode.MaximizedWindow;
            screenResolution.interactable = !isMaximized;
        }

        UpdateResolutionDropdownInteractable();

        screenMode.onValueChanged.AddListener((index) =>
        {
            var config = ConfigManager.Instance.Data;
            config.fullscreenMode = allowedConfigValues[index];
            ConfigManager.Instance.SaveConfig();
            Screen.fullScreenMode = allowedModes[index];

            dropdownIndex = index; 
            UpdateResolutionDropdownInteractable();
        });

        // =============================
        // SCREEN RESOLUTION SETUP
        // =============================
        screenResolution.onValueChanged.RemoveAllListeners();
        screenResolution.ClearOptions();

        var configData = ConfigManager.Instance.Data;
        var availableResolutions = Screen.resolutions;

        List<Resolution> uniqueResolutions = new List<Resolution>();
        foreach (var res in availableResolutions)
        {
            if (!uniqueResolutions.Exists(r => r.width == res.width && r.height == res.height))
                uniqueResolutions.Add(res);
        }

        uniqueResolutions.Sort((a, b) =>
        {
            int comp = b.width.CompareTo(a.width);
            if (comp == 0)
                comp = b.height.CompareTo(a.height);
            return comp;
        });

        List<string> resolutionOptions = new List<string>();
        foreach (var res in uniqueResolutions)
            resolutionOptions.Add($"{res.width}x{res.height}");

        screenResolution.AddOptions(resolutionOptions);

        if (configData.resolutionWidth == -1 || configData.resolutionHeight == -1)
        {
            configData.resolutionWidth = Screen.currentResolution.width;
            configData.resolutionHeight = Screen.currentResolution.height;
            ConfigManager.Instance.SaveConfig();
            LogManager.LogDebugOnly("Resolution was unsetted, setted to native resolution.", LogType.System);
        }

        int currentResolutionIndex = 0;
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            if (uniqueResolutions[i].width == configData.resolutionWidth &&
                uniqueResolutions[i].height == configData.resolutionHeight)
            {
                currentResolutionIndex = i;
                break;
            }
        }

        screenResolution.SetValueWithoutNotify(currentResolutionIndex);

        screenResolution.onValueChanged.AddListener((index) =>
        {
            Resolution selected = uniqueResolutions[index];
            var config = ConfigManager.Instance.Data;

            config.resolutionWidth = selected.width;
            config.resolutionHeight = selected.height;
            ConfigManager.Instance.SaveConfig();

            FullScreenMode mode = FullScreenMode.FullScreenWindow;
            for (int i = 0; i < allowedConfigValues.Length; i++)
            {
                if (allowedConfigValues[i] == config.fullscreenMode)
                {
                    mode = allowedModes[i];
                    break;
                }
            }

            Screen.SetResolution(selected.width, selected.height, mode);
        });

        // =============================
        // GRAPHICS QUALITY SETUP
        // =============================
        graphicsQuality.onValueChanged.RemoveAllListeners();
        graphicsQuality.ClearOptions();

        string[] qualityKeys = new string[]
        {
            "settings_video_graphicsquality_ultra",
            "settings_video_graphicsquality_high",
            "settings_video_graphicsquality_mid",
            "settings_video_graphicsquality_low"
        };

        List<TMP_Dropdown.OptionData> qualityOptions = new List<TMP_Dropdown.OptionData>();
        foreach (string key in qualityKeys)
        {
            string localizedText = LocalitzationManager.Instance.GetKey(key);
            qualityOptions.Add(new TMP_Dropdown.OptionData(localizedText));
        }

        graphicsQuality.AddOptions(qualityOptions);

        int currentQuality = ConfigManager.Instance.Data.graphicsQuality;

        int dropdownQualityIndex = (qualityKeys.Length - 1) - currentQuality;
        graphicsQuality.SetValueWithoutNotify(dropdownQualityIndex);
        graphicsQuality.RefreshShownValue();

        graphicsQuality.onValueChanged.AddListener((index) =>
        {
            int selectedQuality = (qualityKeys.Length - 1) - index;

            var config = ConfigManager.Instance.Data;
            config.graphicsQuality = selectedQuality;
            ConfigManager.Instance.SaveConfig();

            QualitySettings.SetQualityLevel(selectedQuality, true);
        });

        // =============================
        // LIMIT FPS SETUP
        // =============================
        limitFPS.onValueChanged.RemoveAllListeners();
        limitFPS.ClearOptions();

        int[] fpsValues = new int[] { 360, 240, 144, 120, 60, 30 };
        List<TMP_Dropdown.OptionData> fpsOptions = new List<TMP_Dropdown.OptionData>();

        fpsOptions.Add(new TMP_Dropdown.OptionData(LocalitzationManager.Instance.GetKey("settings_video_limitfps_unlimitted")));

        foreach (int fps in fpsValues)
        {
            fpsOptions.Add(new TMP_Dropdown.OptionData(fps + " FPS"));
        }

        limitFPS.AddOptions(fpsOptions);

        int currentFPSConfig = ConfigManager.Instance.Data.limitFPS;

        int dropdownFPSIndex;
        if (currentFPSConfig == 0)
        {
            dropdownFPSIndex = 0; 
        }
        else
        {
            dropdownFPSIndex = fpsValues.Length - currentFPSConfig + 1;
        }

        limitFPS.SetValueWithoutNotify(dropdownFPSIndex);
        limitFPS.RefreshShownValue();

        limitFPS.onValueChanged.AddListener((index) =>
        {
            var config = ConfigManager.Instance.Data;

            if (index == 0)
            {
                config.limitFPS = 0;
                Application.targetFrameRate = -1;
            }
            else
            {
                int selectedFPS = fpsValues[index - 1];
                config.limitFPS = fpsValues.Length - index + 1; 
                Application.targetFrameRate = selectedFPS;
            }

            ConfigManager.Instance.SaveConfig();
        });

        // =============================
        // SET SETTINGS FROM CONFIG
        // =============================
        FullScreenMode currentMode = FullScreenMode.FullScreenWindow;
        for (int i = 0; i < allowedConfigValues.Length; i++)
        {
            if (allowedConfigValues[i] == configData.fullscreenMode)
            {
                currentMode = allowedModes[i];
                break;
            }
        }

        Screen.SetResolution(configData.resolutionWidth, configData.resolutionHeight, currentMode);
        QualitySettings.SetQualityLevel(configData.graphicsQuality, true);
        Application.targetFrameRate = configData.limitFPS == 0 ? -1 : fpsValues[fpsValues.Length - configData.limitFPS + 1 - 1];
    }

    private void SetupToggles()
    {
        var config = ConfigManager.Instance.Data;

        toggleAim.isOn = config.toggleAim;
        toggleAim.onValueChanged.RemoveAllListeners();
        toggleAim.onValueChanged.AddListener((isOn) =>
        {
            config.toggleAim = isOn;
            ConfigManager.Instance.SaveConfig();
            PlayerManager.Instance.gameObject.transform.Find("Player").gameObject.GetComponent<ThirdPersonMovement>().ConfigureAimInput();
        });

        toggleAutoReload.isOn = config.autoReload;
        toggleAutoReload.onValueChanged.RemoveAllListeners();
        toggleAutoReload.onValueChanged.AddListener((isOn) =>
        {
            config.autoReload = isOn;
            ConfigManager.Instance.SaveConfig();
        });

        dynamicFov.isOn = config.dynamicFOV;
        dynamicFov.onValueChanged.RemoveAllListeners();
        dynamicFov.onValueChanged.AddListener((isOn) =>
        {
            config.dynamicFOV = isOn;
            ConfigManager.Instance.SaveConfig();
        });

        renderDamage.isOn = config.showDamageNumbers;
        renderDamage.onValueChanged.RemoveAllListeners();
        renderDamage.onValueChanged.AddListener((isOn) =>
        {
            config.showDamageNumbers = isOn;
            ConfigManager.Instance.SaveConfig();
        });

        muteUnfocused.isOn = config.muteWhenUnfocused;
        muteUnfocused.onValueChanged.RemoveAllListeners();
        muteUnfocused.onValueChanged.AddListener((isOn) =>
        {
            config.muteWhenUnfocused = isOn;
            ConfigManager.Instance.SaveConfig();
        });

        vsync.isOn = config.vSync;
        vsync.onValueChanged.RemoveAllListeners();
        QualitySettings.vSyncCount = config.vSync ? 1 : 0;
        if (config.vSync)
            limitFPS.interactable = false;
        vsync.onValueChanged.AddListener((isOn) =>
        {
            config.vSync = isOn;
            ConfigManager.Instance.SaveConfig();
            QualitySettings.vSyncCount = isOn ? 1 : 0;
            if (isOn)
                limitFPS.interactable = false;
            else
                limitFPS.interactable = true;
        });

        invertY.isOn = config.invertY;
        invertY.onValueChanged.RemoveAllListeners();
        var camera = PlayerManager.Instance
                .gameObject
                .transform.Find("MainCamera")
                .GetComponent<ThirdPersonCamera>();
        camera.invertY = config.invertY;
        invertY.onValueChanged.AddListener((isOn) =>
        {
            config.invertY = isOn;
            ConfigManager.Instance.SaveConfig();
            camera.invertY = isOn;
        });
    }

    private void SetupSliders()
    {
        var config = ConfigManager.Instance.Data;

        masterVolume.value = config.masterVolume * 10f;
        masterVolume.onValueChanged.RemoveAllListeners();
        masterVolumePercentage.text = Mathf.RoundToInt(config.masterVolume * 100f).ToString() + "%";
        masterVolumePercentage.ForceMeshUpdate();
        AudioManager.Instance.SetMasterVolume(config.masterVolume);
        masterVolume.onValueChanged.AddListener((value) =>
        {
            float normalized = Mathf.Clamp01(value / 10f);
            if (normalized <= 0.0001f)
                normalized = 0.0001f;

            config.masterVolume = normalized;
            AudioManager.Instance.SetMasterVolume(normalized);
            ConfigManager.Instance.SaveConfig();
            masterVolumePercentage.text = Mathf.RoundToInt(normalized * 100f).ToString() + "%";
            masterVolumePercentage.ForceMeshUpdate();
        });

        bgmVolume.value = config.bgmVolume * 10f;
        bgmVolume.onValueChanged.RemoveAllListeners();
        bgmVolumePercentage.text = Mathf.RoundToInt(config.bgmVolume * 100f).ToString() + "%";
        bgmVolumePercentage.ForceMeshUpdate();
        AudioManager.Instance.SetVolume(SoundType.BGM, config.bgmVolume);
        bgmVolume.onValueChanged.AddListener((value) =>
        {
            float normalized = Mathf.Clamp01(value / 10f);
            if (normalized <= 0.0001f)
                normalized = 0.0001f;

            config.bgmVolume = normalized;
            AudioManager.Instance.SetVolume(SoundType.BGM, normalized);
            ConfigManager.Instance.SaveConfig();
            bgmVolumePercentage.text = Mathf.RoundToInt(normalized * 100f).ToString() + "%";
            bgmVolumePercentage.ForceMeshUpdate();
        });

        sfxVolume.value = config.sfxVolume * 10f;
        sfxVolume.onValueChanged.RemoveAllListeners();
        sfxVolumePercentage.text = Mathf.RoundToInt(config.sfxVolume * 100f).ToString() + "%";
        sfxVolumePercentage.ForceMeshUpdate();
        AudioManager.Instance.SetVolume(SoundType.SFX, config.sfxVolume);
        sfxVolume.onValueChanged.AddListener((value) =>
        {
            float normalized = Mathf.Clamp01(value / 10f);
            if (normalized <= 0.0001f)
                normalized = 0.0001f;
            config.sfxVolume = normalized;
            AudioManager.Instance.SetVolume(SoundType.SFX, normalized);
            ConfigManager.Instance.SaveConfig();
            sfxVolumePercentage.text = Mathf.RoundToInt(normalized * 100f).ToString() + "%";
            sfxVolumePercentage.ForceMeshUpdate();
        });
    }

    private void SetupSensitivity()
    {
        var config = ConfigManager.Instance.Data;
        var camera = PlayerManager.Instance
            .gameObject
            .transform.Find("MainCamera")
            .GetComponent<ThirdPersonCamera>();

        void UpdateMouseText()
        {
            mouseSensitivityValue.text = config.mouseSensitivity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        UpdateMouseText();
        camera.mouseRotationSpeed = config.mouseSensitivity;

        subtractMouseSensitivity.onClick.RemoveAllListeners();
        subtractMouseSensitivity.onClick.AddListener(() =>
        {
            config.mouseSensitivity = Mathf.Clamp(config.mouseSensitivity - 0.05f, 0.1f, 10f);
            ConfigManager.Instance.SaveConfig();
            UpdateMouseText();
            camera.mouseRotationSpeed = config.mouseSensitivity;
        });

        addMouseSensitivity.onClick.RemoveAllListeners();
        addMouseSensitivity.onClick.AddListener(() =>
        {
            config.mouseSensitivity = Mathf.Clamp(config.mouseSensitivity + 0.05f, 0.1f, 10f);
            ConfigManager.Instance.SaveConfig();
            UpdateMouseText();
            camera.mouseRotationSpeed = config.mouseSensitivity;
        });

        void UpdateControlText()
        {
            controlSensitivityValue.text = config.controllerSensitivity.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }

        UpdateControlText();
        camera.gamepadRotationSpeed = config.controllerSensitivity;

        subtractControlSensitivity.onClick.RemoveAllListeners();
        subtractControlSensitivity.onClick.AddListener(() =>
        {
            config.controllerSensitivity = Mathf.Clamp(config.controllerSensitivity - 10f, 50f, 1000f);
            ConfigManager.Instance.SaveConfig();
            UpdateControlText();
            camera.gamepadRotationSpeed = config.controllerSensitivity;
        });

        addControlSensitivity.onClick.RemoveAllListeners();
        addControlSensitivity.onClick.AddListener(() =>
        {
            config.controllerSensitivity = Mathf.Clamp(config.controllerSensitivity + 10f, 50f, 1000f);
            ConfigManager.Instance.SaveConfig();
            UpdateControlText();
            camera.gamepadRotationSpeed = config.controllerSensitivity;
        });
    }

    public void SwitchSettings(bool isOpen)
    {
        settingsPanel.SetActive(isOpen);

        if (isOpen)
        {
            ESCManager.Instance.SetBlockEsc(true);
            SwitchSection(0);
        }
        else
            ESCManager.Instance.SetBlockEsc(false);

        if (isOpen && InputManager.Instance.CurrentDevice == InputDeviceType.Gamepad)
            InputManager.Instance.SearchNewButton();
        else if (!isOpen && InputManager.Instance.CurrentDevice == InputDeviceType.Gamepad)
            InputManager.Instance.SearchNewButton();
    }

    private void OnLanguageChanged(int index)
    {
        Language selectedLanguage = (Language)index;
        LocalitzationManager.Instance.SetLanguage(selectedLanguage);

        var config = ConfigManager.Instance.Data;
        config.language = index;
        ConfigManager.Instance.SaveConfig();

        StartCoroutine(RefreshDropdownsNextFrame());
    }

    private IEnumerator RefreshDropdownsNextFrame()
    {
        yield return null;
        SetupDropdowns();
        screenMode.RefreshShownValue();
        graphicsQuality.RefreshShownValue();
        limitFPS.RefreshShownValue();
        crosshairStyleDropdown.RefreshShownValue();
        crosshairColorDropdown.RefreshShownValue();
    }
}