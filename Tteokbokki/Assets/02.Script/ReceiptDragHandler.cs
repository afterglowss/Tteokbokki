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
        // 원래 위치와 부모 저장
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
                    slot.HandleReceiptDrop(receiptItem);  // 내부에서 manager.RemoveReceipt() 호출
                    return;
                }
            }
        }

        // 드롭 실패 → 다시 원위치 복귀 (리스트 유지!)
        //transform.SetParent(originalParent);
        //rectTransform.DOAnchorPos(originalPosition, 0.25f).SetEase(Ease.OutCubic);

        var receipt = GetComponent<ReceiptLineItem>();
        // 1. 드래그 상태 해제
        if (receipt != null)
        {
            receipt.OnEndDrag(); // IsBeingDragged = false 설정
        }

        // 2. 부모를 원래 리스트(ScrollContent 등)로 복귀
        // (드래그 중에 Canvas 최상단 등으로 옮겼었다면 여기서 원래 부모로 되돌려놔야 함)
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
        }

        // 3. ✨ 스스로 움직이지 말고, 매니저에게 전체 정렬 명령!
        // 이렇게 하면 방금 드롭된 녀석도 RepositionAll 로직에 의해
        // 정확한 Grid 위치(targetPosition)로 부드럽게 이동합니다.
        ReceiptLineManager.Instance.RepositionAll();
    }
}
