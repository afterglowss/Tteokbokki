using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;
using Unity.VisualScripting;

public class ReceiptLineItem : MonoBehaviour
{
    public Button receiptButton;
    public TextMeshProUGUI orderIDText;
    public float cookTimeSeconds;

    private Receipt receipt;
    private ReceiptLineManager manager;
    private ReceiptPopup receiptPopup;
    private CombinedIngredientManager combinedIngredientManager;

    private DateTime orderStartTime;

    private Vector3 originalPosition;
    private Transform originalParent;

    private int originalSiblingIndex;

    public int CurrentSlotIndex { get; set; }  // 리스트 상 자신의 위치 인덱스

    public bool IsBeingDragged { get; private set; }

    private bool isTweening = false;

    public void CachePosition()
    {
        originalPosition = GetComponent<RectTransform>().anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
    }

    public void ReturnToOriginalPosition(float duration = 0.25f)
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        isTweening = true;
        GetComponent<RectTransform>().DOAnchorPos(originalPosition, duration).SetEase(Ease.OutCubic)
            .OnComplete(() => isTweening = false);
    }

    // 드래그 시작 시 호출
    public void OnBeginDrag()
    {
        if (isTweening) return;  // 이미 트윈이 진행 중이면 무시

        GetComponent<RectTransform>().DOComplete();

        IsBeingDragged = true;
        CachePosition();
    }
    public void OnEndDrag()
    {
        IsBeingDragged = false;
    }

    public void Setup(Receipt receipt, float cookMinutes, ReceiptLineManager manager, ReceiptPopup popup, CombinedIngredientManager ingredientManager)
    {
        this.receipt = receipt;
        this.manager = manager;
        this.receiptPopup = popup;  // 의존성 주입
        this.combinedIngredientManager = ingredientManager;  // 의존성 주입
        cookTimeSeconds = cookMinutes * 60f;
        orderIDText.text = $"주문번호: {receipt.OrderID}";
        orderStartTime = receipt.OrderDateTime;

        CachePosition();    // 드래그 이전 자리 기억

        receiptButton.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        DateTime now = GameClock.gameTime;

        TimeSpan elapsed = now - orderStartTime;

        //Debug.Log($"[영수증 {receipt.OrderID}] 경과 시간: {elapsed.TotalMinutes:F2}분 / 제한: {cookTimeSeconds / 60f}분");

        if (elapsed.TotalMinutes >= cookTimeSeconds / 60f)
        {
            //Debug.LogWarning($"[영수증 {receipt.OrderID}] 시간이 초과되어 삭제됩니다.");
            manager.RemoveReceipt(this);
            return;
        }
    }

    
    private void OnClick()
    {
        if (receiptPopup == null || combinedIngredientManager == null)
        {
            Debug.LogError("ReceiptPopup 또는 CombinedIngredientManager가 연결되지 않았습니다.");
            return;
        }

        ReceiptStateManager.Instance.SetActiveReceipt(receipt);
        combinedIngredientManager.DisplayAllCombinedIngredients(receipt);
        receiptPopup.Show(receipt);
    }
    public Receipt GetReceipt() { return receipt; }

    public Vector3 GetSlotPosition(float spacing)
    {
        return new Vector3(-CurrentSlotIndex * spacing, 0f, 0f);
    }
    public float GetRemainingTime()
    {
        TimeSpan elapsed = GameClock.gameTime - orderStartTime;
        return Mathf.Max(0f, cookTimeSeconds - (float)elapsed.TotalSeconds);
    }

    public float GetLimitTime()
    {
        return cookTimeSeconds;
    }
    public void OverrideRemainingTime(float remaining)
    {
        // 1. 유효성 검사
        if (cookTimeSeconds <= 0f)
        {
            Debug.LogWarning("cookTimeSeconds가 아직 설정되지 않았거나 0입니다.");
            return;
        }

        // 2. 남은 시간 값 클램프
        float clampedRemaining = Mathf.Clamp(remaining, 0f, cookTimeSeconds);

        // 3. 시작 시간 역산하여 설정
        orderStartTime = GameClock.gameTime.AddSeconds(-(cookTimeSeconds - clampedRemaining));

        // 4. 디버깅 로그 (선택)
        // Debug.Log($"제한시간: {cookTimeSeconds}, 남은시간: {clampedRemaining}, 역산된 시작시간: {orderStartTime}");
    }

}
