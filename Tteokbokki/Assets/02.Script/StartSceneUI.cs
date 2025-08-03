using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneUI : MonoBehaviour
{
    [Header("메인 버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button exitButton;

    [Header("설정창 UI")]
    [SerializeField] private PauseMenuUI pauseMenuUI;



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
    }

    private void OnStartButtonClicked()
    {
        // 게임 씬으로 이동
        // SceneManager.LoadScene("SampleScene");
        SceneManager.LoadScene("SampleScene 1");
    }

    private void OnContinueButtonClicked()
    {
        // 이어하기 버튼 눌렀을때
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