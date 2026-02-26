using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private List<SoundData> soundClips;

    private Dictionary<int, AudioClip> soundDict = new Dictionary<int, AudioClip>();
    private List<AudioSource> activeLoopingSources = new List<AudioSource>();

    private float masterVol = 1f;
    private float bgmVol = 0.5f;
    private float sfxVol = 0.5f;
    private float currentBgmScale = 1.0f;

    [System.Serializable]
    public struct SoundData
    {
        public int id;
        public string description;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var data in soundClips)
            soundDict[data.id] = data.clip;

        masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVol = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
    }

    private void Start()
    {
        UpdateAllVolumes();
        PlayBGM(201, 1.0f);
    }

    private void SetMixerValue(string parameterName, float sliderValue)
    {
        if (mainMixer == null) return;
        float dB = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20 : -80f;
        mainMixer.SetFloat(parameterName, dB);
    }

    private void UpdateAllVolumes()
    {
        SetMixerValue("MasterVolume", masterVol);
        SetMixerValue("BGMVolume", bgmVol);
        SetMixerValue("SFXVolume", sfxVol);

        bgmSource.volume = currentBgmScale;
        sfxSource.volume = 1f;

        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            if (activeLoopingSources[i] == null) { activeLoopingSources.RemoveAt(i); continue; }
            activeLoopingSources[i].volume = 1f;
        }
    }

    // --- 재생 함수 ---
    public void PlaySFX(int id, float volumeScale = 1.0f)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
            sfxSource.PlayOneShot(clip, volumeScale);
    }

    public AudioSource PlayLoopSFX(int id, float volumeScale = 1.0f)
    {
        if (!soundDict.TryGetValue(id, out AudioClip clip)) return null;

        GameObject soundObj = new GameObject($"LoopSFX_{id}");
        soundObj.transform.SetParent(this.transform);

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        source.volume = volumeScale;
        source.Play();

        activeLoopingSources.Add(source);
        return source;
    }

    // --- 제어 함수 (중복 해결) ---

    // 1. AudioSource 객체로 멈추기
    public void StopLoopSFX(AudioSource source)
    {
        if (source != null && activeLoopingSources.Contains(source))
        {
            activeLoopingSources.Remove(source);
            source.Stop();
            Destroy(source.gameObject);
        }
    }

    // 2. 사운드 ID(int)로 멈추기 (오버로딩)
    public void StopLoopSFX(int soundID)
    {
        if (!soundDict.ContainsKey(soundID)) return;
        AudioClip targetClip = soundDict[soundID];
        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            if (activeLoopingSources[i] != null && activeLoopingSources[i].clip == targetClip)
            {
                AudioSource src = activeLoopingSources[i];
                activeLoopingSources.RemoveAt(i);
                src.Stop();
                Destroy(src.gameObject);
            }
        }
    }

    public void PauseAllLoopSFX(bool isPaused)
    {
        foreach (var src in activeLoopingSources)
        {
            if (src != null) { if (isPaused) src.Pause(); else src.UnPause(); }
        }
    }

    public void StopAllLoopSFX()
    {
        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            if (activeLoopingSources[i] != null)
            {
                activeLoopingSources[i].Stop();
                Destroy(activeLoopingSources[i].gameObject);
            }
        }
        activeLoopingSources.Clear();
    }

    public void PlayBGM(int id, float volumeScale = 1.0f)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            currentBgmScale = volumeScale;
            bgmSource.volume = currentBgmScale;
            bgmSource.Play();
        }
    }

    public void StopBGM() { if (bgmSource != null) bgmSource.Stop(); }
    public void ResumeBGM() { if (bgmSource != null && !bgmSource.isPlaying) bgmSource.UnPause(); }

    // --- 볼륨 조절 ---
    public void SetMasterVolume(float volume) { masterVol = volume; PlayerPrefs.SetFloat("MasterVolume", volume); UpdateAllVolumes(); }
    public void SetBGMVolume(float volume) { bgmVol = volume; PlayerPrefs.SetFloat("BGMVolume", volume); UpdateAllVolumes(); }
    public void SetSFXVolume(float volume) { sfxVol = volume; PlayerPrefs.SetFloat("SFXVolume", volume); UpdateAllVolumes(); }

    public float GetMasterVolume() => masterVol;
    public float GetBGMVolume() => bgmVol;
    public float GetSFXVolume() => sfxVol;
}