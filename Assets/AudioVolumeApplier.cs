using UnityEngine;

/// Gắn vào GameObject có AudioSource.
/// Chọn loại Music hoặc SFX, script sẽ tự nhân với volume trong Settings.
[RequireComponent(typeof(AudioSource))]
public class AudioVolumeApplier : MonoBehaviour
{
    public enum Category { Music, SFX }
    public Category category = Category.SFX;

    AudioSource src;
    float baseVolume; // volume gốc bạn đặt trong Inspector

    void Awake()
    {
        src = GetComponent<AudioSource>();
        baseVolume = src.volume;
        Apply();
    }

    public void Apply()
    {
        string key = category == Category.Music ? "MusicVolume" : "SFXVolume";
        float def  = category == Category.Music ? 0.8f : 1f;
        src.volume = baseVolume * PlayerPrefs.GetFloat(key, def);
    }
}
