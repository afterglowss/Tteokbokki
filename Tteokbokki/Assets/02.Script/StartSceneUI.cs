using System;
using System.IO;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SaveData;
using DG.Tweening; // DOTween (선택사항, 여기선 코루틴으로 처리함)

public static class GameLoadFlags
{
    public static bool shouldLoadFromSave = false;
    // ✨ [NEW] 방금 튜토리얼을 "직접 플레이하고" 왔는지 여부
    // true면: 마라소스 지급 + 재료 차감 + 셔터 애니메이션 재생
    // false면: 셔터 없이 바로 시작 (스킵했거나, 이어하기거나)
    public static bool isTutorialJustFinished = false;
}

public class StartSceneUI : MonoBehaviour
{
    [Header("메인 버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button exitButton;

    // ✨ [NEW] 디버그용 세이브 삭제 버튼
    [Header("디버그")]
    [SerializeField] private Button deleteSaveButton;

    [Header("이어하기 정보")]
    [SerializeField] private TextMeshProUGUI continueDateText;

    // ✨ [NEW] 텍스트 색상 설정 추가
    [Header("텍스트 색상 설정")]
    [SerializeField] private Color enableTextColor = Color.white;       // 활성화 (흰색 등)
    [SerializeField] private Color disableTextColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 비활성화 (회색)

    [Header("팝업 & 설정창")]
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private GameObject newGameWarningPanel;
    [SerializeField] private Button warningConfirmButton;
    [SerializeField] private Button warningCancelButton;
    [SerializeField] private RectTransform newGameWindow;

    [Header("제작진(Credit) 팝업")]
    [SerializeField] private Button btnShowCredits;      // 메인 화면의 '제작진/출처' 버튼
    [SerializeField] private GameObject panelCredits;    // 팝업 패널 전체 (검은 배경 포함)
    [SerializeField] private Button btnCloseCredits;     // 팝업 안의 'X' 닫기 버튼
    [SerializeField] private RectTransform creditWindow; // (선택) 팝업 창 본체 (애니메이션용)

    [Header("✨ 로딩창 UI")]
    [SerializeField] private GameObject loadingPanel;    // 로딩 패널 전체
    [SerializeField] private Slider loadingSlider;       // 진행률 게이지
    [SerializeField] private TextMeshProUGUI loadingText; // "재료 손질 중..." 텍스트

    [Header("✨ 타이핑 효과 설정")]
    [SerializeField] private float typingSpeed = 0.05f; // 글자당 0.05초
    [SerializeField] private float textStayDelay = 1.0f; // 다 쓰고 나서 1초 대기

    [Header("✨ 로딩 연출 설정")]
    //[SerializeField] private float minLoadingTime = 2.0f;

    [Header("Scene Settings")]
    [SerializeField] private string mainSceneName = "MainScene";       // 이어하기 -> 바로 게임
    [SerializeField] private string tutorialSceneName = "TutorialScene"; // 새 게임 -> 튜토리얼(얀 스크립트)

    // ✨ [핵심] 로딩 속도 그래프 (인스펙터에서 설정)
    // X축: 시간 (0~1), Y축: 진행률 (0~1)
    [SerializeField]
    private AnimationCurve loadingCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.5f, 0.7f), // 중간에 확 오르고
        new Keyframe(0.8f, 0.85f), // 80%쯤에서 살짝 느려지다가
        new Keyframe(1, 1) // 마지막에 완료
    );

    // 재미있는 로딩 멘트들
    private string[] loadingTips = new string[]
    {
        "재료를 손질하는 중...",
        "육수를 끓이는 중...",
        "단무지를 채워넣는 중...",
        "앞치마를 매는 중...",
        "진상 손님 예방 훈련 중...",
        "배달 오토바이 시동 거는 중...",
        "사장님 지갑 확인하는 중..."
    };

    private string saveFilePath;
    private Coroutine typingCoroutine;

