using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Linq;


public class PauseMenuUI : MonoBehaviour
{
    [Header("설정 패널")]
    [SerializeField] private RectTransform panelPause;
    private Button closeButton;

    [Header("임시 텍스트")]
    [SerializeField] private TextMeshProUGUI rightPanelText;

    [Header("메뉴 버튼들")]
    [SerializeField] private Button buttonSound;
    [SerializeField] private Button buttonDisplay;
    [SerializeField] private Button buttonMenu;
    [SerializeField] private Button buttonMap;
    [SerializeField] private Button buttonMoveToStart;

    private bool isPauseOpen = false;

    private void Start()
    {
        // Panel_Pause 안에서 close 버튼을 탐색
        closeButton = panelPause.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(b => b.name == "Button_Close");

        if (closeButton != null)
            Debug.Log("닫기 버튼을 정상적으로 찾았습니다.");
        else
            Debug.Log("닫기 버튼을 찾을 수 없습니다.");

        ShowSound();  // 게임 시작 시 '소리' 탭 자동 활성화
        closeButton.onClick.AddListener(TogglePausePanel);  // 닫기 버튼 연결
        panelPause.anchoredPosition = new Vector2(0, 1000); // 시작 시 화면 위로 숨기기
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
            // 아래로 내려오기
            panelPause.DOAnchorPosY(0, 0.4f).SetEase(Ease.OutCubic); 
        }
        else
        {
            // 위로 숨기기
            panelPause.DOAnchorPosY(1000, 0.4f).SetEase(Ease.InCubic); 
        }
    }

    private void SetActiveButton(Button selected)
    {
        Button[] buttons = { buttonSound, buttonDisplay, buttonMenu, buttonMap, buttonMoveToStart };

        foreach (var btn in buttons)
        {
            var colors = btn.colors;
            if (btn == selected)
            {
                // 선택된 버튼: 원래 색상
                colors.normalColor = Color.white;
                colors.disabledColor = Color.white;
            }
            else
            {
                // 흐리게 (회색으로)
                colors.normalColor = new Color(0.7f, 0.7f, 0.7f);
                colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
            }
            btn.colors = colors;
        }
    }

    public void ShowSound()
    {
        rightPanelText.text = "전체음량\r\n배경음\r\n효과음";
        SetActiveButton(buttonSound);
    }

    public void ShowDisplay()
    {
        rightPanelText.text = "전체화면\r\n창화면\r\n레시피 끄기/켜기";
        SetActiveButton(buttonDisplay);
    }

    public void ShowMenu()
    {
        rightPanelText.text = "마크 발전 과제처럼";
        SetActiveButton(buttonMenu);
    }

    public void ShowMap()
    {
        rightPanelText.text = "주변약도 / 인수할 수 있는 건물 확인\r\n알바 채용 아이콘";
        SetActiveButton(buttonMap);
    }

    public void MoveStartScene()
    {
        //씬 이동 전 처리 필요?
        SetActiveButton(buttonMoveToStart);
        

        SceneManager.LoadScene("StartScene");
    }
}
