using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// ･・ﾙ･・x談UI
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    // UIｲﾎﾕﾕ
    [Header("UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button imageButton;

    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text collectText;
    [SerializeField] private Image previewImage;

    // ･ﾇｩ`･ｿ
    [Header("Data")]
    [SerializeField] private SelectItemData[] items;

    [Header("Text Prefix")]
    [SerializeField] private string numberPrefix = "No.";

    [Header("Mode")]
    [SerializeField] private bool isWorldSelector = false;

    private int currentIndex = 0;

    public UnityEvent<int, SelectItemData> onSelected;

    private void Awake()
    {
        prevButton.onClick.AddListener(Next); // 上ボタン → 数字増加
        nextButton.onClick.AddListener(Prev); // 下ボタン → 数字減少
        imageButton.onClick.AddListener(Confirm);
    }

    private void Start()
    {
        UpdateView();
    }

    // ﾇｰ､ﾘ
    private void Prev()
    {
        if (!HasItems()) return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = items.Length - 1;

        UpdateView();
    }

    // ｴﾎ､ﾘ
    private void Next()
    {
        if (!HasItems()) return;

        currentIndex++;

        if (currentIndex >= items.Length)
            currentIndex = 0;

        UpdateView();
    }

    // UIｸ・?
    private void UpdateView()
    {
        if (!HasItems()) return;

        SelectItemData item = items[currentIndex];

        bool unlocked = IsUnlocked(item);

        numberText.text = $"{numberPrefix} {currentIndex + 1}";

        if (unlocked)
        {
            bool cleared = !isWorldSelector &&
                           SaveManager.Instance != null &&
                           SaveManager.Instance.IsStageCleared(item.worldId, item.stageId);

            nameText.text = cleared ? $"{item.displayName} CLEAR" : item.displayName;
        }
        else
        {
            nameText.text = "???";
        }

        previewImage.sprite = item.previewImage;
        previewImage.color = unlocked ? Color.white : Color.gray;

        UpdateCollectText(item, unlocked);
    }

    private void UpdateCollectText(SelectItemData item, bool unlocked)
    {
        if (collectText == null) return;

        if (!unlocked)
        {
            collectText.text = "? / ?";
            return;
        }

        if (SaveManager.Instance == null)
        {
            collectText.text = "0 / 0";
            return;
        }

        if (isWorldSelector)
        {
            int current = SaveManager.Instance.GetWorldCollectedCount(item.worldId);
            int total = SaveManager.Instance.GetWorldCollectTotal(item.worldId);
            collectText.text = $"{current} / {total}";
        }
        else
        {
            int current = SaveManager.Instance.GetStageCollectedCount(item.worldId, item.stageId);
            int total = SaveManager.Instance.GetStageCollectTotal(item.worldId, item.stageId);
            collectText.text = $"{current} / {total}";
        }
    }

    // ｴ_ｶｨ
    private void Confirm()
    {
        if (!HasItems()) return;

        SelectItemData item = items[currentIndex];

        if (!IsUnlocked(item))
        {
            Debug.Log("This item is locked.");
            return;
        }

        onSelected?.Invoke(currentIndex, item);
    }

    private bool IsUnlocked(SelectItemData item)
    {
        if (SaveManager.Instance == null)
            return true;

        if (isWorldSelector)
            return SaveManager.Instance.IsWorldUnlocked(item.worldId);

        return SaveManager.Instance.IsStageUnlocked(item.worldId, item.stageId);
    }

    private bool HasItems()
    {
        return items != null && items.Length > 0;
    }

    public void SetItems(SelectItemData[] newItems)
    {
        items = newItems;
        currentIndex = 0;
        UpdateView();
    }

    public void SetIndex(int index)
    {
        if (items == null || items.Length == 0) return;

        currentIndex = Mathf.Clamp(index, 0, items.Length - 1);
        UpdateView();
    }
}