using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    [Header("UI References")]
    public CanvasScaler canvasScaler;
    public RectTransform titleLogo;
    public Button startButton;
    public Button quitButton;

    [Header("Logo Animation")]
    public float logoBobSpeed = 1.5f;
    public float logoBobAmount = 15f;
    public float logoFadeInDuration = 1.2f;

    [Header("Scene")]
    public string gameSceneName = "GameScene";
    public float transitionDuration = 0.8f;

    [Header("Background")]
    public Image backgroundImage;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.2f);

    private CanvasGroup canvasGroup;
    private Vector2 logoStartAnchoredPos;
    private bool isTransitioning = false;

    void Awake()
    {
        // Force resolution independence
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // balance between width and height
        }
    }

    void Start()
    {
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (titleLogo != null)
            logoStartAnchoredPos = titleLogo.anchoredPosition;

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        StartCoroutine(FadeIn());
    }

    void Update()
    {
        if (isTransitioning) return;

        // Bob the logo up and down
        if (titleLogo != null)
        {
            float offsetY = Mathf.Sin(Time.time * logoBobSpeed) * logoBobAmount;
            titleLogo.anchoredPosition = logoStartAnchoredPos + new Vector2(0f, offsetY);
        }

        // Keyboard shortcut
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            OnStartClicked();
    }

    void OnStartClicked()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOutAndLoad(gameSceneName));
    }

    void OnQuitClicked()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOutAndQuit());
    }

    IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < logoFadeInDuration)
        {
            canvasGroup.alpha = Mathf.Clamp01(elapsed / logoFadeInDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        isTransitioning = true;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / transitionDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeOutAndQuit()
    {
        isTransitioning = true;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / transitionDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Application.Quit();
    }
}