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
    private bool isDragging = false;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    public void PlayClickAnimation()
    {
        if (isAnimating) return;
        StartCoroutine(ClickRoutine());
    }

    // Åö Call this when drag starts
    public void OnDragStart()
    {
        isDragging = true;
        targetScale = originalScale * scaleFactor;
    }

    // Åö Call this when drag ends
    public void OnDragEnd()
    {
        isDragging = false;
        targetScale = originalScale;
    }

    IEnumerator ClickRoutine()
    {
        isAnimating = true;

        targetScale = originalScale * scaleFactor;

        // Shake
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

        transform.localPosition = basePos;

        // Only shrink back if not still dragging
        if (!isDragging)
            targetScale = originalScale;

        isAnimating = false;
    }
}