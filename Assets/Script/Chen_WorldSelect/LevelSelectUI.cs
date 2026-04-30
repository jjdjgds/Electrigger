using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button imageButton;

    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image previewImage;

    [Header("Data")]
    [SerializeField] private SelectItemData[] items;

    [Header("Text Prefix")]
    [SerializeField] private string numberPrefix = "No.";

    private int currentIndex = 0;

    public UnityEvent<int, SelectItemData> onSelected;

    private void Awake()
    {
        Debug.Log($"{gameObject.name} LevelSelectUI Awake");

        prevButton.onClick.AddListener(Prev);
        nextButton.onClick.AddListener(Next);
        imageButton.onClick.AddListener(Confirm);
    }

    private void Start()
    {
        UpdateView();
    }

    private void Prev()
    {
        if (items == null || items.Length == 0) return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = items.Length - 1;

        UpdateView();
    }

    private void Next()
    {
        Debug.Log($"{gameObject.name} Next Clicked");

        if (items == null || items.Length == 0) return;

        currentIndex++;

        if (currentIndex >= items.Length)
            currentIndex = 0;

        UpdateView();
    }

    private void UpdateView()
    {
        if (items == null || items.Length == 0) return;

        SelectItemData item = items[currentIndex];

        numberText.text = $"{numberPrefix} {currentIndex + 1}";
        nameText.text = item.displayName;
        previewImage.sprite = item.previewImage;
    }

    private void Confirm()
    {
        if (items == null || items.Length == 0) return;

        onSelected?.Invoke(currentIndex, items[currentIndex]);
    }

    public void SetItems(SelectItemData[] newItems)
    {
        items = newItems;
        currentIndex = 0;
        UpdateView();
    }
}