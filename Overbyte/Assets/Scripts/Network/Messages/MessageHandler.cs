using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MessageHandler
{
    private static float lastSendTime = 0f;

    public static void Handle(string json)
    {
        try
        {
            JObject obj = JObject.Parse(json);
            string type = obj.Value<string>("type");

            switch (type)
            {
                case "init":
                    HandleInit(obj);
                    break;

                case "startGame":
                    HandleStartGame(obj);
                    break;

                case "startPositions":
                    HandleStartPositions(obj);
                    break;

                case "playerMoved":
                    HandlePlayerMoved(obj);
                    break;

                case "playerJoin":
                    HandlePlayerJoin(obj);
                    break;

                case "playerDisconnected":
                    HandlePlayerDisconnected(obj);
                    break;

                case "roomInactive":
                    LogManager.LogDebugOnly("Room is inactive.", LogType.Network);
                    CoroutineRunner.Instance.Run(HandleRoomInactiveEnum(obj.Value<string>("roomId")));
                    break;

                case "roomDeleted":
                    HandleRoomDeleted(obj);
                    break;

                case "roomLefted":
                    ESCManager.Instance.ActionLeaveOk();
                    LogManager.LogDebugOnly("Left room successfully.", LogType.Network);
                    break;

                case "authSuccess":
                    LogManager.Log("Authentication successful.", LogType.Network);
                    break;

                case "adminMessage":
                    HandleAdminMessage(obj);
                    break;

                case "ping":
                    NetworkManager.Instance.Send("{\"type\":\"pong\"}");
                    break;

                case "pong":
                    HandlePong();
                    break;

                case "gunChanged":
                    HandleGunChanged(obj);
                    break;

                case "aiming":
                    HandleAiming(obj);
                    break;

                case "shootFired":
                    HandleShootFired(obj);
                    break;

                case "shootReceived":
                    HandleShootRecivied(obj);
                    break;

                case "shootGiven":
                    HandleShootGiven(obj);
                    break;

                case "shootDebugInfo":
                    DrawGizmoDebugShoot(obj);
                    break;

                case "error":
                    string error = obj.Value<string>("error");
                    LogManager.Log($"Server error: {error}", LogType.Error);
                    break;

                default:
                    LogManager.Log($"Unknown message type: {type}", LogType.Warning);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to parse message: {ex.Message}, {json}", LogType.Error);
        }
    }

    private static void HandlePong()
    {
        double rtt = (Time.timeAsDouble - NetworkManager.Instance.LastPingSent) * 1000.0;
        NetworkManager.Instance.AddRttSample(rtt);

        double avg = NetworkManager.Instance.GetAverageRtt();
        double jitter = NetworkManager.Instance.GetJitter();

        LogManager.LogDebugOnly($"Ping: {rtt:F1} ms | Avg: {avg:F1} ms | Jitter: {jitter:F1} ms", LogType.Network);
    }

    private static void HandleInit(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");

        PlayerManager.Instance.SetUUID(uuid);
        LogManager.Log($"Received init. UUID: {uuid}", LogType.Network);
    }

    private static void HandleStartGame(JObject msg)
    {
        string roomId = msg.Value<string>("roomId");
        string mapName = msg.Value<string>("map") ?? "Unknown Map";
        var playersToken = msg["players"];

        var playersList = new List<PlayerInfo>();

        if (playersToken is JArray playersArray)
        {
            foreach (var p in playersArray)
            {
                if (p is JObject playerObj)
                {
                    string uuid = playerObj.Value<string>("uuid");
                    Vector3 spawnPos = Vector3.zero;
                    Quaternion spawnRot = Quaternion.identity;

                    playersList.Add(new PlayerInfo
                    {
                        uuid = uuid,
                        spawnPosition = spawnPos,
                        spawnRotation = spawnRot
                    });
                }
                else if (p is JValue playerVal)
                {
                    string uuid = playerVal.Value<string>();
                    playersList.Add(new PlayerInfo
                    {
                        uuid = uuid,
                        spawnPosition = Vector3.zero,
                        spawnRotation = Quaternion.identity
                    });
                }
                else
                {
                    Debug.LogWarning("Unexpected player format: " + p);
                }
            }
        }
        else
        {
            Debug.LogWarning("Players token is not an array: " + playersToken);
        }

        NetworkManager.Instance.SetMatchData(roomId, playersList, mapName);
        LogManager.LogDebugOnly($"Match started in Room: {roomId} with players: {playersList.Count}", LogType.Gameplay);

        if (LobbyManager.Instance != null)
            CoroutineRunner.Instance.Run(LobbyManager.Instance.MatchFoundScreenEnum());
    }

    private static void HandleStartPositions(JObject msg)
    {
        var positionsToken = msg["positions"] as JObject;
        if (positionsToken == null) return;

        foreach (var kvp in positionsToken)
        {
            string uuid = kvp.Key;
            var posObj = kvp.Value as JObject;
            if (posObj == null) continue;

            float x = posObj.Value<float>("x");
            float y = posObj.Value<float>("y");
            float z = posObj.Value<float>("z");

            var playerInfo = NetworkManager.Instance.Players.Find(pl => pl.uuid == uuid);
            if (playerInfo != null)
            {
                playerInfo.spawnPosition = new Vector3(x, y, z);
                playerInfo.spawnRotation = Quaternion.identity;
            }
            LogManager.LogDebugOnly($"Updated {uuid} spawn -> {playerInfo.spawnPosition}", LogType.Gameplay);
        }

        NetworkManager.Instance.MarkSpawnsReady();
        LogManager.LogDebugOnly($"Start positions updated for {positionsToken.Count} players.", LogType.Gameplay);
    }

    public static void Move(Vector3 position, float rotationY, Vector3 velocity)
    {
        if (!NetworkManager.IsAlive || !NetworkManager.Instance.IsConnected) return;

        if (Time.time - lastSendTime < 0.05f)
            return;

        lastSendTime = Time.time;

        JObject moveMsg = new JObject
        {
            ["type"] = "move",
            ["x"] = position.x,
            ["y"] = position.y,
            ["z"] = position.z,
            ["rotationY"] = rotationY,
            ["vx"] = velocity.x,
            ["vy"] = velocity.y,
            ["vz"] = velocity.z
        };

        NetworkManager.Instance.Send(moveMsg.ToString());
    }

    private static void HandlePlayerMoved(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        float x = msg.Value<float>("x");
        float y = msg.Value<float>("y");
        float z = msg.Value<float>("z");
        float rotationY = msg.Value<float>("rotationY");
        float vx = msg.Value<float>("vx");
        float vy = msg.Value<float>("vy");
        float vz = msg.Value<float>("vz");

        Vector3 position = new Vector3(x, y, z);
        Vector3 velocity = new Vector3(vx, vy, vz);

        LogManager.LogDebugOnly($"Player {uuid} moved to {position} rotY({rotationY}) vel({velocity})", LogType.Gameplay);

        if (uuid != PlayerManager.Instance.UUID)
        {
            MapManager.Instance.UpdateRemotePlayer(uuid, position, rotationY, velocity);
        }
    }

    private static void HandlePlayerJoin(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        LogManager.Log($"Player {uuid} joined the game.", LogType.Gameplay);
        if (MapManager.IsAlive)
            MapManager.Instance.AddRemotePlayer(uuid);
    }

    private static void HandlePlayerDisconnected(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        LogManager.Log($"Player {uuid} disconnected.", LogType.Gameplay);

        if (MapManager.IsAlive)
            MapManager.Instance.RemoveRemotePlayer(uuid);
    }

    private static void HandleGunChanged(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        string gunName = msg.Value<string>("gun");

        LogManager.LogDebugOnly($"Player {uuid} changed gun to {gunName}", LogType.Gameplay);

        if (uuid != PlayerManager.Instance.UUID && MapManager.IsAlive)
        {
            MapManager.Instance.SetRemotePlayerGun(uuid, gunName);
        }
    }

    private static void HandleAiming(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        float pitch = msg.Value<float>("pitch");
        LogManager.LogDebugOnly($"Player {uuid} aiming: {pitch}", LogType.Gameplay);
        if (uuid != PlayerManager.Instance.UUID && MapManager.IsAlive)
        {
            MapManager.Instance.SetRemotePlayerAiming(uuid, pitch);
        }
    }

    private static void HandleShootFired(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        string hit = msg.Value<string>("hit");
        Vector3 hitPoint = ToVector3(msg["hitPoint"]);
        Vector3 hitNormal = ToVector3(msg["hitNormal"]);
        string gun = msg.Value<string>("gun");
        LogManager.LogDebugOnly($"Player {uuid} fired a shot. HitPoint: {hitPoint}, HitNormal: {hitNormal}, Gun {gun}", LogType.Gameplay);
        if (uuid != PlayerManager.Instance.UUID && MapManager.IsAlive)
        {
            MapManager.Instance.RemotePlayerShoot(uuid, hit, hitPoint, hitNormal, gun);
        }
    }

    private static void HandleShootRecivied(JObject msg)
    {
        int HP = msg.Value<int>("HP");
        LogManager.LogDebugOnly($"Recivied a new shoot, new HP: {HP}", LogType.Gameplay);
        if (HP != PlayerManager.Instance.HP)
        {
            HUDManager.Instance.UpdateHealth(HP);
        }
    }

    private static void HandleShootGiven(JObject msg)
    {
        string targetUUID = msg.Value<string>("targetUuid");
        int damage = msg.Value<int>("HP");
        LogManager.LogDebugOnly($"Given a shoot to {targetUUID}, damage: {damage}", LogType.Gameplay);
        if (targetUUID != PlayerManager.Instance.UUID)
        {
            MapManager.Instance.RemotePlayerShootGiven(targetUUID, damage);
        }
    }

    private static void HandleRoomDeleted(JObject msg)
    {
        string roomId = msg.Value<string>("roomId");
        LogManager.Log($"Room {roomId} has been deleted.", LogType.Network);
        CoroutineRunner.Instance.Run(HandleRoomDeletedEnum(roomId));
    }

    private static IEnumerator HandleRoomDeletedEnum(string roomId)
    {
        PlayerManager.Instance.SetState(PlayerState.Lobby);
        ESCManager.Instance.CloseESCMenu();
        yield return new WaitForSeconds(1f);

        if (MapManager.IsAlive)
            MapManager.Instance.ResetMapState();

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: false);
        HUDManager.Instance.UpdateHealth(100);
        HUDManager.Instance.ShowHUD(false);
        GUIManager.Instance.SetGUIOpen(true);
        PlayerManager.Instance.gameObject.transform.Find("Player").gameObject.transform.position = new Vector3(0, 0, 0);
        GUIManager.Instance.ShowPanel(GUIManager.Instance.lobbyMatchFound, false);
        yield return new WaitForSeconds(1f);
        PopupManager.Instance.Open(PopupType.RoomDeleted);
    }

    private static IEnumerator HandleRoomInactiveEnum(string roomId)
    {
        PlayerManager.Instance.SetState(PlayerState.Lobby);
        ESCManager.Instance.CloseESCMenu();
        yield return new WaitForSeconds(1f);

        if (MapManager.IsAlive)
            MapManager.Instance.ResetMapState();

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: false);

        HUDManager.Instance.UpdateHealth(100);
        HUDManager.Instance.ShowHUD(false);

        GUIManager.Instance.SetGUIOpen(true);
        GUIManager.Instance.ShowPanel(GUIManager.Instance.lobbyMatchFound, false);

        var player = PlayerManager.Instance.gameObject.transform.Find("Player")?.gameObject;
        if (player != null)
        {
            var movement = player.GetComponent<ThirdPersonMovement>();
            if (movement != null)
                movement.enabled = false;

            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            if (movement != null)
                movement.enabled = true;
        }

        yield return new WaitForSeconds(1f);

        PopupManager.Instance.Open(PopupType.RoomInactive);
    }

    private static void HandleAdminMessage(JObject msg)
    {
        string message = msg.Value<string>("content");
        PopupManager.Instance.Open("Admin Message", message);
        LogManager.Log($"Admin message: {message}", LogType.Network);
    }

    private static void DrawGizmoDebugShoot(JObject msg)
    {
        if (!MapManager.IsAlive) return;

        var visualizer = Debugger.Instance;
        if (visualizer != null)
            visualizer.AddDebugShot(msg);
        LogManager.LogDebugOnly("Added debug shoot info.", LogType.System);
    }

    private static Vector3 ToVector3(JToken token)
    {
        if (token == null) return Vector3.zero;
        return new Vector3(
            token.Value<float>("x"),
            token.Value<float>("y"),
            token.Value<float>("z")
        );
    }
}
