using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PauseMenuUI : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private bool isMainMenu = false;

    [Header("버튼들")]
    [SerializeField] private GameObject saveButtonObj;   // 저장 버튼 오브젝트
    [SerializeField] private GameObject titleButtonObj;  // 타이틀로 가기 버튼 오브젝트

    [Header("Black Curtain (배경 어둡게)")]
    [SerializeField] private Image blackCurtainImage;
    [SerializeField] private float fadeDuration = 0.4f;
    [Range(0f, 1f)][SerializeField] private float curtainMaxAlpha = 0.7f;

    [Header("설정 패널")]
    [SerializeField] private RectTransform panelPause;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button closeButton;

    [Header("메뉴 버튼들 (왼쪽)")]
    [SerializeField] private List<Button> menuButtons;
    // 메뉴 버튼 강조 이미지
    private List<GameObject> selectionButtons = new List<GameObject>();

    [Header("탭 제목")]
    [SerializeField] private TMP_Text textHeader;
    [SerializeField] private List<string> tabNames = new List<string> { "소리 설정", "화면 설정", "업적" };
    [Header("오른쪽 패널들")]
    [SerializeField] private List<GameObject> rightPanels;

    // 색상 변수 분리
    [Header("버튼 내부 색상 (Image Color)")]
    [SerializeField] private Color normalButtonColor = Color.white;       // 평소 버튼 색
    [SerializeField] private Color selectedButtonColor = Color.white;     // 선택/호버 버튼 색

    [Header("테두리 색상 (Outline Color)")]
    [SerializeField] private Color normalOutlineColor = Color.gray;       // 평소 테두리 색
    [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.976f, 0.231f); // 선택/호버 테두리 색 (노랑)

    [Header("디스플레이 패널")]
    [SerializeField] private Toggle toggleFull;
    [SerializeField] private Toggle toggleWindow;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] public Button applyButton;

    private Resolution[] resolutions;
    private Toggle activatedToggle;
    enum ScreenMode { Full, Window }
    ScreenMode screenMode;

    private bool isPauseOpen = false;
    private int currentMenuIndex = 0;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderBGM;
    [SerializeField] private Slider sliderSFX;

    private void Awake()
    {
        // 씬 시작 시 시간이 멈춰있을 수 있으므로 정상화 (안전장치)
        Time.timeScale = 1f;

        // 시작 시 커튼 비활성화 및 초기화
        if (blackCurtainImage != null)
        {
            blackCurtainImage.gameObject.SetActive(false);
            Color c = blackCurtainImage.color;
            blackCurtainImage.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    private void Start()
    {
        if (isMainMenu)
        {
            if (saveButtonObj != null) saveButtonObj.SetActive(false);
            if (titleButtonObj != null) titleButtonObj.SetActive(false);
        }

        closeButton.onClick.AddListener(TogglePausePanel);
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(TogglePausePanel);
        }
        applyButton.onClick.AddListener(ApplyBtnClick);


        panelPause.anchoredPosition = new Vector2(-2000, 0);
        selectionButtons.Clear();
        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (menuButtons[i] == null) continue;

            // 각 버튼의 첫 번째 자식을 강조 이미지로 수집
            if (menuButtons[i].transform.childCount > 0)
            {
                selectionButtons.Add(menuButtons[i].transform.GetChild(0).gameObject);
            }

            int index = i;
            menuButtons[i].onClick.AddListener(() => ShowPanel(index));
            AddHoverEvents(menuButtons[i], index);
        }

        // 초기 화면 = 소리 탭
        ShowPanel(0);

        // 볼륨 슬라이더 연동
        sliderMaster.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetMasterVolume(value); });
        sliderBGM.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetBGMVolume(value); });
        sliderSFX.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetSFXVolume(value); });

        // 슬라이더 초기값 설정
        // ✨ [수정] 슬라이더 초기값 설정 (AudioManager와 동기화)
        if (AudioManager.Instance != null)
        {
            // 매니저에 저장된 실제 볼륨 값을 가져와서 슬라이더에 반영합니다.
            // (슬라이더 값을 바꾸면 리스너가 트리거되지만, 같은 값으로 설정되므로 문제 없습니다.)
            sliderMaster.value = AudioManager.Instance.GetMasterVolume();
            sliderBGM.value = AudioManager.Instance.GetBGMVolume();
            sliderSFX.value = AudioManager.Instance.GetSFXVolume();
        }
        else
        {
            // 오디오 매니저가 없는 경우(테스트 등)에만 기본값 사용
            sliderMaster.value = 1f;
            sliderBGM.value = 0.5f;
            sliderSFX.value = 0.5f;
        }

        // 디스플레이 설정 초기화
        FilterResolutions();
        SetUpDropdown();
        SetUpToggles();
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.endOfDayPanel != null &&
            GameManager.Instance.endOfDayPanel.activeSelf)
        {
            return;
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
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

        // 호버 중이면 '선택된 색상(Selected)', 아니면 '평소 색상(Normal)' 적용
        SetButtonVisual(index, isHover);
    }

    // 버튼 색과 테두리 색을 각각의 변수에서 가져와 적용
    private void SetButtonVisual(int index, bool isSelectedOrHover)
    {
        // 1. 테두리(Outline) 색상 설정
        var outline = menuButtons[index].GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = isSelectedOrHover ? selectedOutlineColor : normalOutlineColor;
        }

        // 2. 버튼 내부(Image) 색상 설정
        var image = menuButtons[index].GetComponent<Image>();
        if (image != null)
        {
            image.color = isSelectedOrHover ? selectedButtonColor : normalButtonColor;
        }
    }

    private void UpdateAllButtonVisuals(int selectedIndex)
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            // 선택된 인덱스는 true(Selected 색상), 나머지는 false(Normal 색상)
            SetButtonVisual(i, i == selectedIndex);
        }
    }

    public void TogglePausePanel()
    {

        isPauseOpen = !isPauseOpen;

        if (isPauseOpen)
        {
            ShowPanel(0);
            ResetAllScrollViews();

            // ⏸️ 게임 시간 정지
            Time.timeScale = 0f;

            // 🟢 패널 열기
            panelPause.DOKill();
            panelPause.DOAnchorPosX(0, fadeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true); // ⭐ 중요: 게임 시간이 0이어도 애니메이션은 작동하도록 설정

            // 🟢 배경(Curtain) Fade In
            if (blackCurtainImage != null)
            {
                blackCurtainImage.gameObject.SetActive(true);
                blackCurtainImage.DOKill(); // 기존 트윈 제거

                // 알파값을 0부터 시작하지 않고 현재 상태에서 자연스럽게 전환되도록 하려면 
                // Color 초기화를 빼도 되지만, 보통 켜질 땐 0에서 시작하는 것이 깔끔합니다.
                Color c = blackCurtainImage.color;
                blackCurtainImage.color = new Color(c.r, c.g, c.b, 0f);

                blackCurtainImage.DOFade(curtainMaxAlpha, fadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            }
        }
        else
        {
            // ▶️ 게임 시간 재개
            Time.timeScale = 1f;

            // 🔴 패널 닫기
            panelPause.DOKill();
            panelPause.DOAnchorPosX(-2000, fadeDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true); // 중요

            if (blackCurtainImage != null)
            {
                blackCurtainImage.DOKill();
                blackCurtainImage.DOFade(0f, fadeDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true) // 중요
                    .OnComplete(() =>
                    {
                        blackCurtainImage.gameObject.SetActive(false);
                    });
            }
        }
    }

    public void ShowPanel(int index)
    {
        if (index < 0 || index >= rightPanels.Count) return;
        currentMenuIndex = index;

        // 탭 제목 변경
        if (textHeader != null && index < tabNames.Count)
        {
            textHeader.text = tabNames[index];
        }

        for (int i = 0; i < rightPanels.Count; i++)
        {
            rightPanels[i].SetActive(i == index);

            if (selectionButtons != null && i < selectionButtons.Count)
            {
                selectionButtons[i].SetActive(i == index);
            }
        }

        // 버튼 색상 일괄 업데이트
        UpdateAllButtonVisuals(index);
    }

    public void MoveStartScene()
    {
        // 씬 이동 전에 반드시 시간을 다시 흐르게 해야 함!
        Time.timeScale = 1f;

        DOTween.KillAll();

        TogglePausePanel();
        SceneManager.LoadScene("StartScene");
    }

    void FilterResolutions() { resolutions = Screen.resolutions; }

    void SetUpDropdown()
    {
        resolutionDropdown.ClearOptions();
        HashSet<string> options = new HashSet<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " X " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentResolutionIndex = i;
        }
        resolutionDropdown.AddOptions(new List<string>(options));
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    void SetUpToggles()
    {
        toggleFull.onValueChanged.RemoveAllListeners();
        toggleWindow.onValueChanged.RemoveAllListeners();
        bool isFullScreen = Screen.fullScreen;

        if (isFullScreen) { toggleFull.isOn = true; toggleWindow.isOn = false; activatedToggle = toggleFull; screenMode = ScreenMode.Full; }
        else { toggleFull.isOn = false; toggleWindow.isOn = true; activatedToggle = toggleWindow; screenMode = ScreenMode.Window; }

        toggleFull.onValueChanged.AddListener(delegate { ToggleChanged(toggleFull); });
        toggleWindow.onValueChanged.AddListener(delegate { ToggleChanged(toggleWindow); });
    }

    public void SetResolution()
    {
        int resolutionIndex = resolutionDropdown.value;
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    void ToggleChanged(Toggle changedToggle)
    {
        if (changedToggle.isOn)
        {
            activatedToggle = changedToggle;
            if (changedToggle == toggleFull) { toggleWindow.isOn = false; screenMode = ScreenMode.Full; }
            else { toggleFull.isOn = false; screenMode = ScreenMode.Window; }
        }
        else if (activatedToggle == changedToggle) activatedToggle.isOn = true;
        SetScreenMode();
    }

    void SetScreenMode()
    {
        if (screenMode == ScreenMode.Full) { Screen.fullScreenMode = FullScreenMode.FullScreenWindow; Screen.fullScreen = true; }
        else { Screen.fullScreenMode = FullScreenMode.Windowed; Screen.fullScreen = false; }
    }

    public void ApplyBtnClick()
    {
        Resolution selectedResolution = resolutions[resolutionDropdown.value];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
    }

    private void ResetAllScrollViews()
    {
        foreach (var panel in rightPanels)
        {
            if (panel == null) continue;

            // 패널 안에 있는 ScrollRect 컴포넌트들을 찾음
            var scrolls = panel.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scroll in scrolls)
            {
                scroll.verticalNormalizedPosition = 1f; // 맨 위로
            }
        }
    }
}