using System;
using System.IO;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SaveData;
using DG.Tweening; // DOTween (선택사항, 여기선 코루틴으로 처리함)
using UnityEngine.Localization.Settings;

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

    [Header("언어 설정")]
    [SerializeField] private Toggle languageToggle;
    [SerializeField] private TextMeshProUGUI languageToggleText;

    // ✨ [NEW] 타이틀 로고 애니메이션용 변수
    [Header("타이틀 애니메이션")]
    [SerializeField] private RectTransform titleLogoRect;

    // ✨ [수정] Sprite 스왑 대신 GameObject 껐다 켜기!
    [Header("언어별 로고 오브젝트")]
    [SerializeField] private GameObject logoObjectKorean;
    [SerializeField] private GameObject logoObjectEnglish;

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

    // ✨ [수정] 한글 대신 번역 Key를 저장해 둡니다.
    private string[] loadingTipKeys = new string[]
    {
        "Loading_Tip_1",
        "Loading_Tip_2",
        "Loading_Tip_3",
        "Loading_Tip_4",
        "Loading_Tip_5",
        "Loading_Tip_6",
        "Loading_Tip_7"
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

    private static bool isLanguageSynced = false;

    private IEnumerator Start()
    {
        // 1. 아직 언어 체크를 안 한 '진짜 최초 실행'일 때만 들어갑니다.
        if (!isLanguageSynced)
        {
            isLanguageSynced = true; // 도장 쾅! 이제 다신 이 블록 안 들어옴.

            int currentLangIndex = PlayerPrefs.GetInt("GameLanguage", 1);
            if (currentLangIndex == 0) // 어? 영어가 저장되어 있네?
            {
                // 유니티 언어를 영어로 잽싸게 바꾸고
                yield return LocalizationSettings.InitializationOperation;
                foreach (var loc in LocalizationSettings.AvailableLocales.Locales)
                {
                    if (loc.Identifier.Code.StartsWith("en"))
                    {
                        LocalizationSettings.SelectedLocale = loc;
                        break;
                    }
                }

                // ✨ 점장님 아이디어 발동! 씬을 그냥 바로 다시 켜버립니다.
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield break; // 여기서 현재 코드 실행 완전 종료!
            }
        }

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

        // ✨ 1. PlayerPrefs에서 저장된 언어 불러오기 (0: 한국어, 1: 영어)
        if (languageToggle != null)
        {
            int currentLangIndex = PlayerPrefs.GetInt("GameLanguage", 1); // 기본값 0 (한국어)
            bool isEnglish = (currentLangIndex == 0);

            // ✨ [핵심 수정] 언어에 맞춰서 오브젝트 자체를 껐다 켭니다!
            if (logoObjectKorean != null) logoObjectKorean.SetActive(!isEnglish); // 영어면 끄고, 한국어면 켬
            if (logoObjectEnglish != null) logoObjectEnglish.SetActive(isEnglish); // 영어면 켜고, 한국어면 끔

            languageToggle.SetIsOnWithoutNotify(isEnglish);
            if (languageToggleText != null) languageToggleText.text = isEnglish ? "English" : "Korean";
            languageToggle.onValueChanged.AddListener(OnLanguageToggleChanged);

            //StartCoroutine(InitLocaleOnStart(isEnglish));
        }

        UpdateContinueDateLabel();

        if (AudioManager.Instance != null)
        {
            // 혹시 켜져있을지 모를 루프 SFX 끄기 (안전장치)
            AudioManager.Instance.StopLoopSFX(501);

            // 배경음악 재생 (볼륨은 매니저가 알아서 처리하므로 ID만 넘겨도 됨)
            AudioManager.Instance.PlayBGM(201, AudioManager.Instance.GetBGMVolume());
        }

        PlayTitleAnimation();
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
                    continueDateText.text = TextTranslator.GetUIText("Start_SaveRecord", gameTime.Month, gameTime.Day);
                }
                else
                {
                    continueDateText.text = TextTranslator.GetUIText("Start_Continue");
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
                // ✨ 3. 데이터 손상 번역
                continueDateText.text = TextTranslator.GetUIText("Start_DataCorrupted");
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
            continueDateText.text = TextTranslator.GetUIText("Start_NoRecord");
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
        switch (currentPopupAction)
        {
            case PopupAction.NewGame:
                GameLoadFlags.shouldLoadFromSave = false;
                DeleteSaveData();
                // ✨ 로딩창 코루틴 시작
                StartCoroutine(LoadSceneWithLoadingScreen(tutorialSceneName));
                break;

            case PopupAction.DeleteSave:
                DeleteSaveData();
                break;
        }
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

            if (loadingText != null && loadingTipKeys.Length > 0) // ✨ 이름 변경 완료!
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

                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                // ✨ "영업 준비 완료!" 번역
                if (loadingText != null) loadingText.text = TextTranslator.GetUIText("Loading_Complete");

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
            // ✨ 1. 랜덤 Key 뽑기
            string targetKey = loadingTipKeys[UnityEngine.Random.Range(0, loadingTipKeys.Length)];

            // ✨ 2. 번역된 진짜 멘트 가져오기
            string targetText = TextTranslator.GetUIText(targetKey);

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

    // ✨ [NEW] 타이틀 애니메이션 전용 함수
    private void PlayTitleAnimation()
    {
        if (titleLogoRect == null) return;

        // 다른 씬에 갔다가 돌아올 때를 대비해 DOTween 초기화
        titleLogoRect.DOKill();


        //// ==========================================
        //// 💥 후보 2. 위에서 툭! 띠용~ 바운스 (테스트하려면 주석 해제)
        //// ==========================================
        //titleLogoRect.localScale = Vector3.one; // 크기 초기화
        //float originalY = titleLogoRect.anchoredPosition.y;
        //// 위로 600만큼 올려놓은 상태에서
        //titleLogoRect.anchoredPosition = new Vector2(titleLogoRect.anchoredPosition.x, originalY + 600f);
        //// 원래 자리로 탱탱볼처럼 떨어짐
        //titleLogoRect.DOAnchorPosY(originalY, 1.5f).SetEase(Ease.OutBounce);

    }
    // ✨ 2. 토글을 눌렀을 때 실행될 함수
    // ✨ 2. 토글을 눌렀을 때 실행될 함수
    private void OnLanguageToggleChanged(bool isEnglish)
    {
        // 클릭하는 순간 바로 글자를 바꿔줍니다! (Korean -> English)
        if (languageToggleText != null)
            languageToggleText.text = isEnglish ? "English" : "Korean";

        // ✨ [수정] 영어가 0번, 한국어가 1번!
        int targetLangIndex = isEnglish ? 0 : 1;
        PlayerPrefs.SetInt("GameLanguage", targetLangIndex);
        PlayerPrefs.Save();

        StartCoroutine(ChangeLocaleAndReload(isEnglish));
    }

    // ✨ 3. 인덱스가 아니라 정확한 언어 코드("en", "ko")로 찾아서 매칭합니다!
    private IEnumerator ChangeLocaleAndReload(bool isEnglish)
    {
        yield return LocalizationSettings.InitializationOperation;

        string targetCode = isEnglish ? "en" : "ko";
        UnityEngine.Localization.Locale targetLocale = null;

        // 시스템에 등록된 언어 리스트를 싹 뒤져서 코드가 일치하는 걸 찾아냅니다.
        foreach (var loc in LocalizationSettings.AvailableLocales.Locales)
        {
            if (loc.Identifier.Code.StartsWith(targetCode))
            {
                targetLocale = loc;
                break;
            }
        }

        if (targetLocale != null)
            LocalizationSettings.SelectedLocale = targetLocale;
        else
            Debug.LogWarning($"[시스템] '{targetCode}' 언어를 유니티 Localization 설정에서 찾을 수 없습니다!");

        yield return new WaitForSeconds(0.1f);
        StartCoroutine(LoadSceneWithLoadingScreen(SceneManager.GetActiveScene().name));
    }

    // ✨ [NEW] 게임 시작 시 언어 동기화 코루틴
    private IEnumerator InitLocaleOnStart(bool isEnglish)
    {
        yield return LocalizationSettings.InitializationOperation;

        string targetCode = isEnglish ? "en" : "ko";
        foreach (var loc in LocalizationSettings.AvailableLocales.Locales)
        {
            if (loc.Identifier.Code.StartsWith(targetCode))
            {
                LocalizationSettings.SelectedLocale = loc;
                break;
            }
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