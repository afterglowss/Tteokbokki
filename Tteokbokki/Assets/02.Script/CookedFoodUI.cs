using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CookedFoodUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("재료 텍스트")]
    public TextMeshProUGUI ingredientsText;

    [Header("툴팁 관련")]
    public GameObject tooltipPanel;               // 툴팁 패널 (Canvas 상의 별도 UI)
    public TextMeshProUGUI tooltipText;           // 툴팁 내용
    public Vector2 tooltipOffset = new Vector2(15f, -15f);

    private bool isTooltipVisible = false;

    public Dictionary<string, int> Ingredients { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    private Canvas canvas;
    private RectTransform tooltipRect;

    public PackagingSlot currentSlot;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        if (tooltipPanel != null)
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
    }
    private void Update()
    {
        if (isTooltipVisible)
        {
            UpdateTooltipPosition();
        }
    }

    public void Initialize(Dictionary<string, int> ingredients)
    {
        Ingredients = new Dictionary<string, int>(ingredients);
        UpdateText();
    }
    public void SetTooltipReferences(GameObject panel, TextMeshProUGUI text)
    {
        tooltipPanel = panel;
        tooltipText = text;
        if (tooltipPanel != null)
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
    }

    private void UpdateText()
    {
        string result = "";
        foreach (var kv in Ingredients)
        {
            result += $"{kv.Key} x{kv.Value}\n";
        }

        if (ingredientsText != null)
            ingredientsText.text = result;
    }

    private void ShowTooltip()
    {
        if (tooltipPanel == null || tooltipText == null) return;

        UpdateTooltipText();
        tooltipPanel.SetActive(true);
        isTooltipVisible = true;
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        isTooltipVisible = false;
    }

    private void UpdateTooltipText()
    {
        if (tooltipText == null) return;

        string result = "재료:\n";
        foreach (var kv in Ingredients)
        {
            result += $"{kv.Key} x{kv.Value}\n";
        }

        tooltipText.text = result;
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRect == null || canvas == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 localPoint;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            localPoint = mousePosition;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, mousePosition, canvas.worldCamera, out localPoint);
        }

        tooltipRect.anchoredPosition = localPoint + tooltipOffset;
    }

    // 마우스 올라갔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot != null && !currentSlot.IsTopOfStack(this))
        {
            Debug.Log("스택의 맨 위가 아님 → 드래그 불가");
            eventData.pointerDrag = null;  // 드래그 중단
            return;
        }

        // 드래그 허용
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateTooltipPosition();
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("OnEndDrag 호출");
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
}
