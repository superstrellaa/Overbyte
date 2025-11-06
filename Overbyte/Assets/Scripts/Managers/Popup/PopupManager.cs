using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public enum PopupType
{
    ConnectionRefused,
    RoomDeleted,
    NotServer,
    DifferentVersion,
    ExitConfirmation,
    RoomInactive
}

public class PopupManager : Singleton<PopupManager>
{
    [Header("Popup References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button confirmButton;

    [Header("Audio")]
    [SerializeField] private SoundData popupOpenSound;

    private Dictionary<PopupType, (string titleKey, string contentKey)> predefinedPopups;

    private Action retryAction;
    private Action confirmAction;

    private PlayerInput playerInput;
    private InputAction cancelAction;

    private bool canClosedByEscape = true;

    private PopupType? currentPopupType;

    protected override void Awake()
    {
        base.Awake();

        predefinedPopups = new Dictionary<PopupType, (string, string)>
        {
            { PopupType.ConnectionRefused, ("popuptype_connectionrefused_title", "popuptype_connectionrefused_description") },
            { PopupType.RoomDeleted, ("popuptype_roomdeleted_title", "popuptype_roomdeleted_description") },
            { PopupType.NotServer, ("popuptype_notserver_title", "popuptype_notserver_description") },
            { PopupType.DifferentVersion, ("popuptype_differentversion_title", "popuptype_differentversion_description") },
            { PopupType.ExitConfirmation, ("popuptype_exitconfirmation_title", "popuptype_exitconfirmation_description") },
            { PopupType.RoomInactive, ("popuptype_roominactive_title", "popuptype_roominactive_description") }
        };

        playerInput = PlayerManager.Instance.PlayerInput;
        cancelAction = playerInput.actions["Cancel"];
    }

    void Start()
    {
        cancelAction.performed += ctx => TryCloseWithCancel();

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                Close();
                retryAction?.Invoke();
            });
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() =>
            {
                Close();
                confirmAction?.Invoke();
            });
        }
    }

    private void OnEnable()
    {
        cancelAction.Enable();
        LocalitzationManager.OnLanguageChanged += UpdatePopupText;
    }

    private void OnDisable()
    {
        cancelAction.Disable();
        LocalitzationManager.OnLanguageChanged -= UpdatePopupText;
    }

    private void UpdatePopupText()
    {
        if (popupPanel.activeSelf && currentPopupType.HasValue)
        {
            var keys = predefinedPopups[currentPopupType.Value];
            titleText.text = LocalitzationManager.Instance.GetKey(keys.titleKey);
            contentText.text = LocalitzationManager.Instance.GetKey(keys.contentKey);
        }
    }

    private void TryCloseWithCancel()
    {
        if (popupPanel != null && popupPanel.activeSelf && canClosedByEscape)
        {
            Close();
        }
    }

    public void Open(string title, string content, bool showRetry = false, Action onRetry = null, bool showConfirm = false, Action onConfirm = null)
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            GUIManager.Instance.SetGUIOpen(true);
            AudioManager.Instance.PlaySound(popupOpenSound);
        }

        if (titleText != null)
            titleText.text = title;

        if (contentText != null)
            contentText.text = content;

        retryAction = onRetry;
        if (showRetry)
        {
            retryButton.gameObject.SetActive(true);
            closeButton.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
            canClosedByEscape = false;
        }
        else
        {
            retryButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            canClosedByEscape = true;
        }

        confirmAction = onConfirm;
        if (showConfirm)
        {
            confirmButton.gameObject.SetActive(true);
            closeButton.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
        }
        else
        {
            confirmButton.gameObject.SetActive(false);
            if (!showRetry)
                closeButton.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
    }

    public void Open(PopupType type, bool showRetry = false, Action onRetry = null, bool showConfirm = false, Action onConfirm = null)
    {
        if (predefinedPopups.ContainsKey(type))
        {
            var keys = predefinedPopups[type];

            string localizedTitle = LocalitzationManager.Instance.GetKey(keys.titleKey);
            string localizedContent = LocalitzationManager.Instance.GetKey(keys.contentKey);

            currentPopupType = type;

            Open(localizedTitle, localizedContent, showRetry, onRetry, showConfirm, onConfirm);
        }
        else
        {
            LogManager.LogDebugOnly($"PopupType {type} not found in predefined popups!", LogType.Warning);
        }
    }

    public void Close()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            currentPopupType = null;
            CursorManager.Instance.SetCursor(CursorType.Default);
            canClosedByEscape = true;
            if (PlayerManager.Instance.CurrentState == PlayerState.Playing)
                GUIManager.Instance.SetGUIOpen(false);
            else
                GUIManager.Instance.SetGUIOpen(true);
            InputManager.Instance.SearchNewButton();
        }
    }
}
