using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI§À•ﬁ•¶•π§ÚÅ\§ª§øïr§Œíà¥Û—›≥ˆ§Ú––§¶•Ø•È•π
/// </summary>
public class UIHoverScaleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float duration = 0.12f;

    private Vector3 defaultScale;
    private Coroutine scaleCoroutine;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartScale(defaultScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartScale(defaultScale);
    }

    private void StartScale(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
    }

    private IEnumerator ScaleCoroutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }
}