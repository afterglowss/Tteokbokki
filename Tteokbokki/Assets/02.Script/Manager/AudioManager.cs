using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private List<SoundData> sfxClips;  //sfx 목록
    [SerializeField] private List<SoundData> bgmClips;  //bgm 목록

    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();

    // 전체 볼륨 관리
    private float masterVolume = 1f;

    [System.Serializable]
    public struct SoundData
    {
        public string name;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var data in sfxClips) sfxDict[data.name] = data.clip;
        foreach (var data in bgmClips) bgmDict[data.name] = data.clip;
    }

    // --- 재생 로직 ---
    public void PlayBGM(string name)
    {
        if (bgmDict.TryGetValue(name, out AudioClip clip))
        {
            if (bgmSource.clip == clip) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    public void PlaySFX(string name)
    {
        if (sfxDict.TryGetValue(name, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // --- 볼륨 조절 로직 ---

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        bgmSource.volume = masterVolume;
        sfxSource.volume = masterVolume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume * masterVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume * masterVolume;
    }
}