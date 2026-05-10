using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SFXVolume";

    private const string BgmParameter = "BGMVolume";
    private const string SfxParameter = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSavedVolumes();
    }

    public void SetBgmVolume(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        SetMixerVolume(BgmParameter, normalizedValue);
        PlayerPrefs.SetFloat(BgmVolumeKey, normalizedValue);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        SetMixerVolume(SfxParameter, normalizedValue);
        PlayerPrefs.SetFloat(SfxVolumeKey, normalizedValue);
        PlayerPrefs.Save();
    }

    public float GetBgmVolume()
    {
        return PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
    }

    public float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    public void LoadSavedVolumes()
    {
        SetMixerVolume(BgmParameter, GetBgmVolume());
        SetMixerVolume(SfxParameter, GetSfxVolume());
    }

    private void SetMixerVolume(string parameterName, float normalizedValue)
    {
        if (audioMixer == null)
            return;

        if (normalizedValue <= 0.0001f)
        {
            audioMixer.SetFloat(parameterName, -80f);
            return;
        }

        float dB = Mathf.Log10(normalizedValue) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }
}