using System.Collections;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Main HUD")]
    [SerializeField] private GameObject mainHUDObject;

    [Header("Health UI Elements")]
    [SerializeField] private RectTransform healthBarRect;
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Guns HUD")]
    [SerializeField] private GameObject hands;
    [SerializeField] private GameObject littleGun;
    [SerializeField] private GameObject bigGun;

    [Header("Guns References")]
    [SerializeField] private Sprite[] gunsSprites;
    [SerializeField] private Sprite selectedButton;
    [SerializeField] private Sprite unselectedButton;

    [Header("Botton Right - Info Gun")]
    [SerializeField] private TextMeshProUGUI gunName;
    [SerializeField] private TextMeshProUGUI ammoCount;

    [Header("Predator Crosshair")]
    [SerializeField] private CanvasGroup predatorCrosshairGroup;
    [SerializeField] private float crosshairFadeSpeed = 5f;

    private Coroutine crosshairCoroutine;

    private float fullWidth = 434.05f;
    private float emptyWidth = 0f;
    private float fullPosX = 0f;
    private float emptyPosX = -216.8058f;

    private float currentHealthPercent = 1f;
    private float targetHealthPercent = 1f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (mainHUDObject != null)
        {
            ShowHUD(false);
        }
    }

    void Update()
    {
        currentHealthPercent = Mathf.Lerp(currentHealthPercent, targetHealthPercent, Time.deltaTime * smoothSpeed);

        float newWidth = Mathf.Lerp(emptyWidth, fullWidth, currentHealthPercent);
        float newPosX = Mathf.Lerp(emptyPosX, fullPosX, currentHealthPercent);

        healthBarRect.sizeDelta = new Vector2(newWidth, healthBarRect.sizeDelta.y);
        healthBarRect.anchoredPosition = new Vector2(newPosX, healthBarRect.anchoredPosition.y);
    }

    public void ShowHUD(bool show)
    {
        if (mainHUDObject != null)
        {
            mainHUDObject.SetActive(show);
        }
    }

    public void UpdateHealth(int HP)
    {
        PlayerManager.Instance.SetHP(HP);

        targetHealthPercent = Mathf.Clamp01(HP / 100f);
    }

    public void UpdateGunName(string name)
    {
        if (gunName != null)
        {
            gunName.text = name;
        }
    }

    public void UpdateSelectedGun(int index)
    {
        switch (index)
        {
            case 0:
                hands.GetComponent<UnityEngine.UI.Image>().sprite = selectedButton;
                littleGun.GetComponent<UnityEngine.UI.Image>().sprite = unselectedButton;
                bigGun.GetComponent<UnityEngine.UI.Image>().sprite = unselectedButton;
                break;
            case 1:
                hands.GetComponent<UnityEngine.UI.Image>().sprite = unselectedButton;
                littleGun.GetComponent<UnityEngine.UI.Image>().sprite = selectedButton;
                bigGun.GetComponent<UnityEngine.UI.Image>().sprite = unselectedButton;
                break;
            case 2:
                hands.GetComponent<UnityEngine.UI.Image>().sprite = unselectedButton;
                littleGun.GetComponent<UnityEngine.UI.Image>().sprite = unselectedButton;
                bigGun.GetComponent<UnityEngine.UI.Image>().sprite = selectedButton;
                break;
            default:
                break;
        }
    }

    public void UpdateAmmoCount(int ammo)
    {
        if (ammoCount != null)
        {
            ammoCount.text = ammo.ToString();
        }
    }

    public void ShowPredatorCrosshair(bool show)
    {
        if (crosshairCoroutine != null)
            StopCoroutine(crosshairCoroutine);

        crosshairCoroutine = StartCoroutine(FadePredatorCrosshair(show));
    }

    private IEnumerator FadePredatorCrosshair(bool show)
    {
        if (predatorCrosshairGroup == null)
            yield break;

        float targetAlpha = show ? 1f : 0f;

        if (show)
        {
            predatorCrosshairGroup.gameObject.SetActive(true);
            SettingsManager.Instance.crosshairHUD.SetActive(false);
        }

        while (!Mathf.Approximately(predatorCrosshairGroup.alpha, targetAlpha))
        {
            predatorCrosshairGroup.alpha = Mathf.MoveTowards(
                predatorCrosshairGroup.alpha,
                targetAlpha,
                Time.deltaTime * crosshairFadeSpeed
            );
            yield return null;
        }

        if (!show)
        {
            predatorCrosshairGroup.gameObject.SetActive(false);
            SettingsManager.Instance.crosshairHUD.SetActive(true);
        }
    }
}
