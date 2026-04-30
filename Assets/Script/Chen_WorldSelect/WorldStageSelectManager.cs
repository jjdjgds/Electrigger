using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldStageSelectManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject worldSelectPanel;
    [SerializeField] private GameObject stageSelectPanel;

    [Header("Selectors")]
    [SerializeField] private LevelSelectUI worldSelector;
    [SerializeField] private LevelSelectUI stageSelector;

    [Header("Stage Data Per World")]
    [SerializeField] private SelectItemData[] world1Stages;
    [SerializeField] private SelectItemData[] world2Stages;

    private enum UIState
    {
        World,
        Stage
    }

    private UIState currentState;


    private void Start()
    {
        ShowWorldSelect();

        worldSelector.onSelected.AddListener(OnWorldSelected);
        stageSelector.onSelected.AddListener(OnStageSelected);
    }

    private void OnWorldSelected(int worldIndex, SelectItemData worldData)
    {
        ShowStageSelect();

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

    private void OnStageSelected(int stageIndex, SelectItemData stageData)
    {
        if (string.IsNullOrEmpty(stageData.targetSceneName))
        {
            Debug.LogWarning("Target scene name is empty.");
            return;
        }

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
}