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
    private Vector3 basePos;

    private bool isAnimating = false;
    private bool isDragging = false;
    private Coroutine clickCoroutine;

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    // Plays the click animation if one is not already running.
    // クリックアニメーションが再生中でなければ開始します。
    public void PlayClickAnimation()
    {
        if (isAnimating)
            return;

        basePos = transform.localPosition;
        clickCoroutine = StartCoroutine(ClickRoutine());
    }


    // Stops the shake animation and restores the original local position.
    // シェイクアニメーションを停止し、ローカル座標を元に戻します。
    public void StopShake()
    {
        if (!isAnimating)
            return;

        if (clickCoroutine != null)
            StopCoroutine(clickCoroutine);

        transform.localPosition = basePos;
        isAnimating = false;
    }

    // Called when dragging starts.
    // Enlarges the monitor slightly while dragging.
    // ドラッグ開始時に呼び出します。
    // ドラッグ中は少し拡大表示します。
    public void OnDragStart()
    {
        isDragging = true;
        targetScale = originalScale * scaleFactor;
    }

    // Called when dragging ends.
    // Restores scale and stops any shake animation.
    // ドラッグ終了時に呼び出します。
    // スケールを戻し、シェイクを停止します。
    public void OnDragEnd()
    {
        isDragging = false;
        targetScale = originalScale;
        StopShake();
    }

    // Handles the temporary scale-up and shake effect.
    // 一時的な拡大とシェイク演出を処理します。
    private IEnumerator ClickRoutine()
    {
        isAnimating = true;
        targetScale = originalScale * scaleFactor;

        float elapsed = 0f;

        // Shake while the timer is active.
        // 一定時間シェイクを行います。
        while (elapsed < shakeDuration)
        {
            float offsetX = Mathf.Sin(elapsed * shakeSpeed) * shakeMagnitude;
            float offsetY = Mathf.Cos(elapsed * shakeSpeed * 1.3f) * shakeMagnitude;

            transform.localPosition = basePos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = basePos;

        // Only restore scale if dragging is no longer active.
        // まだドラッグ中でなければスケールを元に戻します。
        if (!isDragging)
            targetScale = originalScale;

        isAnimating = false;
    }
}