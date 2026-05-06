using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CollectItemManager : MonoBehaviour
{
    public static CollectItemManager Instance { get; private set; }

    [Header("Stage ID")]
    [SerializeField] private string worldId = "World1";
    [SerializeField] private string stageId = "World1_Stage1";

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
        if (!string.IsNullOrEmpty(LevelSelectReturnData.currentWorldId))
            worldId = LevelSelectReturnData.currentWorldId;

        if (!string.IsNullOrEmpty(LevelSelectReturnData.currentStageId))
            stageId = LevelSelectReturnData.currentStageId;

        RegisterAllCollectItems();
        LoadCollectedData();
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
                Debug.LogWarning($"{item.name} µÄ CollectId Îª¿Õ");
                continue;
            }

            if (collectItems.ContainsKey(id))
            {
                Debug.LogWarning($"CollectId ÖØ¸´£º{id}");
                continue;
            }

            collectItems.Add(id, item);
            item.Initialize(this);
        }
    }

    private void LoadCollectedData()
    {
        collectedIds.Clear();

        if (SaveManager.Instance == null) return;

        HashSet<string> savedIds = SaveManager.Instance.GetCollectedIds(worldId, stageId);

        foreach (string id in savedIds)
        {
            collectedIds.Add(id);
        }
    }

    public void Collect(CollectItem item)
    {
        if (item == null) return;

        string id = item.CollectId;

        if (!collectedIds.Contains(id))
        {
            collectedIds.Add(id);

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.AddCollectedItem(worldId, stageId, id);
            }
        }

        item.HideAfterCollected();

        UpdateCollectUI();

        Debug.Log($"Collect£º{id}");
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
        if (collectCountText == null) return;

        int current = collectedIds.Count;
        int total = collectItems.Count;

        collectCountText.text = $"{current} / {total}";
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