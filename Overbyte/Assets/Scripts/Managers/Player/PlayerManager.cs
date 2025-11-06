using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : Singleton<PlayerManager>
{
    public PlayerInput PlayerInput { get; private set; }

    public string UUID { get; private set; }

    public int HP { get; private set; } = 100;

    public PlayerState CurrentState { get; private set; } = PlayerState.Loading;

    protected override void Awake()
    {
        base.Awake();
        PlayerInput = gameObject.transform.Find("Player").GetComponent<PlayerInput>();
        if (PlayerInput == null)
        {
            LogManager.Log("PlayerInput component not found!", LogType.Error);
        }
    }

    public void SetUUID(string uuid)
    {
        UUID = uuid;
        LogManager.LogDebugOnly($"Player UUID set to: {uuid}", LogType.Network);
    }

    public void SetHP(int hp)
    {
        HP = hp;
        LogManager.LogDebugOnly($"Player HP set to: {hp}", LogType.Gameplay);
    }

    public void SetState(PlayerState state)
    {
        CurrentState = state;
        LogManager.Log($"Player state changed to: {state}", LogType.Gameplay);
    }
}
