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
    [SerializeField] private Button buttonFullscreen;
    [SerializeField] private Button buttonWindowed;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] public Button applyButton;

    private List<Resolution> resolutions = new List<Resolution>();
    private int resolutionNum = 0;

    private bool isPauseOpen = false;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderBGM;
    [SerializeField] private Slider sliderSFX;

    private void Start()
    {
        closeButton.onClick.AddListener(TogglePausePanel);
        applyButton.onClick.AddListener(ApplyBtnClick);
        buttonFullscreen.onClick.AddListener(() => SetFullscreen(true));
        buttonWindowed.onClick.AddListener(() => SetFullscreen(false));

        panelPause.anchoredPosition = new Vector2(0, 1530); // 시작 시 숨기기

        // 기본 선택: 소리 탭
        ShowPanel(0);
        // 각 메뉴 버튼에 클릭 리스너 등록
        for (int i = 0; i < menuButtons.Count; i++)
        {
            int index = i;
            menuButtons[i].onClick.AddListener(() => ShowPanel(index));
        }

        // 지원하는 해상도 목록 필터링
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            if (Screen.resolutions[i].refreshRate == 60 && (Screen.resolutions[i].width * 9 == Screen.resolutions[i].height * 16) && Screen.resolutions[i].width >= 1280)
            {
                resolutions.Add(Screen.resolutions[i]);
            }
        }
        // 현재 해상도 인덱스 찾기
        int currentResIndex = 0;
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
                break;
            }
        }
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionNum = currentResIndex;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        foreach (var res in resolutions)
        {
            options.Add($"{res.width} x {res.height}");
        }

        // TMP_Dropdown은 TMP_Dropdown.OptionData 사용
        List<TMP_Dropdown.OptionData> optionDataList = new List<TMP_Dropdown.OptionData>();
        foreach (string opt in options)
        {
            optionDataList.Add(new TMP_Dropdown.OptionData(opt));
        }

        resolutionDropdown.AddOptions(optionDataList);
        // Dropdown 옵션 선택시 변경
        resolutionDropdown.onValueChanged.AddListener(DropdownOptionChange);

        // 초기 화면 상태(전체/창)
        SetFullscreen(Screen.fullScreen);

        //사운드 슬라이더 연결
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

        // 초기값 설정
        sliderMaster.value = 1f;
        sliderBGM.value = 0.5f;
        sliderSFX.value = 0.5f;
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
        Color unselectedColor = new Color(1f, 0.976f, 0.231f, 0.3f); // fff93b + 낮은 투명도 (0.3f)

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

        SetActiveButton(index);
    }

    public void MoveStartScene()
    {
        TogglePausePanel();
        SceneManager.LoadScene("StartScene");
    }
    public void DropdownOptionChange(int index)
    {
        resolutionNum = index;
    }
    public void ApplyBtnClick()
    {
        Screen.SetResolution(resolutions[resolutionNum].width, resolutions[resolutionNum].height, Screen.fullScreen);
    }

    private void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;

        var fullImg = buttonFullscreen.GetComponent<Image>();
        var windowImg = buttonWindowed.GetComponent<Image>();

        Color selectedColor = Color.black;
        Color unselectedColor = new Color(0.68f, 0.68f, 0.68f); // #AEAEAE

        if (fullscreen)
        {
            fullImg.color = selectedColor;
            windowImg.color = unselectedColor;
        }
        else
        {
            fullImg.color = unselectedColor;
            windowImg.color = selectedColor;
        }
    }

}

