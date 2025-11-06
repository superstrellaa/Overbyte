using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class MatchmakingButton : MonoBehaviour
{
    public static MatchmakingButton Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text timerText;
    public Button cancelButton;

    [Header("Linked UI")]
    public GameObject playButtonObject;
    public GameObject matchmakingButtonObject;

    [Header("Queue Settings")]
    [SerializeField] private Button queueButton;
    [SerializeField] private GameObject queueChanger;
    [SerializeField] private Button queueGamemode1;
    [SerializeField] private Button queueGamemode2;
    [SerializeField] private Button queueGamemode3;
    [SerializeField] private Button queueGamemode4;
    [SerializeField] private Button queueClose;

    private Coroutine timerCoroutine;
    private float elapsedTime = 0f;
    private bool isSearching = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playButtonObject.GetComponent<Button>().onClick.AddListener(StartMatchmaking);
        cancelButton.onClick.AddListener(CancelMatchmaking);
        matchmakingButtonObject.SetActive(false);

        queueButton.onClick.AddListener(ToggleQueueChanger);
        queueClose.onClick.AddListener(ToggleQueueChanger);
        queueGamemode1.onClick.AddListener(() => ChangeGamemode(1));
        queueGamemode2.onClick.AddListener(() => ChangeGamemode(2));
        queueGamemode3.onClick.AddListener(() => ChangeGamemode(3));
        queueGamemode4.onClick.AddListener(() => ChangeGamemode(4));
        queueChanger.SetActive(false);
        queueButton.GetComponentInChildren<TMP_Text>().text = ConfigManager.Instance.Data.gamemode switch
        {
            1 => "1v1",
            2 => "2v2",
            3 => "3v3",
            4 => "4v4",
            _ => "Unknown"
        };
    }

    private void StartMatchmaking()
    {
        StartCoroutine(DoStartMatchmaking());
    }

    private IEnumerator DoStartMatchmaking()
    {
        PlayerManager.Instance.SetState(PlayerState.Queue);
        isSearching = true;
        elapsedTime = 0f;

        matchmakingButtonObject.SetActive(true);
        if (InputManager.Instance.CurrentDevice == InputDeviceType.Gamepad)
            EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
        yield return null; 

        playButtonObject.SetActive(false);
        timerCoroutine = StartCoroutine(UpdateTimer());

        queueButton.GetComponent<ButtonComponent>().SetInteractable(false);

        LogManager.Log("Matchmaking started", LogType.Network);
        int gamemode = ConfigManager.Instance.Data.gamemode;
        NetworkManager.Instance.Send($"{{\"type\":\"joinQueue\",\"quantity\":{gamemode}}}");
    }


    public void CancelMatchmaking()
    {
        PlayerManager.Instance.SetState(PlayerState.Lobby);
        isSearching = false;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        LogManager.Log("Matchmaking cancelled", LogType.Network);

        if (NetworkManager.Instance.IsConnected)
            NetworkManager.Instance.Send("{\"type\":\"leaveQueue\"}");

        matchmakingButtonObject.SetActive(false);
        playButtonObject.SetActive(true);
        queueButton.GetComponent<ButtonComponent>().SetInteractable(true);
        if (InputManager.Instance.CurrentDevice == InputDeviceType.Gamepad)
            EventSystem.current.SetSelectedGameObject(playButtonObject);
    }

    public void RestartMatchmaking()
    {
        isSearching = false;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        elapsedTime = 0f;

        matchmakingButtonObject.SetActive(false);
        playButtonObject.SetActive(true);
    }

    private IEnumerator UpdateTimer()
    {
        while (isSearching)
        {
            elapsedTime += Time.deltaTime;
            int seconds = Mathf.FloorToInt(elapsedTime);
            int minutes = seconds / 60;
            seconds %= 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
            yield return null;
        }
    }

    public void ToggleQueueChanger()
    {
        bool isActive = queueChanger.activeSelf;
        queueChanger.SetActive(!isActive);
        if (!isActive)
        {
            if (InputManager.Instance.CurrentDevice == InputDeviceType.Gamepad)
                EventSystem.current.SetSelectedGameObject(queueGamemode1.gameObject);
        }
        else
        {
            if (InputManager.Instance.CurrentDevice == InputDeviceType.Gamepad)
                EventSystem.current.SetSelectedGameObject(queueButton.gameObject);
        }
    }

    private void ChangeGamemode(int mode)
    {
        ConfigManager.Instance.Data.gamemode = mode;
        ConfigManager.Instance.SaveConfig();

        string label = mode switch
        {
            1 => "1v1",
            2 => "2v2",
            3 => "3v3",
            4 => "4v4",
            _ => "Unknown"
        };

        var textComp = queueButton.gameObject
            .transform.Find("QueueButtonText")
            .GetComponent<TMP_Text>();

        textComp.text = label;

        ToggleQueueChanger();
    }

    public void ActiveQueueButton()
    {
        queueButton.GetComponent<ButtonComponent>().SetInteractable(true);
    }
}
