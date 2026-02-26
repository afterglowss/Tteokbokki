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
        // 1. 저장된 값을 변수에 로드 (이미 Awake에서 했겠지만 한 번 더 확인)
        masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVol = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        // 2. 배경음악 재생 전에 '믹서' 파라미터를 먼저 강제 세팅
        UpdateAllVolumes();

        // 3. 믹서가 세팅된 "후"에 배경음악 재생
        PlayBGM(201, GetBGMVolume());
    }

    private void UpdateAllVolumes()
    {
        // 확실하게 로그 스케일로 밀어 넣으세요.
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MasterVolume", masterVol > 0.0001f ? Mathf.Log10(masterVol) * 20 : -80f);
            mainMixer.SetFloat("BGMVolume", bgmVol > 0.0001f ? Mathf.Log10(bgmVol) * 20 : -80f);
            mainMixer.SetFloat("SFXVolume", sfxVol > 0.0001f ? Mathf.Log10(sfxVol) * 20 : -80f);
        }

        // 소스 볼륨도 믹서와 별개로 0이면 0으로 밀어버립니다 (2중 잠금)
        bgmSource.volume = (bgmVol <= 0.001f) ? 0 : currentBgmScale;
        sfxSource.volume = (sfxVol <= 0.001f) ? 0 : 1f;
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

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }
    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
            bgmSource.UnPause();
    }

    // --- 볼륨 조절 ---
    public void SetMasterVolume(float volume)
    { 
        masterVol = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
        UpdateAllVolumes();
    }
    public void SetBGMVolume(float volume)
    {
        bgmVol = volume;
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();
        UpdateAllVolumes();
    }
    public void SetSFXVolume(float volume)
    {
        sfxVol = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
        UpdateAllVolumes();
    }

    public float GetMasterVolume() => masterVol;
    public float GetBGMVolume() => bgmVol;
    public float GetSFXVolume() => sfxVol;
}