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
        // 初期状態はワールド選択
        ShowWorldSelect();

        // イベントリスナーの登録
        worldSelector.onSelected.AddListener(OnWorldSelected);
        stageSelector.onSelected.AddListener(OnStageSelected);

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

    // ワールド選択時の処理
    private void OnWorldSelected(int worldIndex, SelectItemData worldData)
    {
        currentWorldIndex = worldIndex;

        ShowStageSelect();

        // ステージデータの切り替え
        switch (worldIndex)
        {
            case 0:
                stageSelector.SetItems(world1Stages);
                break;

            case 1:
                stageSelector.SetItems(world2Stages);
                break;

            default:
                Debug.LogWarning("No stage data for this world.");
                break;
        }
    }

    // ステージ選択時の処理
    private void OnStageSelected(int stageIndex, SelectItemData stageData)
    {
        if (string.IsNullOrEmpty(stageData.targetSceneName))
        {
            Debug.LogWarning("Target scene name is empty.");
            return;
        }

        // 選択されたステージの情報を保存
        LevelSelectReturnData.SetCurrentStage(currentWorldIndex, stageIndex);

        SceneManager.LoadScene(stageData.targetSceneName);
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

        switch (currentWorldIndex)
        {
            case 0:
                stageSelector.SetItems(world1Stages);
                break;

            case 1:
                stageSelector.SetItems(world2Stages);
                break;
        }

        stageSelector.SetIndex(LevelSelectReturnData.currentStageIndex);
    }
}