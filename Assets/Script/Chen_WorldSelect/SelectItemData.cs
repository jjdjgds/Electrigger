using UnityEngine;


[System.Serializable]
public class SelectItemData
{
    [Header("Basic")]
    public string displayName;
    public Sprite previewImage;
    public string targetSceneName;

    [Header("Save ID")]
    public string worldId;
    public string stageId;

    [Header("Collect")]
    public int collectItemTotal;
}

public static class LevelSelectReturnData
{
    public static int currentWorldIndex = 0;
    public static int currentStageIndex = 0;
    public static bool shouldReturnToStageSelect = false;

    public static string currentWorldId = "";
    public static string currentStageId = "";

    public static void SetCurrentStage(int worldIndex, int stageIndex, string worldId, string stageId)
    {
        currentWorldIndex = worldIndex;
        currentStageIndex = stageIndex;
        currentWorldId = worldId;
        currentStageId = stageId;
    }

    public static void RequestReturnToStageSelect()
    {
        shouldReturnToStageSelect = true;
    }

    public static void ClearReturnRequest()
    {
        shouldReturnToStageSelect = false;
    }
}