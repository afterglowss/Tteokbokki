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
    [Header("���� ��ư")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button exitButton;

    [Header("����â UI")]
    [SerializeField] private PauseMenuUI pauseMenuUI;

    public TextMeshProUGUI continueDateText;

    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
    }

    void Start()
    {
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
        UpdateContinueDateLabel();
    }
    private void UpdateContinueDateLabel()
    {
        DateTime? lastDate = GameClock.LoadLastPlayedDate();

        if (File.Exists(saveFilePath))
        {
            // SaveData.json�� ������ ����� ���� �ð� �ҷ�����
            GameSaveData data = LoadSaveMetaOnly();
            if (DateTime.TryParse(data.gameTime, out DateTime gameTime))
            {
                continueDateText.text = $"{gameTime.Month}월 {gameTime.Day}일부터";
            }
            continueButton.interactable = true;
        }
        else if (lastDate.HasValue)
        {
            // SaveData.json�� ���� ��¥ ��ϸ� ���� ���
            continueDateText.text = $"{lastDate.Value.Month}월 {lastDate.Value.Day}일 이어하기";
            continueButton.interactable = true;
        }
        else
        {
            // �̾��ϱ� �Ұ���
            continueDateText.text = "기록이 없습니다.";
            continueButton.interactable = false;
        }
    }

    public void MoveScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
    public void MoveScene(string scene, bool save)
    {
        GameLoadFlags.shouldLoadFromSave = true;
        SceneManager.LoadScene(scene);
    }

    private GameSaveData LoadSaveMetaOnly()
    {
        string json = File.ReadAllText(saveFilePath);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    private void OnStartButtonClicked()
    {
        SceneManager.LoadScene("SampleScene");
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
            Debug.LogWarning("PauseMenuUI가 없습니다.");
        }
    }


    private void OnExitButtonClicked()
    {
        // ���� ����
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // ����� ���ӿ��� ���� ���̸� ���ø����̼� ����
        Application.Quit();
    #endif
    }
}