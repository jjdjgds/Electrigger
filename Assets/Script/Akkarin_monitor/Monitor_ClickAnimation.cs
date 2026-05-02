using System.Collections;
using UnityEngine;

public class Monitor_ClickAnimation : MonoBehaviour
{
    [Header("Scale")]
    public float scaleFactor = 1.08f;
    public float scaleSpeed = 10f;

    [Header("Shake")]
    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 0.08f;
    public float shakeSpeed = 40f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isAnimating = false;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Smoothly interpolate scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    public void PlayClickAnimation()
    {
        if (isAnimating) return;
        StartCoroutine(ClickRoutine());
    }

    IEnumerator ClickRoutine()
    {
        isAnimating = true;

        // Scale up
        targetScale = originalScale * scaleFactor;

        // Shake while scaled up
        Vector3 basePos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float offsetX = Mathf.Sin(elapsed * shakeSpeed) * shakeMagnitude;
            float offsetY = Mathf.Cos(elapsed * shakeSpeed * 1.3f) * shakeMagnitude;
            transform.localPosition = basePos + new Vector3(offsetX, offsetY, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset position and scale
        transform.localPosition = basePos;
        targetScale = originalScale;

        isAnimating = false;
    }
}
