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
    private List<AudioSource> activeLoopingSources = new List<AudioSource>();

    private float masterVol = 1f;
    private float bgmVol = 0.5f;
    private float sfxVol = 0.5f;

    // BGM 개별 볼륨 기억용 변수
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

        // 게임 시작 시 저장된 볼륨 불러오기
        masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVol = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
    }

    private void Start()
    {
        UpdateAllVolumes();
        PlayBGM(201, GetBGMVolume());
    }

    // --- 1. 단발성 효과음 (SFX) ---
    public void PlaySFX(int id)
    {
        PlaySFX(id, 1.0f);
    }

    public void PlaySFX(int id, float volumeScale)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            // UpdateAllVolumes에서 이미 sfxSource.volume이 0으로 처리되었다면, 
            // PlayOneShot도 완벽하게 무음으로 재생됩니다.
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    // --- 2. 반복 효과음 (Loop SFX) ---
    public AudioSource PlayLoopSFX(int id)
    {
        return PlayLoopSFX(id, 1.0f);
    }

    public AudioSource PlayLoopSFX(int id, float volumeScale)
    {
        if (!soundDict.TryGetValue(id, out AudioClip clip)) return null;

        GameObject soundObj = new GameObject($"LoopSFX_{id}");
        soundObj.transform.SetParent(this.transform);

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;

        // ✨ [수정] 생성 시점에도 미세 소음 차단 로직 적용
        if (masterVol <= 0.001f || sfxVol <= 0.001f)
        {
            source.volume = 0f;
        }
        else
        {
            source.volume = sfxVol * masterVol * volumeScale;
        }

        source.Play();
        activeLoopingSources.Add(source);

        return source;
    }

    public void StopLoopSFX(AudioSource source)
    {
        if (source != null)
        {
            if (activeLoopingSources.Contains(source))
            {
                activeLoopingSources.Remove(source);
            }
            source.Stop();
            Destroy(source.gameObject);
        }
    }

    public void PauseAllLoopSFX(bool isPaused)
    {
        foreach (var source in activeLoopingSources)
        {
            if (source == null) continue;

            if (isPaused) source.Pause();
            else source.UnPause();
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

    public void StopLoopSFX(int soundID)
    {
        if (!soundDict.ContainsKey(soundID)) return;
        AudioClip targetClip = soundDict[soundID];

        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeLoopingSources[i];
            if (source != null && source.clip == targetClip)
            {
                source.Stop();
                source.clip = null;
                activeLoopingSources.RemoveAt(i);
                Destroy(source.gameObject);
            }
        }
    }

    // --- 3. BGM ---
    public void PlayBGM(int id)
    {
        PlayBGM(id, 1.0f);
    }

    public void PlayBGM(int id, float volumeScale)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;

            currentBgmScale = volumeScale;

            // ✨ [수정] BGM 재생 시작 시점에도 차단 로직 적용
            if (masterVol <= 0.001f || bgmVol <= 0.001f)
            {
                bgmSource.volume = 0f;
            }
            else
            {
                bgmSource.volume = bgmVol * masterVol * currentBgmScale;
            }

            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying) bgmSource.UnPause();
    }

    // --- 볼륨 조절 (핵심 수정 구역) ---
    private void UpdateAllVolumes()
    {
        // ✨ [핵심 수정] 0.001f 이하라면 소수점 잔여값을 무시하고 완벽하게 0으로 만들어버립니다!

        // 1. BGM
        if (masterVol <= 0.001f || bgmVol <= 0.001f)
        {
            bgmSource.volume = 0f;
        }
        else
        {
            bgmSource.volume = bgmVol * masterVol * currentBgmScale;
        }

        // 2. SFX (OneShot용 소스)
        if (masterVol <= 0.001f || sfxVol <= 0.001f)
        {
            sfxSource.volume = 0f;
        }
        else
        {
            sfxSource.volume = sfxVol * masterVol;
        }

        // 3. Loop 소스들
        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            if (activeLoopingSources[i] == null)
            {
                activeLoopingSources.RemoveAt(i);
                continue;
            }

            if (masterVol <= 0.001f || sfxVol <= 0.001f)
            {
                activeLoopingSources[i].volume = 0f;
            }
            else
            {
                activeLoopingSources[i].volume = sfxVol * masterVol;
            }
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVol = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", masterVol);
        PlayerPrefs.Save();
        UpdateAllVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVol = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVol);
        PlayerPrefs.Save();
        UpdateAllVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVol = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVol);
        PlayerPrefs.Save();
        UpdateAllVolumes();
    }

    public float GetMasterVolume() => PlayerPrefs.GetFloat("MasterVolume", 1f);
    public float GetBGMVolume() => PlayerPrefs.GetFloat("BGMVolume", 0.5f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat("SFXVolume", 0.5f);
}