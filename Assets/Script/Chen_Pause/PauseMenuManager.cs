using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ポーズメニュー管理
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    // UI参照
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    // シーン名
    [Header("Scene")]
    [SerializeField] private string worldSelectSceneName = "WorldSelectScene";

    // 設定
    [Header("Animation")]// アニメーション設定
    [SerializeField] private float animationDuration = 0.25f;

    [Header("Input")]// 入力設定
    [SerializeField] private float toggleCooldown = 0.25f;

    // 状態
    private bool isPaused = false;
    private bool isAnimating = false;
    private float lastToggleTime = -999f;

    private RectTransform panelRect;
    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private Coroutine animCoroutine;

    // ポーズ状態を外部から参照可能にするプロパティ
    public static bool IsPaused { get; private set; }

    // ゲーム内で入力を受け付けるべきか
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

    // ポーズ切り替え
    private void TryTogglePause()
    {
        // アニメーション中は切り替え不可
        if (isAnimating) return;

        // クールダウン中は切り替え不可
        if (Time.unscaledTime - lastToggleTime < toggleCooldown)
            return;

        lastToggleTime = Time.unscaledTime;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused || isAnimating) return;

        isPaused = true;
        IsPaused = true;

        pausePanel.SetActive(true);
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