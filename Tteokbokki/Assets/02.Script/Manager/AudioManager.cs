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

    private float masterVol = 1f;
    private float bgmVol = 0.5f;
    private float sfxVol = 0.5f;

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
        PlayBGM(201);
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
        masterVol = Mathf.Clamp01(volume);
        UpdateAllVolumes(); // 마스터가 바뀌면 전체를 다시 계산
    }

    public void SetBGMVolume(float volume)
    {
        bgmVol = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVol * masterVol; // BGM 개별값 * 마스터값
    }

    public void SetSFXVolume(float volume)
    {
        sfxVol = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVol * masterVol; // SFX 개별값 * 마스터값
    }

    // 마스터 볼륨 변경 시 전체 소스에 일괄 적용하는 함수
    private void UpdateAllVolumes()
    {
        bgmSource.volume = bgmVol * masterVol;
        sfxSource.volume = sfxVol * masterVol;
    }
}