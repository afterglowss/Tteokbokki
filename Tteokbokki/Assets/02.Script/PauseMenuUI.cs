using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq; // ✨ Linq 사용 필수

public class PauseMenuUI : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private bool isMainMenu = false;

    [Header("버튼들")]
    [SerializeField] private GameObject saveButtonObj;
    [SerializeField] private GameObject titleButtonObj;
    [SerializeField] private Button btnGoToTitle;

    [Header("경고 팝업 (나가기 확인)")]
    [SerializeField] private GameObject quitWarningPanel;
    [SerializeField] private Button btnConfirmQuit;
    [SerializeField] private Button btnCancelQuit;

    [Header("Black Curtain")]
    [SerializeField] private Image blackCurtainImage;
    [SerializeField] private float fadeDuration = 0.4f;
    [Range(0f, 1f)][SerializeField] private float curtainMaxAlpha = 0.7f;

    [Header("설정 패널")]
    [SerializeField] private RectTransform panelPause;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button closeButton;

    [Header("초기화 버튼 (Reset)")]
    [SerializeField] private Button btnResetAudio;   // 소리 탭의 초기화 버튼
    [SerializeField] private Button btnResetDisplay; // 화면 탭의 초기화 버튼

    [Header("메뉴 버튼들 (왼쪽)")]
    [SerializeField] private List<Button> menuButtons;
    private List<GameObject> selectionButtons = new List<GameObject>();

    [Header("탭 제목")]
    [SerializeField] private TMP_Text textHeader;
    [SerializeField] private List<string> tabNames = new List<string> { "소리 설정", "화면 설정", "업적" };
    [Header("오른쪽 패널들")]
    [SerializeField] private List<GameObject> rightPanels;

    [Header("버튼 비주얼")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = Color.white;
    [SerializeField] private Color normalOutlineColor = Color.gray;
    [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.976f, 0.231f);

    [Header("디스플레이 패널 (화면 설정)")]
    [SerializeField] private Toggle toggleFull;
    [SerializeField] private Toggle toggleWindow;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] public Button applyButton;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderBGM;
    [SerializeField] private Slider sliderSFX;

    // ✨ [NEW] 해상도 관리를 위한 리스트
    private List<Resolution> validResolutions = new List<Resolution>();
    private bool isPauseOpen = false;
    private int currentMenuIndex = 0;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (blackCurtainImage != null)
        {
            blackCurtainImage.gameObject.SetActive(false);
            Color c = blackCurtainImage.color;
            blackCurtainImage.color = new Color(c.r, c.g, c.b, 0f);
        }

        if (quitWarningPanel != null) quitWarningPanel.SetActive(false);
    }

    private void Start()
    {
        // 1. 메인 메뉴 여부에 따른 버튼 숨김
        if (isMainMenu)
        {
            if (saveButtonObj != null) saveButtonObj.SetActive(false);
            if (titleButtonObj != null) titleButtonObj.SetActive(false);
        }
        else
        {
            if (btnGoToTitle != null)
            {
                btnGoToTitle.onClick.RemoveAllListeners();
                btnGoToTitle.onClick.AddListener(OnTitleButtonClicked);
            }
        }

        // 2. 팝업 및 닫기 버튼 연결
        if (btnConfirmQuit != null) btnConfirmQuit.onClick.AddListener(OnConfirmQuit);
        if (btnCancelQuit != null) btnCancelQuit.onClick.AddListener(OnCancelQuit);

        if (btnResetAudio != null) btnResetAudio.onClick.AddListener(ResetAudioSettings);
        if (btnResetDisplay != null) btnResetDisplay.onClick.AddListener(ResetDisplaySettings);

        closeButton.onClick.AddListener(TogglePausePanel);
        if (optionButton != null) optionButton.onClick.AddListener(TogglePausePanel);

        // ✨ [핵심] 화면 설정 초기화 (해상도 & 토글)
        InitializeDisplaySettings();

        // 3. 왼쪽 메뉴 버튼 초기화
        InitializeMenuButtons();

        // 4. 볼륨 슬라이더 초기화
        InitializeAudioSettings();

        // 초기 화면 = 소리 탭
        ShowPanel(0);
    }

    private void InitializeMenuButtons()
    {
        panelPause.anchoredPosition = new Vector2(-2000, 0);
        selectionButtons.Clear();
        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (menuButtons[i] == null) continue;
            if (menuButtons[i].transform.childCount > 0)
            {
                selectionButtons.Add(menuButtons[i].transform.GetChild(0).gameObject);
            }

            int index = i;
            menuButtons[i].onClick.AddListener(() => ShowPanel(index));
            AddHoverEvents(menuButtons[i], index);
        }
    }

    private void InitializeAudioSettings()
    {
        sliderMaster.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetMasterVolume(value); });
        sliderBGM.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetBGMVolume(value); });
        sliderSFX.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetSFXVolume(value); });

        if (AudioManager.Instance != null)
        {
            sliderMaster.value = AudioManager.Instance.GetMasterVolume();
            sliderBGM.value = AudioManager.Instance.GetBGMVolume();
            sliderSFX.value = AudioManager.Instance.GetSFXVolume();
        }
        else
        {
            sliderMaster.value = 1f;
            sliderBGM.value = 0.5f;
            sliderSFX.value = 0.5f;
        }
    }

    // ✨ [핵심] 화면 설정 초기화 함수 (Start에서 호출)
    private void InitializeDisplaySettings()
    {
        // 1. 16:9 해상도만 필터링해서 리스트업
        validResolutions.Clear();
        Resolution[] allResolutions = Screen.resolutions;

        foreach (Resolution res in allResolutions)
        {
            // 16:9 비율 계산 (1.7777...)
            float ratio = (float)res.width / res.height;

            // 오차범위 0.01 내외로 16:9인지 확인 (1920x1080, 1600x900, 1280x720 등)
            if (Mathf.Abs(ratio - (16f / 9f)) < 0.01f)
            {
                // 중복 제거: 이미 리스트에 같은 너비/높이가 있으면 스킵 (주사율 차이 무시)
                if (!validResolutions.Any(x => x.width == res.width && x.height == res.height))
                {
                    validResolutions.Add(res);
                }
            }
        }

        // 혹시 16:9 해상도가 하나도 없다면? (특이한 모니터) -> 현재 해상도라도 넣음
        if (validResolutions.Count == 0)
        {
            validResolutions.Add(Screen.currentResolution);
        }

        // 큰 해상도 순으로 정렬 (필요시 Reverse 제거)
        // validResolutions.Reverse(); 

        // 2. 드롭다운 채우기
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < validResolutions.Count; i++)
        {
            string option = validResolutions[i].width + " x " + validResolutions[i].height;
            options.Add(option);

            // 현재 스크린 해상도와 일치하는지 체크
            if (validResolutions[i].width == Screen.width &&
                validResolutions[i].height == Screen.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        // 3. 토글 초기 상태 설정 (실제 화면 모드 반영)
        bool isFull = Screen.fullScreen;
        toggleFull.SetIsOnWithoutNotify(isFull);
        toggleWindow.SetIsOnWithoutNotify(!isFull);

        // 4. 적용 버튼 이벤트 연결
        applyButton.onClick.RemoveAllListeners();
        applyButton.onClick.AddListener(ApplyDisplaySettings);

        // 5. 토글 이벤트 (단순히 서로 끄는 역할만)
        toggleFull.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) toggleWindow.SetIsOnWithoutNotify(false);
            else if (!toggleWindow.isOn) toggleFull.SetIsOnWithoutNotify(true); // 둘 다 꺼짐 방지
        });

        toggleWindow.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) toggleFull.SetIsOnWithoutNotify(false);
            else if (!toggleFull.isOn) toggleWindow.SetIsOnWithoutNotify(true); // 둘 다 꺼짐 방지
        });
    }

    // ✨ [핵심] 적용 버튼 클릭 시 실행되는 함수
    public void ApplyDisplaySettings()
    {
        // 1. 선택된 해상도 가져오기
        int index = resolutionDropdown.value;
        if (index < 0 || index >= validResolutions.Count) index = 0; // 안전장치

        Resolution targetRes = validResolutions[index];

        // 2. 선택된 화면 모드 확인
        bool isFullScreen = toggleFull.isOn;

        // 3. 해상도 및 모드 적용
        // FullScreenWindow: 전체화면 창모드 (Alt-Tab 빠름, 최신 게임 국룰)
        // Windowed: 일반 창모드
        FullScreenMode mode = isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(targetRes.width, targetRes.height, mode);

        Debug.Log($"[화면 설정 변경] {targetRes.width}x{targetRes.height}, FullScreen: {isFullScreen}");

        // (선택사항) 설정 저장
        // PlayerPrefs.SetInt("ResolutionWidth", targetRes.width);
        // PlayerPrefs.SetInt("ResolutionHeight", targetRes.height);
        // PlayerPrefs.SetInt("IsFullScreen", isFullScreen ? 1 : 0);
        // PlayerPrefs.Save();
    }

    // ... (이하 Update, Hover 이벤트, TogglePausePanel, ShowPanel, Quit 관련 등 기존 코드 그대로 유지) ...

    private void Update()
    {

        if (Keyboard.current == null) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.endOfDayUIHandler != null &&
            GameManager.Instance.endOfDayUIHandler.IsShutterAnimating)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (quitWarningPanel != null && quitWarningPanel.activeSelf)
                OnCancelQuit();
            else
                TogglePausePanel();
        }
    }

    private void AddHoverEvents(Button btn, int index)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnButtonHover(index, true); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnButtonHover(index, false); });
        trigger.triggers.Add(entryExit);
    }

    private void OnButtonHover(int index, bool isHover)
    {
        if (index == currentMenuIndex) return;
        SetButtonVisual(index, isHover);
    }

    private void SetButtonVisual(int index, bool isSelectedOrHover)
    {
        var outline = menuButtons[index].GetComponent<Outline>();
        if (outline != null) outline.effectColor = isSelectedOrHover ? selectedOutlineColor : normalOutlineColor;

        var image = menuButtons[index].GetComponent<Image>();
        if (image != null) image.color = isSelectedOrHover ? selectedButtonColor : normalButtonColor;
    }

    private void UpdateAllButtonVisuals(int selectedIndex)
    {
        for (int i = 0; i < menuButtons.Count; i++) SetButtonVisual(i, i == selectedIndex);
    }

    public void TogglePausePanel()
    {
        isPauseOpen = !isPauseOpen;

        if (isPauseOpen)
        {
            ShowPanel(0);
            ResetAllScrollViews();
            Time.timeScale = 0f;

            GameClock.Pause();

            panelPause.DOKill();
            panelPause.DOAnchorPosX(0, fadeDuration).SetEase(Ease.OutCubic).SetUpdate(true);

            if (blackCurtainImage != null)
            {
                blackCurtainImage.gameObject.SetActive(true);
                blackCurtainImage.DOKill();
                Color c = blackCurtainImage.color;
                blackCurtainImage.color = new Color(c.r, c.g, c.b, 0f);
                blackCurtainImage.DOFade(curtainMaxAlpha, fadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PauseAllLoopSFX(true);
        }
        else
        {
            Time.timeScale = 1f;
            GameClock.Resume();

            if (quitWarningPanel != null) quitWarningPanel.SetActive(false);

            panelPause.DOKill();
            panelPause.DOAnchorPosX(-2000, fadeDuration).SetEase(Ease.InCubic).SetUpdate(true);

            if (blackCurtainImage != null)
            {
                blackCurtainImage.DOKill();
                blackCurtainImage.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).SetUpdate(true)
                    .OnComplete(() => { blackCurtainImage.gameObject.SetActive(false); });
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PauseAllLoopSFX(false);
        }
    }

    public void ShowPanel(int index)
    {
        if (index < 0 || index >= rightPanels.Count) return;
        currentMenuIndex = index;

        if (textHeader != null && index < tabNames.Count) textHeader.text = tabNames[index];

        for (int i = 0; i < rightPanels.Count; i++)
        {
            rightPanels[i].SetActive(i == index);
            if (selectionButtons != null && i < selectionButtons.Count) selectionButtons[i].SetActive(i == index);
        }
        UpdateAllButtonVisuals(index);
    }

    private void OnTitleButtonClicked()
    {
        PunchButton(btnGoToTitle);
        if (quitWarningPanel != null) quitWarningPanel.SetActive(true);
        else MoveStartScene();
    }

    private void OnConfirmQuit()
    {
        PunchButton(btnConfirmQuit);
        MoveStartScene();
    }

    private void OnCancelQuit()
    {
        if (quitWarningPanel != null) quitWarningPanel.SetActive(false);

        PunchButton(btnCancelQuit);
    }

    // ✨ [NEW] 소리 설정 초기화
    public void ResetAudioSettings()
    {
        // 슬라이더 값을 변경하면 onValueChanged 리스너가 작동해서 
        // 자동으로 AudioManager와 PlayerPrefs에 저장까지 수행합니다.
        if (sliderMaster != null) sliderMaster.value = 1.0f;
        if (sliderBGM != null) sliderBGM.value = 0.5f;
        if (sliderSFX != null) sliderSFX.value = 0.5f;

        // (선택사항) 버튼 펀치 효과
        PunchButton(btnResetAudio);
        Debug.Log("[설정] 소리 설정을 기본값으로 초기화했습니다.");
    }

    // ✨ [NEW] 화면 설정 초기화 (FHD & 전체화면)
    public void ResetDisplaySettings()
    {
        // 1. 전체화면으로 변경
        if (toggleFull != null) toggleFull.isOn = true;
        // (토글 리스너에 의해 toggleWindow는 자동으로 꺼짐)

        // 2. 1920x1080 해상도 찾기
        int defaultIndex = -1;
        for (int i = 0; i < validResolutions.Count; i++)
        {
            // 너비가 1920이고 높이가 1080인 항목 찾기
            if (validResolutions[i].width == 1920 && validResolutions[i].height == 1080)
            {
                defaultIndex = i;
                break;
            }
        }

        // 3. 만약 1920x1080이 없다면? -> 가장 높은 해상도(보통 리스트의 마지막 or 첫번째) 선택
        // (validResolutions 정렬 상태에 따라 다를 수 있으니 안전하게 Max 찾기)
        if (defaultIndex == -1 && validResolutions.Count > 0)
        {
            // 너비가 가장 큰 해상도의 인덱스를 찾음
            var maxRes = validResolutions.OrderByDescending(r => r.width).First();
            defaultIndex = validResolutions.IndexOf(maxRes);
        }

        // 4. 드롭다운 값 변경
        if (defaultIndex != -1 && resolutionDropdown != null)
        {
            resolutionDropdown.value = defaultIndex;
            resolutionDropdown.RefreshShownValue();
        }

        // 5. 즉시 적용 (Apply 버튼을 누른 것과 동일한 효과)
        ApplyDisplaySettings();

        // (선택사항) 버튼 펀치 효과
        PunchButton(btnResetDisplay);
        Debug.Log("[설정] 화면 설정을 권장 기본값(1920x1080, Full)으로 초기화했습니다.");
    }

    // (유틸) 버튼 클릭 효과
    private void PunchButton(Button btn)
    {
        if (btn != null)
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.2f, 10, 1);
        }
    }

    public void MoveStartScene()
    {
        Time.timeScale = 1f;
        DOTween.KillAll();

        // ✨ [핵심 해결] 씬을 나갈 때 좀비 사운드(부글부글)를 모조리 죽입니다.
        // 이걸 해야 StartScene에서 소리가 안 들리고, 다시 들어왔을 때 중첩되지 않습니다.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllLoopSFX();
        }

        SceneManager.LoadScene("StartScene");
    }

    private void ResetAllScrollViews()
    {
        foreach (var panel in rightPanels)
        {
            if (panel == null) continue;
            var scrolls = panel.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scroll in scrolls) scroll.verticalNormalizedPosition = 1f;
        }
    }
}