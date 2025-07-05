using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    public Vector2 offset = new Vector2(15f, -15f);
    private RectTransform tooltipRect;
    private Canvas canvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 localPoint;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                localPoint = mousePos;
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, mousePos, canvas.worldCamera, out localPoint);
            }

            tooltipRect.anchoredPosition = localPoint + offset;
        }
    }

    public void Show(string content)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipText.text = content;
            tooltipPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
