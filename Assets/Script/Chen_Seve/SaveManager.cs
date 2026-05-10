using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private Key deleteSaveKey = Key.F12;

    public static SaveManager Instance { get; private set; }

    private GameSaveData saveData = new GameSaveData();

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        Cursor.visible = true;
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadOrCreate(SelectItemData[] worldItems, SelectItemData[][] stageGroups)
    {
        if (File.Exists(SavePath))
        {
            Load();
            return;
        }

        CreateInitialSaveData(worldItems, stageGroups);
        Save();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Save Complete: {SavePath}");
    }

    public void Load()
    {
        string json = File.ReadAllText(SavePath);
        saveData = JsonUtility.FromJson<GameSaveData>(json);

        if (saveData == null)
        {
            saveData = new GameSaveData();
        }
    }

    private void CreateInitialSaveData(
        SelectItemData[] worldItems,
        SelectItemData[][] stageGroups)
    {
        saveData = new GameSaveData();

        for (int worldIndex = 0; worldIndex < worldItems.Length; worldIndex++)
        {
            SelectItemData worldItem = worldItems[worldIndex];

            WorldSaveData world = new WorldSaveData
            {
                worldId = worldItem.worldId,
                isUnlocked = worldIndex == 0
            };

            SelectItemData[] stages = stageGroups[worldIndex];

            for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
            {
                SelectItemData stageItem = stages[stageIndex];

                StageSaveData stage = new StageSaveData
                {
                    stageId = stageItem.stageId,
                    isUnlocked = worldIndex == 0 && stageIndex == 0,
                    isCleared = false,
                    collectItemTotal = stageItem.collectItemTotal
                };

                world.stages.Add(stage);
            }

            saveData.worlds.Add(world);
        }
    }

    private WorldSaveData GetWorld(string worldId)
    {
        return saveData.worlds.Find(w => w.worldId == worldId);
    }

    private StageSaveData GetStage(string worldId, string stageId)
    {
        WorldSaveData world = GetWorld(worldId);
        if (world == null) return null;

        return world.stages.Find(s => s.stageId == stageId);
    }

    public bool IsWorldUnlocked(string worldId)
    {
        WorldSaveData world = GetWorld(worldId);
        return world != null && world.isUnlocked;
    }

    public bool IsStageUnlocked(string worldId, string stageId)
    {
        StageSaveData stage = GetStage(worldId, stageId);
        return stage != null && stage.isUnlocked;
    }

    public bool IsStageCleared(string worldId, string stageId)
    {
        StageSaveData stage = GetStage(worldId, stageId);
        return stage != null && stage.isCleared;
    }

    public void AddCollectedItem(string worldId, string stageId, string collectId)
    {
        StageSaveData stage = GetStage(worldId, stageId);
        if (stage == null) return;

        if (!stage.collectedIds.Contains(collectId))
        {
            stage.collectedIds.Add(collectId);
            Save();
        }
    }

    public HashSet<string> GetCollectedIds(string worldId, string stageId)
    {
        StageSaveData stage = GetStage(worldId, stageId);

        if (stage == null)
            return new HashSet<string>();

        return new HashSet<string>(stage.collectedIds);
    }

    public int GetStageCollectedCount(string worldId, string stageId)
    {
        StageSaveData stage = GetStage(worldId, stageId);
        return stage == null ? 0 : stage.collectedIds.Count;
    }

    public int GetStageCollectTotal(string worldId, string stageId)
    {
        StageSaveData stage = GetStage(worldId, stageId);
        return stage == null ? 0 : stage.collectItemTotal;
    }

    public int GetWorldCollectedCount(string worldId)
    {
        WorldSaveData world = GetWorld(worldId);
        if (world == null) return 0;

        int count = 0;

        foreach (StageSaveData stage in world.stages)
        {
            count += stage.collectedIds.Count;
        }

        return count;
    }

    public int GetWorldCollectTotal(string worldId)
    {
        WorldSaveData world = GetWorld(worldId);
        if (world == null) return 0;

        int total = 0;

        foreach (StageSaveData stage in world.stages)
        {
            total += stage.collectItemTotal;
        }

        return total;
    }

    public void ClearStage(string worldId, string stageId)
    {
        StageSaveData stage = GetStage(worldId, stageId);
        if (stage == null) return;

        stage.isCleared = true;

        UnlockNextStageOrWorld(worldId, stageId);

        Save();
    }

    private void UnlockNextStageOrWorld(string worldId, string stageId)
    {
        int worldIndex = saveData.worlds.FindIndex(w => w.worldId == worldId);
        if (worldIndex < 0) return;

        WorldSaveData world = saveData.worlds[worldIndex];

        int stageIndex = world.stages.FindIndex(s => s.stageId == stageId);
        if (stageIndex < 0) return;

        int nextStageIndex = stageIndex + 1;

        if (nextStageIndex < world.stages.Count)
        {
            world.stages[nextStageIndex].isUnlocked = true;
            return;
        }

        foreach (StageSaveData stage in world.stages)
        {
            if (!stage.isCleared)
                return;
        }

        int nextWorldIndex = worldIndex + 1;

        if (nextWorldIndex < saveData.worlds.Count)
        {
            WorldSaveData nextWorld = saveData.worlds[nextWorldIndex];
            nextWorld.isUnlocked = true;

            if (nextWorld.stages.Count > 0)
                nextWorld.stages[0].isUnlocked = true;
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[deleteSaveKey].wasPressedThisFrame)
        {
            DeleteSaveFile();
        }
    }

    public void DeleteSaveFile()
{
    if (File.Exists(SavePath))
    {
        File.Delete(SavePath);
        Debug.Log("Save file deleted.");
    }
    else
    {
        Debug.Log("Save file does not exist.");
    }

    saveData = new GameSaveData();
}

#if UNITY_EDITOR
    public void DeleteSaveForDebug()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Save Deleted.");
        }
    }
#endif
}