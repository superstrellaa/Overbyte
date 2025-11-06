using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ESCManager : MonoBehaviour
{
    public static ESCManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject escMenuObject;

    [Header("Buttons")]
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject exitButton;

    [Header("Audio")]
    [SerializeField] private SoundData escOpenSound;

    private PlayerInput playerInput;
    private InputAction escAction;

    private bool leaveOk = false;
    private bool blockEsc = false;

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
        playerInput = PlayerManager.Instance.PlayerInput;

        escMenuObject.SetActive(false);

        continueButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseESCMenu);
        exitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ExitToMainMenu);

        escAction = playerInput.actions["ESC"];
        escAction.performed += ctx => ToggleESCMenu();
        escAction.Enable();
    }

    private void ToggleESCMenu()
    {
        if (PlayerManager.Instance.CurrentState != PlayerState.Playing || blockEsc)
            return;

        if (escMenuObject.activeSelf)
        {
            CloseESCMenu();
        }
        else
        {
            OpenESCMenu();
        }
    }

    void OpenESCMenu()
    {
        escMenuObject.SetActive(true);
        AudioManager.Instance.PlaySound(escOpenSound);
        EventSystem.current.SetSelectedGameObject(continueButton);
        if (InputManager.Instance?.CurrentDevice == InputDeviceType.Gamepad)
            CursorManager.Instance.SetCursor(CursorType.Hidden);
        else
            CursorManager.Instance.SetCursor(CursorType.Default);
        GUIManager.Instance.SetFreezeMovement(true);
    }

    public void CloseESCMenu()
    {
        escMenuObject.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        CursorManager.Instance.SetCursor(CursorType.Hidden);
        GUIManager.Instance.SetFreezeMovement(false);
    }

    public void CloseESCMenuCursor()
    {
        escMenuObject.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        GUIManager.Instance.SetFreezeMovement(false);
    }

    void ExitToMainMenu()
    {
        PopupManager.Instance.Open(PopupType.ExitConfirmation, showConfirm: true, onConfirm: () =>
        {
            CoroutineRunner.Instance.StartCoroutine(ExitToMainMenuEnum());
        });
    }

    IEnumerator ExitToMainMenuEnum()
    {
        CloseESCMenu();
        NetworkManager.Instance.Send("{\"type\":\"leaveRoom\"}");
        yield return new WaitUntil(() => leaveOk);
        PlayerManager.Instance.SetState(PlayerState.Lobby);

        if (MapManager.IsAlive)
            MapManager.Instance.ResetMapState();

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: true);
        GUIManager.Instance.SetGUIOpen(true);
        var playerObj = PlayerManager.Instance.gameObject.transform.Find("Player").gameObject;
        var movement = playerObj.GetComponent<ThirdPersonMovement>();
        movement.enabled = false;
        playerObj.transform.position = Vector3.zero;
        playerObj.transform.rotation = Quaternion.identity;
        movement.enabled = true;
        HUDManager.Instance.UpdateHealth(100);
        HUDManager.Instance.ShowHUD(false);

        GUIManager.Instance.ShowPanel(GUIManager.Instance.lobbyMatchFound, false);
    }

    public void ActionLeaveOk()
    {
        leaveOk = true;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && ConfigManager.Instance.Data.muteWhenUnfocused)
        {
            AudioManager.Instance.SetMasterVolume(0.0001f);
        }
        else
        {
            AudioManager.Instance.ResetMasterVolume();
        }

        if (!hasFocus && PlayerManager.Instance.CurrentState == PlayerState.Playing && !escMenuObject.activeSelf)
        {
            OpenESCMenu();
        }
    }

    private void OnDisable()
    {
        escAction?.Disable();
    }

    public void SetBlockEsc(bool value) => blockEsc = value;
}
