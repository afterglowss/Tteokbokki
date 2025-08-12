using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;

public class PauseMenuUI : MonoBehaviour
{
    [Header("설정 패널")]
    [SerializeField] private RectTransform panelPause;
    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("오른쪽 패널들")]
    [SerializeField] private List<GameObject> rightPanels; // 오른쪽 패널 (Sound, Display, Achievement)
    [Header("메뉴 버튼들")]
    [SerializeField] private List<Button> menuButtons; // 왼쪽 패널 (Button_Sound, Button_Display, Button_Achievement)

    [Header("디스플레이 패널")]
    [SerializeField] private Toggle toggleFull;                 //  전체화면으로
    [SerializeField] private Toggle toggleWindow;               //  창모드로
    [SerializeField] private TMP_Dropdown resolutionDropdown;   //  해상도 선택 드롭다운
    [SerializeField] public Button applyButton;                 //  해상도 적용 버튼

    private Resolution[] resolutions;   // 가능한 모든 해상도를 저장할 배열
    private Toggle activatedToggle;         // 활성화된 모드
    enum ScreenMode{ Full, Window } ScreenMode screenMode;

    private bool isPauseOpen = false;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderBGM;
    [SerializeField] private Slider sliderSFX;


    private void Start()
    {
        closeButton.onClick.AddListener(TogglePausePanel);
        applyButton.onClick.AddListener(ApplyBtnClick);

        panelPause.anchoredPosition = new Vector2(0, 1530); // 시작 시 숨기기

        // 기본 선택: 소리 탭
        ShowPanel(0);
        // 각 메뉴 버튼에 클릭 리스너 등록
        for (int i = 0; i < menuButtons.Count; i++)
        {
            int index = i;
            menuButtons[i].onClick.AddListener(() => ShowPanel(index));
        }

        // 볼륨 슬라이더 연결
        sliderMaster.onValueChanged.AddListener((value) =>
        {
            AudioManager.Instance?.SetMasterVolume(value);
        });

        sliderBGM.onValueChanged.AddListener((value) =>
        {
            AudioManager.Instance?.SetBGMVolume(value);
        });

        sliderSFX.onValueChanged.AddListener((value) =>
        {
            AudioManager.Instance?.SetSFXVolume(value);
        });

        // 초기 슬라이더 값.. 사운드 매니저에서 관리 해야할까요?
        sliderMaster.value = 1f;
        sliderBGM.value = 0.5f;
        sliderSFX.value = 0.5f;

        //화면 해상도 얻기
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

    private void SetActiveButton(int selectedIndex)
    {
        Color selectedColor = Color.white; // 선택됐을 때 그대로 흰색
        Color unselectedColor = new Color(1f, 0.976f, 0.231f); // fff93b

        for (int i = 0; i < menuButtons.Count; i++)
        {
            var colors = menuButtons[i].colors;
            if (i == selectedIndex)
            {
                colors.normalColor = selectedColor;
                colors.disabledColor = selectedColor;
            }
            else
            {
                colors.normalColor = unselectedColor;
                colors.disabledColor = unselectedColor;
            }
            menuButtons[i].colors = colors;
        }
    }

    public void ShowPanel(int index)
    {
        if (index < 0 || index >= rightPanels.Count)
        {
            Debug.LogWarning("ShowPanel: index out of range");
            return;
        }

        // 오른쪽 패널 모두 비활성화 후, 선택된 것만 활성화
        for (int i = 0; i < rightPanels.Count; i++)
        {
            rightPanels[i].SetActive(i == index);
        }

        switch (index)
        {
            //Sound
            case 0:
                
                break;
            //Display
            case 1:
                break;
            //Achievement
            case 2:
                
                break;
        }
        SetActiveButton(index);

        //  디버그용
        Debug.Log("Display 패널 활성 상태: " + rightPanels[1].activeSelf);
        Debug.Log("Dropdown 활성 상태: " + resolutionDropdown.gameObject.activeSelf);
        Debug.Log("Dropdown Template 활성 상태: " + resolutionDropdown.template.gameObject.activeSelf);
        Debug.Log("Dropdown 옵션 개수: " + resolutionDropdown.options.Count);

    }

    public void MoveStartScene()
    {
        TogglePausePanel();
        SceneManager.LoadScene("StartScene");
    }
    void FilterResolutions()
    {
        //  해상도 전부 가져오기
        resolutions = Screen.resolutions;

        ////  해상도 필터링
        //List<Resolution> filtered = new List<Resolution>();

        //foreach (var r in Screen.resolutions)
        //{
        //    float hz = (float)r.refreshRateRatio.value;
        //    bool is60Hz = Mathf.Abs(hz - 60f) < 0.5f;
        //    bool is16by9 = (r.width * 9 == r.height * 16);
        //    bool isLargeEnough = (r.width >= 1280);

        //    if (is60Hz && is16by9 && isLargeEnough)
        //    {
        //        filtered.Add(r);
        //    }
        //}
        //resolutions = filtered.ToArray();

        //  디버그용    > 아 제 모니터는 해상도 74HZ라 안됨 아ㅏ
        Debug.Log($"FilterResolutions: {resolutions.Length}개 필터링됨");
        foreach (var res in resolutions)
        {
            Debug.Log($"{res.width} x {res.height} @ {(float)res.refreshRateRatio.value}Hz");
        }
    }

    void SetUpDropdown()
    {
        resolutionDropdown.ClearOptions(); // 초기화

        HashSet<string> options = new HashSet<string>();

        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " X " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(new List<string>(options)); // 선택지 등록
        resolutionDropdown.value = currentResolutionIndex; // 현재 해상도 표시
        resolutionDropdown.RefreshShownValue(); // 값 갱신

    }
    void SetUpToggles()
    {
        // 기존 리스너 제거 (중복 방지)
        toggleFull.onValueChanged.RemoveAllListeners();
        toggleWindow.onValueChanged.RemoveAllListeners();

        // 현재 실제 상태 읽기
        bool isFullScreen = Screen.fullScreen;

        // 현재 화면 모드 상태 반영
        if (isFullScreen)
        {
            toggleFull.isOn = true;
            toggleWindow.isOn = false;
            activatedToggle = toggleFull;
            screenMode = ScreenMode.Full;
        }
    else
        {
            toggleFull.isOn = false;
            toggleWindow.isOn = true;
            activatedToggle = toggleWindow;
            screenMode = ScreenMode.Window;
        }

        // 리스너 다시 추가
        toggleFull.onValueChanged.AddListener(delegate { ToggleChanged(toggleFull); });
        toggleWindow.onValueChanged.AddListener(delegate { ToggleChanged(toggleWindow); });
    }
    public void SetResolution()
    {
        int resolutionIndex = resolutionDropdown.value;
        Resolution resolution = resolutions[resolutionIndex];

        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);

        Debug.Log(Screen.width + " X " + Screen.height);
    }

    void ToggleChanged(Toggle changedToggle)
    {
        if (changedToggle.isOn)
        {
            activatedToggle = changedToggle;

            // 전체 화면 모드 on
            if (changedToggle == toggleFull)
            {
                // 창 모드 off
                toggleWindow.isOn = false;
                screenMode = ScreenMode.Full;
            }

            // 창 모드 on
            else
            {
                // 전체 화면 모드 off
                toggleFull.isOn = false;
                screenMode = ScreenMode.Window;
            }
        }

        // changedToggle을 두 번 클릭한 경우 해제 방지하기
        else
        {
            // 이미 활성화된 토글을 다시 클릭해서 끄려는 경우, 다시 켜기
            if (activatedToggle == changedToggle)
            {
                activatedToggle.isOn = true;
            }
        }

        SetScreenMode();
    }
    void SetScreenMode()
    {
        if (screenMode == ScreenMode.Full)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
        }
    }

    public void ApplyBtnClick()
    {
        Resolution selectedResolution = resolutions[resolutionDropdown.value];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
        Debug.Log($"해상도 적용됨: {selectedResolution.width} x {selectedResolution.height}");
    }


}

