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

        // Raycast�� ���� ī�带 ã��
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
                    slot.OnDrop(eventData);  // ���Կ��� ������ ����
                    Destroy(gameObject);     // ������ UI ����
                    return;
                }
            }
        }

        // ���� ī�尡 �ƴ� �� �� ����
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}
