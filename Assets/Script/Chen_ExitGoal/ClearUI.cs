using UnityEngine;
using TMPro;

/// <summary>
/// ･ｯ･・｢UI･ｯ･鬣ｹ
/// </summary>
public class LevelClearUI : MonoBehaviour
{
    // ･ｷ･ｰ･・ﾈ･､･ｹ･ｿ･ｹ
    public static LevelClearUI Instance { get; private set; }

    // UIｲﾎﾕﾕ
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

    // ･ｯ･・｢UI､桄ｾ､ｹ､・皈ｽ･ﾃ･ﾉ
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