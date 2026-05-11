using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ･ﾝｩ`･ｺ･皈ﾋ･蟀`ｹﾜﾀ・
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    // UIｲﾎﾕﾕ
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    // ･ｷｩ`･・
    [Header("Scene")]
    [SerializeField] private string worldSelectSceneName = "WorldSelectScene";

    // ﾔOｶｨ
    [Header("Animation")]// ･｢･ﾋ･皓`･ｷ･逾Oｶｨ
    [SerializeField] private float animationDuration = 0.25f;

    [Header("Input")]// ﾈ・ｦﾔOｶｨ
    [SerializeField] private float toggleCooldown = 0.25f;

    // ﾗｴ腺
    private bool isPaused = false;
    private bool isAnimating = false;
    private float lastToggleTime = -999f;

    private RectTransform panelRect;
    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private Coroutine animCoroutine;

    // ･ﾝｩ`･ｺﾗｴ腺､箚ｿ､ｫ､魎ﾎﾕﾕｿﾉﾄﾜ､ﾋ､ｹ､・ﾗ･愠ﾑ･ﾆ･?
    public static bool IsPaused { get; private set; }

    // ･ｲｩ`･狷ﾚ､ﾇﾈ・ｦ､ﾜ､ｱｸｶ､ｱ､・ﾙ､ｭ､?
    public static bool CanGameInput()
    {
        return !IsPaused;
    }

    private void Awake()
    {
        IsPaused = false;

        panelRect = pausePanel.GetComponent<RectTransform>();

        shownPos = panelRect.anchoredPosition;
        hiddenPos = shownPos + new Vector2(0f, Screen.height);

        panelRect.anchoredPosition = hiddenPos;
        pausePanel.SetActive(false);
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            TryTogglePause();
        }
    }

    // ･ﾝｩ`･ｺﾇﾐ､・讀?
    private void TryTogglePause()
    {
        // ･｢･ﾋ･皓`･ｷ･逾ﾐ､ﾏﾇﾐ､・讀ｨｲｻｿ?
        if (isAnimating) return;

        // ･ｯｩ`･・ﾀ･ｦ･ﾐ､ﾏﾇﾐ､・讀ｨｲｻｿ?
        if (Time.unscaledTime - lastToggleTime < toggleCooldown)
            return;

        lastToggleTime = Time.unscaledTime;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    // UI､ﾎ･ﾜ･ｿ･ｫ､鮗ﾓｳｹﾓﾃ､ﾎ･皈ｽ･ﾃ･ﾉ
    public void OnPauseButtonClicked()
    {
        TryTogglePause();
    }

    public void PauseGame()
    {
        if (isPaused || isAnimating) return;

        isPaused = true;
        IsPaused = true;

        pausePanel.SetActive(true);
        Cursor.visible = false;
        Time.timeScale = 0f;

        StartSlide(hiddenPos, shownPos, false);
    }

    public void ResumeGame()
    {
        if (!isPaused || isAnimating) return;

        isPaused = false;
        IsPaused = false;

        StartSlide(shownPos, hiddenPos, true);
    }

    private void StartSlide(Vector2 from, Vector2 to, bool hideAfter)
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(SlidePanel(from, to, hideAfter));
    }

    private IEnumerator SlidePanel(Vector2 from, Vector2 to, bool hideAfter)
    {
        isAnimating = true;

        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = time / animationDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            panelRect.anchoredPosition = Vector2.Lerp(from, to, t);

            yield return null;
        }

        panelRect.anchoredPosition = to;

        if (hideAfter)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        isAnimating = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void BackToStageSelect()
    {
        Time.timeScale = 1f;

        LevelSelectReturnData.RequestReturnToStageSelect();
        SceneManager.LoadScene(worldSelectSceneName);
    }

    public void SaveGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
            Debug.Log("Game Saved.");
        }
        else
        {
            Debug.LogWarning("SaveManager not found.");
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}