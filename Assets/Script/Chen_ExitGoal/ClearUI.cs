using UnityEngine;
using TMPro;

public class LevelClearUI : MonoBehaviour
{
    public static LevelClearUI Instance { get; private set; }

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