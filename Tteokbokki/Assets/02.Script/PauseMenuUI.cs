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
    [Header("설정 패널")]
    [SerializeField] private RectTransform panelPause;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button closeButton;

    [Header("오른쪽 패널들")]
    [SerializeField] private List<GameObject> rightPanels;
    [Header("메뉴 버튼들 (왼쪽)")]
    [SerializeField] private List<Button> menuButtons;

    // ✨ 색상 변수 분리 (인스펙터에서 따로 설정하세요!)
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

    private void Start()
    {
        closeButton.onClick.AddListener(TogglePausePanel);
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(TogglePausePanel);
        }
        applyButton.onClick.AddListener(ApplyBtnClick);

        panelPause.anchoredPosition = new Vector2(0, 1530);

        for (int i = 0; i < menuButtons.Count; i++)
        {
            int index = i;
            menuButtons[i].onClick.AddListener(() => ShowPanel(index));
            AddHoverEvents(menuButtons[i], index);
        }

        // 기본 선택: 소리 탭 (0번)
        ShowPanel(0);

        sliderMaster.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetMasterVolume(value); });
        sliderBGM.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetBGMVolume(value); });
        sliderSFX.onValueChanged.AddListener((value) => { AudioManager.Instance?.SetSFXVolume(value); });

        sliderMaster.value = 1f;
        sliderBGM.value = 0.5f;
        sliderSFX.value = 0.5f;

        FilterResolutions();
        SetUpDropdown();
        SetUpToggles();
    }

    private void Update()
    {
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

    // ✨ 핵심 수정: 버튼 색과 테두리 색을 각각의 변수에서 가져와 적용
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
            panelPause.DOAnchorPosY(225, 0.4f).SetEase(Ease.OutCubic);
        }
        else
        {
            panelPause.DOAnchorPosY(1530, 0.4f).SetEase(Ease.InCubic);
        }
    }

    public void ShowPanel(int index)
    {
        if (index < 0 || index >= rightPanels.Count) return;

        currentMenuIndex = index;

        for (int i = 0; i < rightPanels.Count; i++)
        {
            rightPanels[i].SetActive(i == index);
        }

        // 버튼 색상 일괄 업데이트
        UpdateAllButtonVisuals(index);
    }

    // ... (이하 코드 동일) ...
    public void MoveStartScene()
    {
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
}