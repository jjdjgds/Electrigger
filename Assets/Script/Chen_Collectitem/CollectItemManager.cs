using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CollectItemManager : MonoBehaviour
{
    public static CollectItemManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text collectCountText;

    [Header("Reset Test")]
    public Key resetKey = Key.R;

    private readonly Dictionary<string, CollectItem> collectItems = new Dictionary<string, CollectItem>();

    private readonly HashSet<string> collectedIds = new HashSet<string>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RegisterAllCollectItems();
        RefreshAllCollectItems();
        UpdateCollectUI();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[resetKey].wasPressedThisFrame)
        {
            ResetCharacterTest();
        }
    }

    private void RegisterAllCollectItems()
    {
        collectItems.Clear();

        CollectItem[] items = Object.FindObjectsByType<CollectItem>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);


        foreach (CollectItem item in items)
        {
            if (item == null) continue;

            string id = item.CollectId;

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"{item.name} 的 CollectId 为空");
                continue;
            }

            if (collectItems.ContainsKey(id))
            {
                Debug.LogWarning($"CollectId 重复：{id}");
                continue;
            }

            collectItems.Add(id, item);
            item.Initialize(this);
        }
    }

    public void Collect(CollectItem item)
    {
        if (item == null) return;

        string id = item.CollectId;

        collectedIds.Add(id);

        item.HideAfterCollected();

        UpdateCollectUI();

        Debug.Log($"成功：{id}");
    }

    private void RefreshAllCollectItems()
    {
        foreach (var pair in collectItems)
        {
            string id = pair.Key;
            CollectItem item = pair.Value;

            item.gameObject.SetActive(true);

            if (collectedIds.Contains(id))
            {
                item.SetAsCollectedVisual();
            }
            else
            {
                item.SetAsNormalVisual();
            }
        }
    }

    private void UpdateCollectUI()
    {
        if (collectCountText != null)
        {
            collectCountText.text = $"AKKARIN: {collectedIds.Count}";
        }
    }

    private void ResetCharacterTest()
    {
        RefreshAllCollectItems();
        UpdateCollectUI();
    }

    public bool IsCollected(string id)
    {
        return collectedIds.Contains(id);
    }
}