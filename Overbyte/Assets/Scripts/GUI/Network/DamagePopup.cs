using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    private TMP_Text damageText;
    private Transform cam;
    private float duration = 1.2f;
    private float fadeSpeed = 2f;

    public void Initialize(Camera camera)
    {
        cam = camera.transform;
        damageText = transform.Find("Damage")?.GetComponent<TMP_Text>();
        StartCoroutine(AnimatePopup());
    }

    void LateUpdate()
    {
        if (cam == null) return;
        transform.LookAt(transform.position + cam.rotation * Vector3.forward,
                         cam.rotation * Vector3.up);
    }

    private IEnumerator AnimatePopup()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * 1.5f;
        float elapsed = 0f;

        Color startColor = damageText.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, endPos, t);

            // Fade out
            if (damageText != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t * fadeSpeed);
                damageText.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
