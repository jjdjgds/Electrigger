using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private RectTransform titleLogo;
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

    [Header("Option Screen")]
    [SerializeField] private GameObject mainCanvas;
    [SerializeField] private GameObject optionCanvas;
    [SerializeField] private Button backButton;

    [Header("Audio Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Fade Overlay")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Logo Animation")]
    [SerializeField] private float logoBobSpeed = 1.5f;
    [SerializeField] private float logoBobAmount = 15f;
    [SerializeField] private float logoFadeInDuration = 1.2f;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private float transitionDuration = 0.8f;

    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.2f);

    private Vector2 logoStartAnchoredPos;
    private bool isTransitioning = false;
    private bool isOptionOpen = false;

    private void Awake()
    {
        if (canvasScaler == null)
            canvasScaler = GetComponent<CanvasScaler>();

        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
            fadeOverlay.raycastTarget = true;
        }
    }

    private void Start()
    {
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        if (titleLogo != null)
            logoStartAnchoredPos = titleLogo.anchoredPosition;

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (optionButton != null)
        {
            optionButton.onClick.RemoveAllListeners();
            optionButton.onClick.AddListener(OnOptionClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        SetupAudioSettings();
        ShowMain();
        StartCoroutine(FadeFromBlack());
    }

    private void Update()
    {
        if (isTransitioning)
            return;

        if (!isOptionOpen && titleLogo != null)
        {
            float offsetY = Mathf.Sin(Time.time * logoBobSpeed) * logoBobAmount;
            titleLogo.anchoredPosition = logoStartAnchoredPos + new Vector2(0f, offsetY);
        }

        if (!isOptionOpen && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            OnStartClicked();

        if (isOptionOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            OnBackClicked();
    }

    private void ShowMain()
    {
        isOptionOpen = false;

        if (mainCanvas != null)
            mainCanvas.SetActive(true);

        if (optionCanvas != null)
            optionCanvas.SetActive(false);
    }

    private void ShowOption()
    {
        isOptionOpen = true;

        if (mainCanvas != null)
            mainCanvas.SetActive(false);

        if (optionCanvas != null)
            optionCanvas.SetActive(true);
    }

    private void SetupAudioSettings()
    {
        float bgmValue = AudioManager.Instance != null ? AudioManager.Instance.GetBgmVolume() : 1f;
        float sfxValue = AudioManager.Instance != null ? AudioManager.Instance.GetSfxVolume() : 1f;

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.wholeNumbers = false;
            bgmSlider.SetValueWithoutNotify(bgmValue);
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.wholeNumbers = false;
            sfxSlider.SetValueWithoutNotify(sfxValue);
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }
    }

    private void OnBgmSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBgmVolume(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);
    }

    private void OnStartClicked()
    {
        if (isTransitioning || isOptionOpen)
            return;

        StartCoroutine(FadeToBlackAndLoad(gameSceneName));
    }

    private void OnOptionClicked()
    {
        if (isTransitioning)
            return;

        ShowOption();
    }

    private void OnBackClicked()
    {
        if (isTransitioning)
            return;

        ShowMain();
    }

    private void OnQuitClicked()
    {
        if (isTransitioning)
            return;

        StartCoroutine(FadeToBlackAndQuit());
    }

    private IEnumerator FadeFromBlack()
    {
        if (fadeOverlay == null)
            yield break;

        float elapsed = 0f;
        Color color = fadeOverlay.color;

        while (elapsed < logoFadeInDuration)
        {
            float alpha = 1f - (elapsed / logoFadeInDuration);
            fadeOverlay.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeOverlay.color = new Color(color.r, color.g, color.b, 0f);
        fadeOverlay.raycastTarget = false;
    }

    private IEnumerator FadeToBlackAndLoad(string sceneName)
    {
        isTransitioning = true;

        if (fadeOverlay == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        Color color = fadeOverlay.color;

        while (elapsed < transitionDuration)
        {
            float alpha = elapsed / transitionDuration;
            fadeOverlay.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeOverlay.color = new Color(color.r, color.g, color.b, 1f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeToBlackAndQuit()
    {
        isTransitioning = true;

        if (fadeOverlay == null)
        {
            Application.Quit();
            yield break;
        }

        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        Color color = fadeOverlay.color;

        while (elapsed < transitionDuration)
        {
            float alpha = elapsed / transitionDuration;
            fadeOverlay.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeOverlay.color = new Color(color.r, color.g, color.b, 1f);
        Application.Quit();
    }
}