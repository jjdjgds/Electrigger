using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ワールドとステージ
/// </summary>
public class WorldStageSelectManager : MonoBehaviour
{
    // UI参照
    [Header("Panels")]
    [SerializeField] private GameObject worldSelectPanel;
    [SerializeField] private GameObject stageSelectPanel;

    [Header("Selectors")]
    [SerializeField] private LevelSelectUI worldSelector;
    [SerializeField] private LevelSelectUI stageSelector;

    [Header("World Data")]
    [SerializeField] private SelectItemData[] worldItems;

    // ワールドデータ
    [Header("Stage Data Per World")]
    [SerializeField] private SelectItemData[] world1Stages;
    [SerializeField] private SelectItemData[] world2Stages;

    private int currentWorldIndex = 0;// ワールドインデックスを保持

    // UI状態管理
    private enum UIState
    {
        World,
        Stage
    }

    private UIState currentState;// 現在のUI状態


    private void Start()
    {
        InitializeSave();

        worldSelector.onSelected.AddListener(OnWorldSelected);
        stageSelector.onSelected.AddListener(OnStageSelected);

        worldSelector.SetItems(worldItems);

        // ステージ復帰処理
        if (LevelSelectReturnData.shouldReturnToStageSelect)
        {
            RestoreStageSelect();
            LevelSelectReturnData.ClearReturnRequest();
        }
        else
        {
            ShowWorldSelect();
        }
    }


    private void InitializeSave()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveManager not found.");
            return;
        }

        SelectItemData[][] stageGroups =
        {
            world1Stages,
            world2Stages
        };

        SaveManager.Instance.LoadOrCreate(worldItems, stageGroups);
    }

    // ワールド選択時の処理
    private void OnWorldSelected(int worldIndex, SelectItemData worldData)
    {
        if (SaveManager.Instance != null &&
             !SaveManager.Instance.IsWorldUnlocked(worldData.worldId))
        {
            Debug.Log("World is locked.");
            return;
        }

        currentWorldIndex = worldIndex;

        ShowStageSelect();

        stageSelector.SetItems(GetStagesByWorldIndex(worldIndex));
    }

    // ステージ選択時の処理
    private void OnStageSelected(int stageIndex, SelectItemData stageData)
    {
        if (SaveManager.Instance != null &&
            !SaveManager.Instance.IsStageUnlocked(stageData.worldId, stageData.stageId))
        {
            Debug.Log("Stage is locked.");
            return;
        }

        if (string.IsNullOrEmpty(stageData.targetSceneName))
        {
            Debug.LogWarning("Target scene name is empty.");
            return;
        }

        LevelSelectReturnData.SetCurrentStage(
            currentWorldIndex,
            stageIndex,
            stageData.worldId,
            stageData.stageId);

        SceneManager.LoadScene(stageData.targetSceneName);
    }

    private SelectItemData[] GetStagesByWorldIndex(int worldIndex)
    {
        switch (worldIndex)
        {
            case 0:
                return world1Stages;

            case 1:
                return world2Stages;

            default:
                return new SelectItemData[0];
        }
    }


    public void ShowWorldSelect()
    {
        worldSelectPanel.SetActive(true);
        stageSelectPanel.SetActive(false);
        currentState = UIState.World;
    }

    public void ShowStageSelect()
    {
        worldSelectPanel.SetActive(false);
        stageSelectPanel.SetActive(true);
        currentState = UIState.Stage;
    }

    public void OnBackButton()
    {
        if (currentState == UIState.Stage)
        {
            ShowWorldSelect();
        }
        else
        {
            Debug.Log("Back to Title");
        }
    }

    private void RestoreStageSelect()
    {
        currentWorldIndex = LevelSelectReturnData.currentWorldIndex;

        ShowStageSelect();

        stageSelector.SetItems(GetStagesByWorldIndex(currentWorldIndex));
        stageSelector.SetIndex(LevelSelectReturnData.currentStageIndex);
    }
}