    private enum PopupAction { None, NewGame, DeleteSave }
    private PopupAction currentPopupAction = PopupAction.None;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "SaveData.json");
        if (newGameWarningPanel != null) newGameWarningPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false); // 시작할 땐 끄기
    }

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);

        if (deleteSaveButton != null)
            deleteSaveButton.onClick.AddListener(OnDeleteSaveButtonClicked);

        // 팝업 버튼은 이제 상황에 따라 다르게 동작함
        if (warningConfirmButton != null) warningConfirmButton.onClick.AddListener(OnConfirmNewGame);
        if (warningCancelButton != null) warningCancelButton.onClick.AddListener(OnCancelNewGame);

        // ✨ [NEW] 제작진 팝업 버튼 연결
        if (btnShowCredits != null)
            btnShowCredits.onClick.AddListener(OnShowCreditsClicked);

        if (btnCloseCredits != null)
            btnCloseCredits.onClick.AddListener(OnCloseCreditsClicked);

        // 시작할 땐 꺼두기
        if (panelCredits != null) panelCredits.SetActive(false);
        if (newGameWarningPanel != null) newGameWarningPanel.SetActive(false);

        UpdateContinueDateLabel();

        if (AudioManager.Instance != null)
        {
            // 혹시 켜져있을지 모를 루프 SFX 끄기 (안전장치)
            AudioManager.Instance.StopLoopSFX(501);

            // 배경음악 재생 (볼륨은 매니저가 알아서 처리하므로 ID만 넘겨도 됨)
            AudioManager.Instance.PlayBGM(201, AudioManager.Instance.GetBGMVolume());
        }
    }

    // ✨ [NEW] 팝업 열기
    private void OnShowCreditsClicked()
    {
        if (panelCredits != null&& !panelCredits.activeSelf)
        {
            panelCredits.SetActive(true);

            // (연출) 팝업 창이 '뿅' 하고 튀어나오는 효과
            if (creditWindow != null)
            {
                creditWindow.localScale = Vector3.zero;
                creditWindow.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }
        else if (panelCredits != null && panelCredits.activeSelf)
        {
            OnCloseCreditsClicked();
        }
    }

    // ✨ [NEW] 팝업 닫기
    private void OnCloseCreditsClicked()
    {
        if (panelCredits != null)
        {
            // (연출) 팝업 창이 작아지면서 사라짐
            if (creditWindow != null)
            {
                creditWindow.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    panelCredits.SetActive(false);
                });
            }
            else
            {
                panelCredits.SetActive(false);
            }
        }
    }

    // ... (UpdateContinueDateLabel, LoadSaveMetaOnly는 기존과 동일) ...
    private void UpdateContinueDateLabel()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                GameSaveData data = LoadSaveMetaOnly();
                if (DateTime.TryParse(data.gameTime, out DateTime gameTime))
                {
                    continueDateText.text = $"{gameTime.Month}월 {gameTime.Day}일 영업 기록";
                }
                else
                {
                    continueDateText.text = "이어하기";
                }
                // ✨ [수정] 활성화 상태 색상 적용
                continueButton.interactable = true;
                if (continueDateText != null) 
                { 
                    continueDateText.color = enableTextColor; 
                    continueButton.GetComponentInChildren<TextMeshProUGUI>().color = enableTextColor;
                }

                // 파일이 있으면 삭제 버튼도 활성화
                if (deleteSaveButton != null) deleteSaveButton.interactable = true;
            }
            catch
            {
                continueDateText.text = "데이터 손상됨";
                // ✨ [수정] 비활성화 상태 색상 적용
                continueButton.interactable = false;
                if (continueDateText != null)
                {
                    continueDateText.color = disableTextColor;
                    continueButton.GetComponentInChildren<TextMeshProUGUI>().color = disableTextColor;
                }

                // 파일 없으면 삭제 버튼 비활성화
                if (deleteSaveButton != null) deleteSaveButton.interactable = false;
            }
        }
        else
        {
            continueDateText.text = "기록이 없습니다";
            // ✨ [수정] 비활성화 상태 색상 적용
            continueButton.interactable = false;
            if (continueDateText != null)
            {
                continueDateText.color = disableTextColor;
                continueButton.GetComponentInChildren<TextMeshProUGUI>().color = disableTextColor;
            }
        }
    }

    private GameSaveData LoadSaveMetaOnly()
    {
        string json = File.ReadAllText(saveFilePath);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<GameSaveData>(json);
    }

    // --- 버튼 이벤트 ---

    private void OnStartButtonClicked()
    {
        if (File.Exists(saveFilePath))
        {
            // 새 게임인데 파일이 있음 -> 팝업 띄우고 목적 설정
            currentPopupAction = PopupAction.NewGame;
            OpenNewGamePopup();
        }
        else
        {
            StartNewGameRoutine();
        }
    }

    // ✨ [NEW] 팝업 여는 함수 (애니메이션 추가)
    private void OpenNewGamePopup()
    {
        if (newGameWarningPanel != null)
        {
            newGameWarningPanel.SetActive(true);

            // 팝업 뿅! 등장
            if (newGameWindow != null)
            {
                newGameWindow.localScale = Vector3.zero;
                newGameWindow.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }
    }

    // ✨ [NEW] 취소 버튼 (애니메이션 닫기)
    private void OnCancelNewGame()
    {
        if (newGameWarningPanel != null)
        {
            // 팝업 쏙! 사라짐
            if (newGameWindow != null)
            {
                newGameWindow.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    newGameWarningPanel.SetActive(false);
                });
            }
            else
            {
                newGameWarningPanel.SetActive(false);
            }
        }
    }

    private void OnConfirmNewGame()
    {
        if (newGameWarningPanel != null) newGameWarningPanel.SetActive(false);
        GameLoadFlags.shouldLoadFromSave = false;

        // ✨ 로딩창 코루틴 시작
        StartCoroutine(LoadSceneWithLoadingScreen(tutorialSceneName));
    }

    private void OnContinueButtonClicked()
    {
        GameLoadFlags.shouldLoadFromSave = true;
        // ✨ 로딩창 코루틴 시작
        StartCoroutine(LoadSceneWithLoadingScreen(mainSceneName));
    }

    // ✨ [NEW] 삭제 버튼 클릭 시
    private void OnDeleteSaveButtonClicked()
    {
        if (File.Exists(saveFilePath))
        {
            // 삭제 의도 -> 팝업 띄우고 목적 설정
            currentPopupAction = PopupAction.DeleteSave;
            if (newGameWarningPanel != null) newGameWarningPanel.SetActive(true);
        }
    }

    // ✨ [수정] 팝업 확인 버튼 (통합됨)
    private void OnConfirmPopup()
    {
        if (newGameWarningPanel != null) newGameWarningPanel.SetActive(false);

        switch (currentPopupAction)
        {
            case PopupAction.NewGame:
                StartNewGameRoutine();
                break;

            case PopupAction.DeleteSave:
                DeleteSaveData();
                break;
        }

        currentPopupAction = PopupAction.None; // 초기화
    }

    // ✨ [수정] 팝업 취소 버튼
    private void OnCancelPopup()
    {
        if (newGameWarningPanel != null) newGameWarningPanel.SetActive(false);
        currentPopupAction = PopupAction.None;
    }

    // (기능 분리) 새 게임 시작 로직
    private void StartNewGameRoutine()
    {
        GameLoadFlags.shouldLoadFromSave = false;
        GameLoadFlags.isTutorialJustFinished = false;
        StartCoroutine(LoadSceneWithLoadingScreen(tutorialSceneName));
    }

    // ✨ [NEW] 실제 파일 삭제 로직
    private void DeleteSaveData()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("[System] 세이브 파일이 삭제되었습니다.");
        }

        // UI 즉시 갱신 (이어하기 버튼 비활성화됨)
        UpdateContinueDateLabel();
    }

    // ✨ [핵심] 로딩 스크린 연출 코루틴
    // ✨ [수정] 로딩 스크린 연출 코루틴
    private IEnumerator LoadSceneWithLoadingScreen(string targetSceneName)
    {
        // 1. 로딩창 활성화
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingSlider != null) loadingSlider.value = 0f;

            // ✨ 타이핑 효과 시작! (기존 텍스트 설정 로직 대체)
            if (loadingText != null && loadingTips.Length > 0)
            {
                // 혹시 돌고 있을 코루틴 정지 후 새로 시작
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypewriterLoop());
            }
        }

        // 2. 비동기 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        float timer = 0.0f;
        float minLoadingTime = 2.0f; // 타이핑 효과를 좀 보여주기 위해 최소 시간을 살짝 늘림

        // 3. 로딩 진행 루프
        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            float realProgress = op.progress / 0.9f;
            // 2. ✨ [수정] 커브를 이용한 가짜 진행률 계산
            // 시간이 지날수록(timer/minLoadingTime) 커브의 Y값을 가져옵니다.
            float timeRatio = Mathf.Clamp01(timer / minLoadingTime);
            float fakeProgress = loadingCurve.Evaluate(timeRatio);

            // 실제 로딩과 가짜 로딩 중 '더 느린 쪽'을 보여줌 (로딩이 안 끝났는데 100% 되면 안 되니까)
            // 하지만 'fakeProgress'가 주도권을 갖도록 Min을 쓰되, 
            // 실제 로딩이 너무 느리면 슬라이더가 멈춰있게 됩니다.
            float finalProgress = Mathf.Min(realProgress, fakeProgress);

            if (loadingSlider != null)
            {
                loadingSlider.value = finalProgress;
            }

            // 로딩 완료 & 최소 시간 경과
            // 로딩 완료 & 연출 시간 종료
            if (op.progress >= 0.9f && timer >= minLoadingTime)
            {
                if (loadingSlider != null) loadingSlider.value = 1f;

                // 타이핑 멈춤 & 완료 멘트
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                if (loadingText != null) loadingText.text = "영업 준비 완료!";

                yield return new WaitForSeconds(0.5f);

                op.allowSceneActivation = true;
            }
        }
    }

    // ✨ [NEW] 타자기 효과 코루틴
    private IEnumerator TypewriterLoop()
    {
        while (true) // 무한 반복 (로딩 끝날 때 StopCoroutine으로 멈춤)
        {
            // 1. 랜덤 멘트 뽑기
            string targetText = loadingTips[UnityEngine.Random.Range(0, loadingTips.Length)];

            // 2. 텍스트 비우기
            loadingText.text = "";

            // 3. 한 글자씩 타이핑
            foreach (char c in targetText)
            {
                loadingText.text += c;
                // 타닥거리는 소리를 여기서 재생해도 좋습니다!
                // if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(TypeSoundID); 

                yield return new WaitForSeconds(typingSpeed);
            }

            // 4. 다 쓰고 나서 잠시 대기 (유저가 읽을 시간)
            yield return new WaitForSeconds(textStayDelay);

            // 5. 다시 싹 지우고(루프 처음으로 돌아감) 다음 멘트 타이핑
        }
    }

    private void OnSettingButtonClicked()
    {
        if (pauseMenuUI != null) pauseMenuUI.TogglePausePanel();
    }

    private void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}