using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ReceiptDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private Vector3 originalPosition;
    private Transform originalParent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Raycast로 음식 카드를 찾기
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var foodUI = result.gameObject.GetComponentInParent<CookedFoodUI>();
            if (foodUI != null)
            {
                var slot = foodUI.GetComponentInParent<PackagingSlot>();
                if (slot != null)
                {
                    slot.OnDrop(eventData);  // 슬롯에게 영수증 전달
                    Destroy(gameObject);     // 영수증 UI 제거
                    return;
                }
            }
        }

        // 음식 카드가 아닌 곳 → 복귀
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}
