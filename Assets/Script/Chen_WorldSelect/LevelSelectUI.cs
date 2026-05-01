using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// レベル選択UI
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    // UI参照
    [Header("UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button imageButton;

    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image previewImage;

    // データ
    [Header("Data")]
    [SerializeField] private SelectItemData[] items;

    [Header("Text Prefix")]
    [SerializeField] private string numberPrefix = "No.";

    private int currentIndex = 0;

    public UnityEvent<int, SelectItemData> onSelected;

    private void Awake()
    {
        // ボタン登録
        prevButton.onClick.AddListener(Prev);
        nextButton.onClick.AddListener(Next);
        imageButton.onClick.AddListener(Confirm);
    }

    private void Start()
    {
        UpdateView();
    }

    // 前へ
    private void Prev()
    {
        if (!HasItems()) return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = items.Length - 1;

        UpdateView();
    }

    // 次へ
    private void Next()
    {
        if (!HasItems()) return;

        currentIndex++;

        if (currentIndex >= items.Length)
            currentIndex = 0;

        UpdateView();
    }

    // UI更新
    private void UpdateView()
    {
        if (!HasItems()) return;

        SelectItemData item = items[currentIndex];

        numberText.text = $"{numberPrefix} {currentIndex + 1}";
        nameText.text = item.displayName;
        previewImage.sprite = item.previewImage;
    }

    // 確定
    private void Confirm()
    {
        if (!HasItems()) return;

        onSelected?.Invoke(currentIndex, items[currentIndex]);
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