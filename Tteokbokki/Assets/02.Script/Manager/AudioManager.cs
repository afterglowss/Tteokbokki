using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource; // 단발성 효과음용 (OneShot)

    [Header("Audio Clips (BGM+SFX)")]
    [SerializeField] private List<SoundData> soundClips;

    private Dictionary<int, AudioClip> soundDict = new Dictionary<int, AudioClip>();

    // 반복 재생 중인 소스들을 추적 관리하기 위한 리스트
    private List<AudioSource> activeLoopingSources = new List<AudioSource>();

    private float masterVol = 1f;
    private float bgmVol = 0.5f;
    private float sfxVol = 0.5f;

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
    }

    private void Start()
    {
        // 게임 시작 시 초기화 (필요하다면 저장된 볼륨 불러오기 등)
        UpdateAllVolumes();
        // 예시 BGM 재생 (필요시 주석 해제)
        PlayBGM(201); 
    }

    // --- 1. 단발성 효과음 (UI, 성공, 실패 등) ---
    public void PlaySFX(int id)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            // PlayOneShot은 하나의 소스에서 여러 소리를 겹쳐 재생 가능
            sfxSource.PlayOneShot(clip);
        }
    }

    // --- 2. 반복 효과음 (조리 중 끓는 소리 등) ---
    // 리턴값으로 AudioSource를 줍니다. 이걸 받은 쪽에서 Stop 시켜야 합니다.
    public AudioSource PlayLoopSFX(int id)
    {
        if (!soundDict.TryGetValue(id, out AudioClip clip)) return null;

        // 동적으로 오브젝트 생성
        GameObject soundObj = new GameObject($"LoopSFX_{id}");
        soundObj.transform.SetParent(this.transform);

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f; // 2D 사운드
        source.volume = sfxVol * masterVol; // 현재 볼륨 적용
        source.Play();

        // 관리 리스트에 추가
        activeLoopingSources.Add(source);

        return source;
    }

    // 반복 효과음 정지 및 제거
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

    // --- BGM ---
    public void PlayBGM(int id)
    {
        if (soundDict.TryGetValue(id, out AudioClip clip))
        {
            if (bgmSource.clip == clip) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    // --- 볼륨 조절 ---
    public void SetMasterVolume(float volume)
    {
        masterVol = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVol = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVol = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    private void UpdateAllVolumes()
    {
        // 1. 기본 소스들 조절
        bgmSource.volume = bgmVol * masterVol;
        sfxSource.volume = sfxVol * masterVol;

        // 2. 현재 재생 중인 루프 소스들도 실시간 조절 (중요!)
        // 리스트를 역순으로 순회하거나, null 체크를 하며 조절
        for (int i = activeLoopingSources.Count - 1; i >= 0; i--)
        {
            if (activeLoopingSources[i] == null)
            {
                activeLoopingSources.RemoveAt(i);
                continue;
            }
            activeLoopingSources[i].volume = sfxVol * masterVol;
        }
    }
}