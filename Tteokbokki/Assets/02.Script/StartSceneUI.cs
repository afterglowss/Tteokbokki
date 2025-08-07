using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class GameLoadFlags
{
    public static bool shouldLoadFromSave = false;
}
public class StartSceneUI : MonoBehaviour
{
    [Header("메인 버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button exitButton;

    [Header("설정창 UI")]
    [SerializeField] private PauseMenuUI pauseMenuUI;

    public TextMeshProUGUI continueDateText;

    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
    }

    void Start()
    {
        // 버튼 클릭 이벤트 등록
        startButton.onClick.AddListener(OnStartButtonClicked);
        if (true) 
        { 
            // (나중에) 시작버튼 누른거 체크되면 활성화
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
        UpdateContinueDateLabel();
    }
    private void UpdateContinueDateLabel()
    {
        DateTime? lastDate = GameClock.LoadLastPlayedDate();

        if (File.Exists(saveFilePath))
        {
            // SaveData.json이 있으면 저장된 게임 시간 불러오기
            GameSaveData data = LoadSaveMetaOnly();
            if (DateTime.TryParse(data.gameTime, out DateTime gameTime))
            {
                continueDateText.text = $"{gameTime.Month}월 {gameTime.Day}일 이어하기";
            }
            continueButton.interactable = true;
        }
        else if (lastDate.HasValue)
        {
            // SaveData.json이 없고 날짜 기록만 있을 경우
            continueDateText.text = $"{lastDate.Value.Month}월 {lastDate.Value.Day}일 이어하기";
            continueButton.interactable = true;
        }
        else
        {
            // 이어하기 불가능
            continueDateText.text = "이어하기 (없음)";
            continueButton.interactable = false;
        }
    }

    private GameSaveData LoadSaveMetaOnly()
    {
        string json = File.ReadAllText(saveFilePath);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    private void OnStartButtonClicked()
    {
        // 게임 씬으로 이동
        SceneManager.LoadScene("SampleScene");
        //SceneManager.LoadScene("SampleScene 1");
    }

    private void OnContinueButtonClicked()
    {
        GameLoadFlags.shouldLoadFromSave = true;
        SceneManager.LoadScene("SampleScene");
    }

    private void OnSettingButtonClicked()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.TogglePausePanel();
        }
        else
        {
            Debug.LogWarning("PauseMenuUI가 할당되지 않았습니다.");
        }
    }


    private void OnExitButtonClicked()
    {
        // 게임 종료
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // 빌드된 게임에서 실행 중이면 애플리케이션 종료
        Application.Quit();
    #endif
    }
}