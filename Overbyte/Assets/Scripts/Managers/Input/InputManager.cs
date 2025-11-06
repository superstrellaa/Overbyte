using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public enum InputDeviceType
{
    KeyboardMouse,
    Gamepad,
    Touch
}

public class InputManager : Singleton<InputManager>
{
    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.KeyboardMouse;

    public event Action<InputDeviceType> OnDeviceChanged;

    private void OnEnable() 
    { 
        if (!Application.isPlaying)
            return;

        InputSystem.onAnyButtonPress.Call(OnAnyButtonPress); 
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable() 
    { 
        InputSystem.onDeviceChange -= OnDeviceChange; 
    }

    private void OnAnyButtonPress(InputControl control)
    {
        if (control.device is Gamepad)
            SetDevice(InputDeviceType.Gamepad);
        else if (control.device is Keyboard || control.device is Mouse)
            SetDevice(InputDeviceType.KeyboardMouse);
        else if (control.device is Touchscreen)
            SetDevice(InputDeviceType.Touch);

        // InputSystem.onAnyButtonPress.Call(OnAnyButtonPress);
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Disconnected && device is Gamepad)
        {
            SetDevice(InputDeviceType.KeyboardMouse);
        }
    }

    private void SetDevice(InputDeviceType newDevice)
    {
        if (newDevice == CurrentDevice)
            return;

        CurrentDevice = newDevice;
        OnDeviceChanged?.Invoke(CurrentDevice);

        LogManager.LogDebugOnly($"Device changed to {newDevice}", LogType.InputManager);

        if (CursorManager.Instance == null || PlayerManager.Instance == null || GUIManager.Instance == null)
            return;

        if (CurrentDevice == InputDeviceType.Gamepad)
        {
            CursorManager.Instance.SetCursor(CursorType.Hidden);
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                var firstButton = GameObject.FindGameObjectWithTag("Input/DefaultSelected");
                if (firstButton != null && firstButton.activeSelf == true)
                    EventSystem.current.SetSelectedGameObject(firstButton);
            }
        }
        else
        {
            if (PlayerManager.Instance.CurrentState == PlayerState.Playing && !GUIManager.Instance.IsGUIOpen && !GUIManager.Instance.freezeMovement)
                CursorManager.Instance.SetCursor(CursorType.Hidden);
            else
                CursorManager.Instance.SetCursor(CursorType.Default);
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    public void SearchNewButton()
    {
        var button = GameObject.FindGameObjectsWithTag("Input/DefaultSelected");
        if (button.Length > 0 && button[0].activeSelf == true)
            EventSystem.current?.SetSelectedGameObject(button[0]);
    }
}
