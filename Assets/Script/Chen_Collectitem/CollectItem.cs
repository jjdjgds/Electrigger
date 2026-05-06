using UnityEngine;

public class CollectItem : MonoBehaviour
{
    [Header("ID")]
    [SerializeField] private string collectId;

    [Header("Visual Settings")]
    [SerializeField] private float collectedAlpha = 0.5f;

    [Header("Float Settings")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatHeight = 0.2f;

    [Header("Flip Settings")]
    [SerializeField] private float flipSpeed = 4f;

    public string CollectId => collectId;

    private CollectItemManager manager;
    private SpriteRenderer spriteRenderer;

    private Vector3 startLocalPos;
    private Vector3 startLocalScale;

    private bool canCollect = true;
    private bool isAnimationActive = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startLocalPos = transform.localPosition;
        startLocalScale = transform.localScale;
    }

    private void Update()
    {
        if (!isAnimationActive) return;

        UpdateFloatAnimation();
        UpdateFlipAnimation();
    }

    public void Initialize(CollectItemManager owner)
    {
        manager = owner;

        startLocalPos = transform.localPosition;
        startLocalScale = transform.localScale;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canCollect) return;

        if (collision.CompareTag("Player"))
        {
            manager?.Collect(this);
        }
    }

    private void UpdateFloatAnimation()
    {
        float y = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = startLocalPos + new Vector3(0, y, 0);
    }

    private void UpdateFlipAnimation()
    {
        float t = (Mathf.Sin(Time.time * flipSpeed) + 1f) * 0.5f;
        float flipScale = Mathf.Lerp(-1f, 1f, t);
        transform.localScale = new Vector3(
            startLocalScale.x * flipScale,
            startLocalScale.y,
            startLocalScale.z);
    }

    public void HideAfterCollected()
    {
        gameObject.SetActive(false);
    }

    public void SetAsCollectedVisual()
    {
        SetAlpha(collectedAlpha);
    }

    public void SetAsNormalVisual()
    {
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}