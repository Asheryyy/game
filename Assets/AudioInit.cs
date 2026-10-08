using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Gắn vào 1 GameObject bất kỳ trong SampleScene.
/// Áp volume đã lưu (PlayerPrefs) vào AudioMixer khi load scene.
/// </summary>
public class AudioInit : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer; // MainMixer

    // Mixer chỉ nhận giá trị lúc Start, không nhận ở Awake
    void Start()
    {
        AudioListener.volume = 1f; // reset giá trị cũ, volume giờ do Mixer quản lý
        SetMixerVolume(mixer, "MasterVol", PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMixerVolume(mixer, "MusicVol",  PlayerPrefs.GetFloat("MusicVolume",  0.8f));
        SetMixerVolume(mixer, "SFXVol",    PlayerPrefs.GetFloat("SFXVolume",    1f));
    }

    /// Slider 0–1 → decibel (-80 dB = tắt, 0 dB = gốc)
    public static void SetMixerVolume(AudioMixer mixer, string param, float value)
    {
        if (mixer == null) return;
        float db = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        mixer.SetFloat(param, db);
    }
}
