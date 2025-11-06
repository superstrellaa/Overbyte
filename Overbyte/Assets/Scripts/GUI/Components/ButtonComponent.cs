using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonComponent : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Colors")]
    public Color normalColor = new Color(0.9f, 0.2f, 0.2f);
    public Color hoverColor = Color.white;
    public Color pressedColor = new Color(0.6f, 0.1f, 0.1f);
    public float transitionTime = 0.2f;

    [Header("Disabled")]
    [Tooltip("Color visual cuando el botón está desactivado")]
    public Color disabledColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("Color de texto cuando está desactivado (si autoTextContrast está desactivado o quieres override)")]
    public Color disabledTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [Tooltip("Si true el botón arrancará desactivado")]
    public bool startDisabled = false;

    [Header("Text")]
    public TMP_Text buttonText;
    [Tooltip("Localization key used to fetch the text from LocalitzationManager")]
    public string localizationKey = "lobby_play";
    public float textTransitionTime = 0.2f;
    public float hoverSpacing = 50f;

    [Header("Scale")]
    public float hoverScale = 1.05f;
    public float scaleTransitionTime = 0.2f;

    [Header("Auto Text Color")]
    public bool autoTextContrast = true;
    public Color lightTextColor = Color.white;
    public Color darkTextColor = Color.black;

    [Header("Audio")]
    public SoundData clickSound;

    [Header("Image Tinting (optional)")]
    public Image iconImage;

    [Header("Click Image Swap (optional)")]
    public bool changeImageOnClick = false;
    public Sprite normalSprite;
    public Sprite pressedSprite;

    [Header("Hover Effects Enabled")]
    public bool enableHoverColor = true;
    public bool enableHoverSpacing = true;
    public bool enableHoverScale = true;

    [Header("Events")]
    public UnityEvent onClick;

    private Image background;
    private Button unityButton;

    private bool isHovered = false;
    private Coroutine colorRoutine;
    private Coroutine spacingRoutine;
    private Coroutine scaleRoutine;

    private Color originalTextColor;

    private float defaultSpacing = 0f;
    private Vector3 originalScale;

    private bool interactable = true;
    public bool IsInteractable => interactable;

    void Awake()
    {
        background = GetComponent<Image>();
        unityButton = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        originalScale = transform.localScale;
        if (buttonText != null)
        {
            defaultSpacing = buttonText.characterSpacing;
            originalTextColor = buttonText.color; 
        }

        UpdateText();
        LocalitzationManager.OnLanguageChanged += UpdateText;

        background.color = normalColor;
        if (autoTextContrast)
            SetTextContrast(normalColor);
        else if (buttonText != null)
            buttonText.color = originalTextColor;

        SetInteractable(!startDisabled, true);
    }

    void OnDestroy()
    {
        LocalitzationManager.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (buttonText == null) return;

        if (string.IsNullOrEmpty(localizationKey))
            // LogManager.Log("Localization key is not set for button " + gameObject.name);
            return;

        buttonText.text = LocalitzationManager.Instance.GetKey(localizationKey);
    }

    public void SetInteractable(bool enable, bool instant = false)
    {
        if (interactable == enable && !instant) return;

        interactable = enable;

        if (unityButton != null) unityButton.interactable = enable;

        if (background != null) background.raycastTarget = enable;

        ApplyInteractableVisuals(enable, instant);
    }

    private void ApplyInteractableVisuals(bool enabled, bool instant)
    {
        if (!enabled)
        {
            if (background != null) background.color = disabledColor;
            if (buttonText != null) buttonText.color = disabledTextColor;
            if (iconImage != null) iconImage.color = disabledTextColor;
        }
        else
        {
            if (instant)
            {
                if (background != null) background.color = normalColor;

                if (autoTextContrast)
                    SetTextContrast(normalColor);
                else if (buttonText != null)
                    buttonText.color = originalTextColor; 

                if (iconImage != null && !autoTextContrast)
                    iconImage.color = originalTextColor;

                transform.localScale = originalScale;
                if (buttonText != null) buttonText.characterSpacing = defaultSpacing;
            }
            else
            {
                if (enableHoverColor && background != null)
                    AnimateColor(background.color, normalColor);

                if (enableHoverSpacing && buttonText != null)
                    AnimateTextSpacing(buttonText.characterSpacing, defaultSpacing);

                if (enableHoverScale)
                    AnimateScale(transform.localScale, originalScale);

                if (!autoTextContrast && buttonText != null)
                    buttonText.color = originalTextColor;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;

        isHovered = true;

        if (enableHoverColor)
        {
            AnimateColor(background.color, hoverColor);
            SetTextContrast(hoverColor);
        }

        if (enableHoverSpacing)
            AnimateTextSpacing(buttonText.characterSpacing, hoverSpacing);

        if (enableHoverScale)
            AnimateScale(transform.localScale, originalScale * hoverScale);

        CursorManager.Instance?.SetCursor(CursorType.Pointer);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;

        isHovered = false;

        if (enableHoverColor)
        {
            AnimateColor(background.color, normalColor);
            SetTextContrast(normalColor);
        }

        if (enableHoverSpacing)
            AnimateTextSpacing(buttonText.characterSpacing, defaultSpacing);

        if (enableHoverScale)
            AnimateScale(transform.localScale, originalScale);

        CursorManager.Instance?.SetCursor(CursorType.Default);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable) return;

        AnimateColor(background.color, pressedColor);
        SetTextContrast(pressedColor);

        if (changeImageOnClick && pressedSprite != null)
            background.sprite = pressedSprite;

        if (clickSound != null)
            AudioManager.Instance.PlaySound(clickSound);

        CursorManager.Instance?.SetCursor(CursorType.Default);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactable) return;

        onClick?.Invoke();

        if (enableHoverColor)
        {
            AnimateColor(background.color, isHovered ? hoverColor : normalColor);
            SetTextContrast(isHovered ? hoverColor : normalColor);
        }

        if (changeImageOnClick && normalSprite != null)
            background.sprite = normalSprite;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!interactable) return;

        isHovered = true;

        if (enableHoverColor)
        {
            AnimateColor(background.color, hoverColor);
            SetTextContrast(hoverColor);
        }

        if (enableHoverSpacing)
            AnimateTextSpacing(buttonText.characterSpacing, hoverSpacing);

        if (enableHoverScale)
            AnimateScale(transform.localScale, originalScale * hoverScale);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!interactable) return;

        isHovered = false;

        if (enableHoverColor)
        {
            AnimateColor(background.color, normalColor);
            SetTextContrast(normalColor);
        }

        if (enableHoverSpacing)
            AnimateTextSpacing(buttonText.characterSpacing, defaultSpacing);

        if (enableHoverScale)
            AnimateScale(transform.localScale, originalScale);
    }

    private void AnimateColor(Color from, Color to)
    {
        if (!isActiveAndEnabled) return;
        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(LerpColor(from, to));
    }

    private IEnumerator LerpColor(Color from, Color to)
    {
        float t = 0f;
        while (t < transitionTime)
        {
            float easedT = EaseOutBack(t / transitionTime);
            if (background != null) background.color = Color.Lerp(from, to, easedT);
            t += Time.deltaTime;
            yield return null;
        }
        if (background != null) background.color = to;
        colorRoutine = null;
    }

    private void AnimateTextSpacing(float from, float to)
    {
        if (!isActiveAndEnabled) return;
        if (spacingRoutine != null) StopCoroutine(spacingRoutine);
        spacingRoutine = StartCoroutine(LerpSpacing(from, to));
    }

    private IEnumerator LerpSpacing(float from, float to)
    {
        float t = 0f;
        while (t < textTransitionTime)
        {
            float easedT = EaseOutBack(t / textTransitionTime);
            if (buttonText != null) buttonText.characterSpacing = Mathf.Lerp(from, to, easedT);
            t += Time.deltaTime;
            yield return null;
        }
        if (buttonText != null) buttonText.characterSpacing = to;
        spacingRoutine = null;
    }

    private void AnimateScale(Vector3 from, Vector3 to)
    {
        if (!isActiveAndEnabled) return;
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(LerpScale(from, to));
    }

    private IEnumerator LerpScale(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < scaleTransitionTime)
        {
            float easedT = EaseOutBack(t / scaleTransitionTime);
            transform.localScale = Vector3.Lerp(from, to, easedT);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = to;
        scaleRoutine = null;
    }

    private void SetTextContrast(Color bg)
    {
        if (buttonText != null && autoTextContrast)
        {
            float brightness = (bg.r * 0.299f + bg.g * 0.587f + bg.b * 0.114f);
            Color textColor = (brightness > 0.5f) ? darkTextColor : lightTextColor;
            buttonText.color = textColor;

            if (iconImage != null)
                iconImage.color = textColor;
        }
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
}
