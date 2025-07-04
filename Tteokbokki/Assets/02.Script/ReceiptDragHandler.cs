using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

        var receipt = GetComponent<ReceiptLineItem>();
        receipt?.OnBeginDrag();
        // ���� ��ġ�� �θ� ����
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

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var foodUI = result.gameObject.GetComponentInParent<CookedFoodUI>();
            if (foodUI != null)
            {
                var slot = foodUI.GetComponentInParent<PackagingSlot>();
                if (slot != null && slot.HasAnyFood())
                {
                    var receiptItem = GetComponent<ReceiptLineItem>();
                    slot.HandleReceiptDrop(receiptItem);  // ���ο��� manager.RemoveReceipt() ȣ��
                    return;
                }
            }
        }

        // ��� ���� �� �ٽ� ����ġ ���� (����Ʈ ����!)
        //transform.SetParent(originalParent);
        //rectTransform.DOAnchorPos(originalPosition, 0.25f).SetEase(Ease.OutCubic);

        var receipt = GetComponent<ReceiptLineItem>();
        receipt?.OnEndDrag();
        if (receipt != null)
        {
            receipt.ReturnToOriginalPosition();  // ���� ��ġ�� ����
        }
    }
}
