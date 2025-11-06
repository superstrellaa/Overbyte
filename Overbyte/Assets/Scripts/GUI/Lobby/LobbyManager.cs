using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private SoundData matchFoundSound;

    private GameObject lobbyPanel;
    private GameObject lobbyMatchFound;
    private RectTransform countdownBar;

    private Vector2 initialPos;
    private Vector2 initialSize;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (lobbyPanel == null)
            lobbyPanel = GUIManager.Instance.lobbyGUI;

        if (lobbyMatchFound == null)
            lobbyMatchFound = GUIManager.Instance.lobbyMatchFound;

        if (lobbyPanel != null)
        {
            GUIManager.Instance.SetGUIOpen(true);
            GUIManager.Instance.ShowPanel(lobbyPanel, true);
            if (InputManager.Instance.CurrentDevice == InputDeviceType.Gamepad)
                EventSystem.current.SetSelectedGameObject(lobbyPanel.transform.Find("PlayButton")?.gameObject);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Lobby GUI not found in the scene.");
        }

        if (lobbyMatchFound != null)
        {
            var barObj = lobbyMatchFound
                .transform.Find("MatchFoundCountBG")
                ?.Find("MatchFoundCountBGg")
                ?.Find("MatchFoundCount");

            if (barObj != null)
            {
                countdownBar = barObj.GetComponent<RectTransform>();
                initialPos = countdownBar.anchoredPosition;
                initialSize = countdownBar.sizeDelta;
            }
            else
            {
                Debug.LogWarning("[LobbyManager] MatchFoundCount no encontrado dentro de lobbyMatchFound");
            }
        }
    }

    void OnDestroy()
    {
        if (GUIManager.IsAlive && lobbyPanel != null)
        {
            GUIManager.Instance.SetGUIOpen(false);
            GUIManager.Instance.ShowPanel(lobbyPanel, false);
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public IEnumerator MatchFoundScreenEnum()
    {
        if (!PlayerManager.Instance.CurrentState.Equals(PlayerState.Queue))
        {
            LogManager.Log("Player is not in queue state, aborting match found screen.", LogType.Warning);
            yield break;
        }

        MatchmakingButton.Instance.RestartMatchmaking();
        SettingsManager.Instance.SwitchSettings(false);
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForSeconds(0.5f);

        GUIManager.Instance.ShowPanel(lobbyMatchFound, true);
        AudioManager.Instance.PlaySound(matchFoundSound);

        int countdown = 5;
        float duration = countdown;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (countdownBar != null)
            {
                float newWidth = Mathf.Lerp(initialSize.x, 0, t);
                countdownBar.sizeDelta = new Vector2(newWidth, initialSize.y);

                float offsetX = (initialSize.x - newWidth) * 0.5f;
                countdownBar.anchoredPosition = initialPos - new Vector2(offsetX, 0);
            }

            yield return null;
        }

        LogManager.Log("Countdown finished, starting game...", LogType.Gameplay);
        SceneTransitionManager.Instance.TransitionTo(
            $"sc_{NetworkManager.Instance.MapName.Replace(" ", "")}",
            withFade: true
        );
        HUDManager.Instance.ShowHUD(true);
        MatchmakingButton.Instance.ActiveQueueButton();
        PlayerManager.Instance.SetState(PlayerState.Playing);

        if (countdownBar != null)
        {
            countdownBar.sizeDelta = initialSize;
            countdownBar.anchoredPosition = initialPos;
        }

        yield break;
    }
}
