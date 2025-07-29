using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("임시 텍스트")]
    [SerializeField] private TextMeshProUGUI rightPanelText;

    [Header("메뉴 버튼들")]
    [SerializeField] private Button buttonSound;
    [SerializeField] private Button buttonDisplay;
    [SerializeField] private Button buttonMenu;
    [SerializeField] private Button buttonMap;

    private void Start()
    {
        ShowSound();  // 게임 시작 시 '소리' 탭 자동 활성화
    }

    private void SetActiveButton(Button selected)
    {
        Button[] buttons = { buttonSound, buttonDisplay, buttonMenu, buttonMap };

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
}
