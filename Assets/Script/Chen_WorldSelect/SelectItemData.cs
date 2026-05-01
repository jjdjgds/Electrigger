using UnityEngine;


[System.Serializable]
/// <summary>
/// •∑©`•Ûﬂxík•¢•§•∆•‡§Œ•«©`•ø•Ø•È•π
/// </summary>
public class SelectItemData
{
    public string displayName;
    public Sprite previewImage;
    public string targetSceneName;
}

/// <summary>
/// •∑©`•Ûﬂw“∆··§À•π•∆©`•∏ﬂxík§Àë¯§Î§ø§·§Œ•«©`•øπ‹¿Ì•Ø•È•π
/// </summary>
public static class LevelSelectReturnData
{
    public static int currentWorldIndex = 0;
    public static int currentStageIndex = 0;
    public static bool shouldReturnToStageSelect = false;

    public static void SetCurrentStage(int worldIndex, int stageIndex)
    {
        currentWorldIndex = worldIndex;
        currentStageIndex = stageIndex;
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