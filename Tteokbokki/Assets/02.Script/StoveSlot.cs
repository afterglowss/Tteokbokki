using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class StoveSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI timerText;
    public GameObject wokIcon;

    private float cookTimeSeconds;
    private float cookTimeRemaining;
    private bool isCooking = false;
    private bool isCooked = false;  // 조리 완료 상태

    private Action<OrderItem> onCookComplete;
    private OrderItem currentOrderItem;

    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    public bool isPointerOver;  // 마우스가 올라가 있는지
    public Vector2 tooltipOffset = new Vector2(15f, -15f);

    public bool IsAvailable => !isCooking && currentOrderItem == null;

    public void StartCooking(OrderItem order, float cookTime, Action<OrderItem> onComplete)
    {
        currentOrderItem = order;
        cookTimeSeconds = cookTime;
        cookTimeRemaining = cookTime;
        onCookComplete = onComplete;

        order.PlaceOnStove();
        isCooking = true;
        isCooked = false;  // 조리 완료 초기화

        wokIcon.SetActive(true);
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            UpdateTooltipPosition();
        }

        if (!isCooking) return;

        cookTimeRemaining -= Time.deltaTime * (60f / 3f);
        if (cookTimeRemaining <= 0)
        {
            FinishCooking();
        }
        else
        {
            UpdateTimerDisplay();
        }
    }

    private void FinishCooking()
    {
        isCooking = false;
        isCooked = true;  // 조리 완료 플래그
        timerText.text = "완료!";
        currentOrderItem.MarkAsCompleted();

        // 🔔 마우스가 이미 올라와있다면 툴팁 갱신
        if (isPointerOver)
        {
            UpdateTooltipText();
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(cookTimeRemaining / 60);
        int seconds = Mathf.FloorToInt(cookTimeRemaining % 60);
        timerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public void OnClick()
    {
        if (!isCooking && currentOrderItem != null)
        {
            wokIcon.SetActive(false);

            var completedOrder = currentOrderItem;
            currentOrderItem = null;
            timerText.text = "대기 중";

            isCooked = false;  // 완료 후 초기화

            onCookComplete?.Invoke(completedOrder);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        if (currentOrderItem != null)
        {
            ShowTooltip();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        HideTooltip();
    }

    private void ShowTooltip()
    {
        UpdateTooltipText();
        tooltipPanel.SetActive(true);
        UpdateTooltipPosition();
    }

    private void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void UpdateTooltipText()
    {
        if (currentOrderItem == null) return;

        if (isCooking)
        {
            tooltipText.text = $"[{currentOrderItem.ItemID}] {currentOrderItem.Menu.Name}\n조리 중...";
        }
        else if (isCooked)
        {
            tooltipText.text = $"[{currentOrderItem.ItemID}] {currentOrderItem.Menu.Name}\n조리 완료 (완료 버튼을 눌러주세요)";
        }
        else
        {
            tooltipText.text = $"[{currentOrderItem.ItemID}] {currentOrderItem.Menu.Name}\n대기 중";
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipPanel == null) return;

        var canvas = tooltipPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 localPoint;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            localPoint = mousePosition;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePosition, canvas.worldCamera, out localPoint);
        }

        tooltipRect.anchoredPosition = localPoint + tooltipOffset;
    }
}
