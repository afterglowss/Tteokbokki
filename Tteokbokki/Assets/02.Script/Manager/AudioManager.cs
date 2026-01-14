using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips (BGM+SFX)")]
    [SerializeField] private List<SoundData> soundClips;

    private Dictionary<int, AudioClip> soundDict = new Dictionary<int, AudioClip>();
    private float masterVolume = 1f;

    [System.Serializable]
    public struct SoundData
    {
        public int id;          // 숫자 ID (예: 101, 201)
        public string description; // 사운드 설명
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 딕셔너리 생성
        foreach (var data in soundClips)
            soundDict[data.id] = data.clip;

    }
    private void Start()
    {
        //test
        PlayBGM(101);
    }

    // --- 재생 로직 ---
    public void PlaySFX(int id)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
            sfxSource.PlayOneShot(clip);
    }

    public void PlayBGM(int id)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            if (bgmSource.clip == clip) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    // --- 볼륨 조절 로직 ---

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        bgmSource.volume = masterVolume;
        sfxSource.volume = masterVolume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume) * masterVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume) * masterVolume;
    }
}