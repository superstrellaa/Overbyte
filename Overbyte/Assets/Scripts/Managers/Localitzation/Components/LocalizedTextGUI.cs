using TMPro;
using UnityEngine;

public class LocalizedTextGUI : MonoBehaviour
{
    [Header("Localization Key")]
    [SerializeField] private string localizationKey;
    private TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText == null)
        {
            LogManager.Log("LocalizedTextGUI requires a TMP_Text component.");
            return;
        }
        UpdateText();
        LocalitzationManager.OnLanguageChanged += UpdateText;
    }

    void OnDestroy()
    {
        LocalitzationManager.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            LogManager.Log("Localization key is not set.");
            return;
        }
        tmpText.text = LocalitzationManager.Instance.GetKey(localizationKey);
    }
}
