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

    [Header("Highlight")]
    public Outline selectionOutline;

    [Header("Time Visuals")]
    // ✨ [NEW] 색상이 변할 배경 이미지 (Inspector에서 연결)
    public Image targetGraphic;

    // ✨ [NEW] 시작할 때 색상 (평화로움)
    public Color safeColor = Color.white;

    // ✨ [NEW] 시간이 다 됐을 때 색상 (위험!)
    public Color dangerColor = new Color(1f, 0.4f, 0.4f); // 연한 빨강

    public void CachePosition()
    {
        originalPosition = GetComponent<RectTransform>().anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
    }

    public void ReturnToOriginalPosition(float duration = 0.25f)
    {
        // 이 함수는 이제 호출되지 않거나, 호출되어도 매니저에게 위임해야 합니다.
        // 여기서는 삭제하거나 비워두는 것을 추천하지만, 
        // 외부 호출 의존성이 있다면 아래처럼 매니저를 부르게 변경하세요.
        ReceiptLineManager.Instance.RepositionAll();
    }

    // 드래그 시작 시 호출
    public void OnBeginDrag()
    {
        if (isTweening) return;

        GetComponent<RectTransform>().DOComplete();
        IsBeingDragged = true;

        // CachePosition(); // 굳이 좌표를 기억할 필요가 없어짐

        // 드래그 시작 시 부모 변경 (맨 앞으로 가져오기 위해)
        originalParent = transform.parent;
        // originalSiblingIndex = transform.GetSiblingIndex(); // 필요하다면 유지
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
        orderIDText.text = $"{receipt.OrderID}";
        orderStartTime = receipt.OrderDateTime;

        CachePosition();    // 드래그 이전 자리 기억

        receiptButton.onClick.AddListener(OnClick);
        // ✨ [NEW] 시작 시 색상 초기화
        if (targetGraphic != null) targetGraphic.color = safeColor;
    }

    private void Update()
    {
        // ** 여기 영수증 시간이 안흘러서 테스트 불가. 일단 주석 처리 해둘테니 필요할때 바꾸는 걸로
        //if (TutorialManager.IsFreeze) return;
        DateTime now = GameClock.gameTime;

        TimeSpan elapsed = now - orderStartTime;

        //Debug.Log($"[영수증 {receipt.OrderID}] 경과 시간: {elapsed.TotalMinutes:F2}분 / 제한: {cookTimeSeconds / 60f}분");
        float elapsedSeconds = (float)elapsed.TotalSeconds;
        // ✨ [NEW] 색상 변경 로직
        if (targetGraphic != null && cookTimeSeconds > 0)
        {
            // 진행률 (0 ~ 1) 계산
            float ratio = elapsedSeconds / cookTimeSeconds;

            // 색상 보간 (Safe -> Danger로 서서히 이동)
            targetGraphic.color = Color.Lerp(safeColor, dangerColor, ratio);
        }

        if (elapsed.TotalMinutes >= cookTimeSeconds / 60f)
        {
            // 🔥 [전화] 시간 초과 실패 전화 걸기
            if (PhoneCallManager.Instance != null)
                PhoneCallManager.Instance.TriggerCall(FailReason.Timeout);

            AudioManager.Instance.PlaySFX(118);

            ReceiptLineManager.Instance.RecordFailedReceipt(receipt);
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

    public Vector3 GetSlotPosition()
    {
        var mgr = ReceiptLineManager.Instance;
        int col = CurrentSlotIndex % mgr.gridColumns;
        int row = CurrentSlotIndex / mgr.gridColumns;

        float x = mgr.startOffset.x + (col * mgr.slotSpacingX);
        float y = mgr.startOffset.y - (row * mgr.slotSpacingY);

        return new Vector3(x, y, 0f);
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
        //Debug.Log($"제한시간: {cookTimeSeconds}, 남은시간: {clampedRemaining}, 역산된 시작시간: {orderStartTime}");
    }

    public void SetHighlight(bool isActive)
    {
        if (selectionOutline != null)
        {
            selectionOutline.enabled = isActive;

            // (선택 사항) 색상도 코드에서 제어하고 싶다면:
            // if (isActive) selectionOutline.effectColor = Color.yellow;
        }
    }

}
