using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class GameLoader : MonoBehaviour
{
    private GameObject gameLoaderPanel;
    private TMP_Text statusText;
    private string apiToken;

    private Dictionary<string, LocalizedText> texts = new Dictionary<string, LocalizedText>();

    void Start()
    {
        gameLoaderPanel = GUIManager.Instance.gameLoaderGUI;
        statusText = GUIManager.Instance.gameLoaderGUI.GetComponentInChildren<TMP_Text>();

        string[] keys = new string[]
        {
            "gameloader_init",
            "gameloader_version",
            "gameloader_versionok",
            "gameloader_versiondifferent",
            "gameloader_versionerror",
            "gameloader_versionretry",
            "gameloader_api",
            "gameloader_apiok",
            "gameloader_apierror",
            "gameloader_apiretry",
            "gameloader_websocket",
            "gameloader_websocketok",
            "gameloader_websocketerror",
            "gameloader_websocketretry",
            "gameloader_websocketerrorgeneral",
            "gameloader_lobby"
        };

        foreach (var key in keys)
            texts[key] = new LocalizedText(key);

        GUIManager.Instance.ShowPanel(gameLoaderPanel, true);
        GUIManager.Instance.SetGUIOpen(true);

        StartCoroutine(GameLoaderEnum());
    }

    void OnDestroy()
    {
        if (gameLoaderPanel != null && GUIManager.Instance != null)
        {
            GUIManager.Instance.ShowPanel(gameLoaderPanel, false);
        }
    }

    private IEnumerator GameLoaderEnum()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateStatus(texts["gameloader_init"].Value);
        yield return new WaitForSeconds(1f);
        gameLoaderPanel.gameObject.transform.Find("VersionText").GetComponent<TMP_Text>().text = $"v{Application.version}";
        LogManager.Log("Game Loader initialized.", LogType.System);

        UpdateStatus(texts["gameloader_version"].Value);
        yield return CheckGameVersion();

        UpdateStatus(texts["gameloader_api"].Value);
        yield return ConnectToAPI();

        UpdateStatus(texts["gameloader_websocket"].Value);
        yield return ConnectToWebSocket();

        if (NetworkManager.Instance.HasError)
        {
            UpdateStatus(texts["gameloader_websocketerrorgeneral"].Value);
            yield break;
        }

        UpdateStatus(texts["gameloader_lobby"].Value);
        yield return new WaitForSeconds(0.5f);

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: true);
        PlayerManager.Instance.SetState(PlayerState.Lobby);
    }

    private IEnumerator CheckGameVersion()
    {
        yield return new WaitForSeconds(1f);

        using (UnityWebRequest www = UnityWebRequest.Get($"{NetworkManager.Instance.apiUrl}/game/version"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                LogManager.Log($"Version Check Error: {www.error}", LogType.Error);
                UpdateStatus(texts["gameloader_versionerror"].Value);

                bool retryPressed = false;

                PopupManager.Instance.Open(
                    PopupType.NotServer,
                    showRetry: true,
                    onRetry: () =>
                    {
                        UpdateStatus(texts["gameloader_versionretry"].Value);
                        retryPressed = true;
                    });

                yield return new WaitUntil(() => retryPressed);

                yield return CheckGameVersion();
                yield break;
            }

            var responseJson = www.downloadHandler.text;
            VersionResponse versionResponse = JsonUtility.FromJson<VersionResponse>(responseJson);

            string serverVersion = versionResponse.version.TrimStart('v');
            string clientVersion = Application.version;

            LogManager.LogDebugOnly($"Server version: {serverVersion}, Client version: {clientVersion}", LogType.Bootstrap);

            if (serverVersion != clientVersion)
            {
                UpdateStatus(texts["gameloader_versiondifferent"].Value);

                bool retryPressed = false;

                PopupManager.Instance.Open(
                    PopupType.DifferentVersion,
                    showRetry: true,
                    onRetry: () =>
                    {
                        UpdateStatus(texts["gameloader_versionretry"].Value);
                        retryPressed = true;
                    });

                yield return new WaitUntil(() => retryPressed);

                yield return CheckGameVersion();
                yield break;
            }

            UpdateStatus(texts["gameloader_versionok"].Value);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ConnectToAPI()
    {
        using (UnityWebRequest www = new UnityWebRequest($"{NetworkManager.Instance.apiUrl}/auth/login", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(new byte[0]);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                LogManager.Log($"API Error: {www.error}", LogType.Error);
                apiToken = null;

                bool retryPressed = false;

                UpdateStatus(texts["gameloader_apierror"].Value);
                PopupManager.Instance.Open(
                    PopupType.NotServer,
                    showRetry: true,
                    onRetry: () =>
                    {
                        UpdateStatus(texts["gameloader_apiretry"].Value);
                        retryPressed = true;
                    });

                yield return new WaitUntil(() => retryPressed);

                yield return ConnectToAPI();
                yield break;
            }
            else
            {
                var responseJson = www.downloadHandler.text;
                apiToken = JsonUtility.FromJson<TokenResponse>(responseJson).token;
                LogManager.LogDebugOnly("API connected. Token received: " + apiToken, LogType.Bootstrap);
                UpdateStatus(texts["gameloader_apiok"].Value);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private IEnumerator ConnectToWebSocket()
    {
        NetworkManager.Instance.ConnectToServer();

        float timeout = 5f;
        float elapsed = 0f;

        while (!NetworkManager.Instance.IsConnected && !NetworkManager.Instance.HasError)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                NetworkManager.Instance.Disconnect();
                NetworkManager.Instance.MarkError();
                break;
            }
            yield return null;
        }

        if (NetworkManager.Instance.HasError)
        {
            bool retryPressed = false;

            UpdateStatus(texts["gameloader_websocketerror"].Value);
            PopupManager.Instance.Open(
                PopupType.NotServer,
                showRetry: true,
                onRetry: () =>
                {
                    StartCoroutine(ConnectToWebSocket());
                    UpdateStatus(texts["gameloader_websocketretry"].Value);
                    retryPressed = true;
                });
            yield return new WaitUntil(() => retryPressed);

            yield return ConnectToWebSocket();
            yield break;
        }

        if (!string.IsNullOrEmpty(apiToken))
        {
            var authMessage = new WebSocketAuthMessage
            {
                type = "auth",
                token = apiToken
            };
            string authJson = JsonUtility.ToJson(authMessage);
            NetworkManager.Instance.Send(authJson);
        }

        UpdateStatus(texts["gameloader_websocketok"].Value);
        yield return new WaitForSeconds(0.5f);
        LogManager.Log("WebSocket connected.", LogType.Bootstrap);
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
            LogManager.Log($"[GameLoader] {text}", LogType.Bootstrap);
        }
    }

    [System.Serializable]
    private class TokenResponse
    {
        public string token;
    }

    [System.Serializable]
    private class WebSocketAuthMessage
    {
        public string type;
        public string token;
    }

    [System.Serializable]
    private class VersionResponse
    {
        public string version;
    }
}
