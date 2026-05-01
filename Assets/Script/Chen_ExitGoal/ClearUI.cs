using UnityEngine;
using TMPro;

/// <summary>
/// クリアUIクラス
/// </summary>
public class LevelClearUI : MonoBehaviour
{
    // シングルトンインスタンス
    public static LevelClearUI Instance { get; private set; }

    // UI歌孚
    [Header("UI")]
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private TMP_Text clearText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
    }

    // クリアUIを燕幣するメソッド
    public void ShowClearUI()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(true);
        }

        if (clearText != null)
        {
            clearText.text = "GooooooD!!";
        }
    }
}