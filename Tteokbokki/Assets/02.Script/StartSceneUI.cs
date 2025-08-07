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
        // ��ư Ŭ�� �̺�Ʈ ���
        startButton.onClick.AddListener(OnStartButtonClicked);
        if (true) 
        { 
            // (���߿�) ���۹�ư ������ üũ�Ǹ� Ȱ��ȭ
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
            // SaveData.json�� ������ ����� ���� �ð� �ҷ�����
            GameSaveData data = LoadSaveMetaOnly();
            if (DateTime.TryParse(data.gameTime, out DateTime gameTime))
            {
                continueDateText.text = $"{gameTime.Month}�� {gameTime.Day}�� �̾��ϱ�";
            }
            continueButton.interactable = true;
        }
        else if (lastDate.HasValue)
        {
            // SaveData.json�� ���� ��¥ ��ϸ� ���� ���
            continueDateText.text = $"{lastDate.Value.Month}�� {lastDate.Value.Day}�� �̾��ϱ�";
            continueButton.interactable = true;
        }
        else
        {
            // �̾��ϱ� �Ұ���
            continueDateText.text = "�̾��ϱ� (����)";
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
        // ���� ������ �̵�
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
            Debug.LogWarning("PauseMenuUI�� �Ҵ���� �ʾҽ��ϴ�.");
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