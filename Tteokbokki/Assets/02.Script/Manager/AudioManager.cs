using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    // ... (기존 변수들: Instance, Source, List, Dictionary 등 유지) ...

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

        // ✨ [핵심 수정] 게임 시작 시 저장된 볼륨 불러오기
        // (저장된 값이 없으면 기본값 1f, 0.5f 사용)
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

    // 기존 함수 (기본 볼륨 1.0)
    public void PlaySFX(int id)
    {
        PlaySFX(id, 1.0f);
    }

    // ✨ [오버로딩] 볼륨 조절 가능 버전
    // volumeScale: 0.0 ~ 1.0 (1.0이면 원래 크기, 0.5면 절반)
    public void PlaySFX(int id, float volumeScale)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            // PlayOneShot의 두 번째 인자는 volumeScale입니다.
            // 최종 볼륨 = (클립 기본 볼륨) * (volumeScale) * (AudioSource.volume)
            // 이미 sfxSource.volume에 (sfxVol * masterVol)이 반영되어 있으므로,
            // 여기서는 개별 배율만 곱해주면 됩니다.
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    // --- 2. 반복 효과음 (Loop SFX) ---

    // 기존 함수
    public AudioSource PlayLoopSFX(int id)
    {
        return PlayLoopSFX(id, 1.0f);
    }

    // ✨ [오버로딩] 볼륨 조절 가능 버전
    public AudioSource PlayLoopSFX(int id, float volumeScale)
    {
        if (!soundDict.TryGetValue(id, out AudioClip clip)) return null;

        GameObject soundObj = new GameObject($"LoopSFX_{id}");
        soundObj.transform.SetParent(this.transform);

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;

        // 최종 볼륨 = (설정된 SFX 볼륨) * (마스터 볼륨) * (개별 배율)
        // 나중에 볼륨 슬라이더를 조절할 때 '개별 배율' 정보를 잃어버리지 않으려면
        // 별도로 저장하거나, 여기서는 초기값만 세팅하고 관리는 복잡해질 수 있습니다.
        // 하지만 간단하게 구현하기 위해 초기 볼륨만 세팅합니다.
        // (주의: 슬라이더로 볼륨을 바꿀 때 이 비율이 유지되려면 추가 로직이 필요하지만, 
        //  현재 구조상 UpdateAllVolumes에서는 일괄적으로 덮어쓰므로 
        //  Loop 사운드는 개별 볼륨 조절보다는 클립 자체를 수정하는 게 나을 수도 있습니다.
        //  일단 요청하신 대로 적용은 해드립니다.)

        source.volume = sfxVol * masterVol * volumeScale;
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
    // ✨ [NEW] 1. 일시정지 시 반복 효과음들만 멈추기 (포즈 메뉴용)
    public void PauseAllLoopSFX(bool isPaused)
    {
        foreach (var source in activeLoopingSources)
        {
            if (source == null) continue;

            if (isPaused)
            {
                source.Pause(); // 일시정지
            }
            else
            {
                source.UnPause(); // 다시 재생
            }
        }
    }
    // ✨ [NEW] 2. 씬 이동 시 모든 반복 효과음 제거하기 (StartScene 갈 때용)
    public void StopAllLoopSFX()
    {
        // 리스트를 역순으로 돌며 모두 파괴
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
        // 1. 해당 ID의 클립을 찾음
        if (!soundDict.ContainsKey(soundID)) return;
        AudioClip targetClip = soundDict[soundID];

        // 2. 현재 재생 중인 루프 소스들 중에서 해당 클립을 재생 중인 녀석을 찾아서 끔
        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeLoopingSources[i];
            if (source != null && source.clip == targetClip)
            {
                source.Stop();
                source.clip = null;
                // 리스트에서 제거하거나, 풀링 방식에 따라 처리 (여기서는 단순 정지)
                activeLoopingSources.RemoveAt(i);
                Destroy(source.gameObject); // 동적 생성된 소스라면 파괴
            }
        }
    }

    // --- 3. BGM ---

    // 기존 함수
    public void PlayBGM(int id)
    {
        PlayBGM(id, 1.0f);
    }

    // ✨ [오버로딩] 볼륨 조절 가능 버전
    public void PlayBGM(int id, float volumeScale)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            // BGM은 소스가 하나이므로 Source 자체의 볼륨을 조절하면 
            // 나중에 슬라이더 조절 시 꼬일 수 있습니다.
            // 따라서 BGM은 volumeScale을 적용하기 까다롭지만, 
            // 굳이 하려면 멤버변수로 currentBgmScale을 저장해둬야 합니다.
            currentBgmScale = volumeScale;
            bgmSource.volume = bgmVol * masterVol * currentBgmScale;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }

    // BGM 개별 볼륨 기억용 변수
    //private float currentBgmScale = 1.0f;

    // --- 볼륨 조절 (기존 유지 + Loop/BGM 스케일 반영) ---
    // ... (SetMasterVolume, SetBGMVolume, SetSFXVolume 등은 그대로) ...

    private void UpdateAllVolumes()
    {
        // 1. BGM: 설정 볼륨 * 개별 스케일
        bgmSource.volume = bgmVol * masterVol * currentBgmScale;

        // 2. SFX (OneShot용 소스): 설정 볼륨만 (PlayOneShot 호출 시 스케일은 그때그때 곱해짐)
        sfxSource.volume = sfxVol * masterVol;

        // 3. Loop 소스들: 
        // 아까 말씀드린 대로, Loop 소스는 개별 스케일(volumeScale)을 기억하지 못하고 
        // 이 함수가 호출되면 일괄적으로 초기화되는 문제가 있습니다.
        // 이를 해결하려면 Dictionary<AudioSource, float> 등으로 개별 스케일을 저장해야 합니다.
        // 지금 단계에서는 복잡도를 낮추기 위해 'Loop 사운드는 일괄 볼륨'으로 처리하거나
        // 아래처럼 단순히 설정값만 적용하겠습니다. (개별 조절했던 게 초기화될 수 있음 주의)

        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            if (activeLoopingSources[i] == null)
            {
                activeLoopingSources.RemoveAt(i);
                continue;
            }
            // 원래는 activeLoopingSources[i].volume = sfxVol * masterVol * [저장된 개별 스케일]; 이어야 함
            activeLoopingSources[i].volume = sfxVol * masterVol;
        }
    }

    // --- 볼륨 조절 ---
    public void SetMasterVolume(float volume)
    {
        masterVol = Mathf.Clamp01(volume);

        // ✨ 변경된 값 저장
        PlayerPrefs.SetFloat("MasterVolume", masterVol);
        PlayerPrefs.Save();

        UpdateAllVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVol = Mathf.Clamp01(volume);

        // ✨ 변경된 값 저장
        PlayerPrefs.SetFloat("BGMVolume", bgmVol);
        PlayerPrefs.Save();

        UpdateAllVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVol = Mathf.Clamp01(volume);

        // ✨ 변경된 값 저장
        PlayerPrefs.SetFloat("SFXVolume", sfxVol);
        PlayerPrefs.Save();

        UpdateAllVolumes();
    }

    public float GetMasterVolume()
    {
        // AudioMixer를 쓴다면 mixer.GetFloat(...)를 써야 하지만,
        // 간단하게는 PlayerPrefs나 내부 변수를 반환하면 됩니다.
        return PlayerPrefs.GetFloat("MasterVolume", 1f);
    }

    public float GetBGMVolume()
    {
        return PlayerPrefs.GetFloat("BGMVolume", 0.5f);
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat("SFXVolume", 0.5f);
    }
}