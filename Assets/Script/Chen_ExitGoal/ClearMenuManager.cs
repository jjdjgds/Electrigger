using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// クリアメニュー管理
/// </summary>
public class ClearMenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject clearPanel;

    [Header("Scene")]
    [SerializeField] private string worldSelectSceneName = "WorldSelectScene";

    private void Awake()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
    }

    // クリアメニューを表示
    public void ShowClearMenu()
    {
        if (clearPanel == null) return;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearStage(
                LevelSelectReturnData.currentWorldId,
                LevelSelectReturnData.currentStageId);
        }
        else
        {
            Debug.LogWarning("SaveManager not found.");
        }

        clearPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    // ステージセレクトに戻る
    public void BackToStageSelect()
    {
        Time.timeScale = 1f;

        LevelSelectReturnData.RequestReturnToStageSelect();
        SceneManager.LoadScene(worldSelectSceneName);
    }
}