using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public List<WorldSaveData> worlds = new();
}

[Serializable]
public class WorldSaveData
{
    public string worldId;
    public bool isUnlocked;
    public List<StageSaveData> stages = new();
}

[Serializable]
public class StageSaveData
{
    public string stageId;
    public bool isUnlocked;
    public bool isCleared;
    public int collectItemTotal;
    public List<string> collectedIds = new();
}