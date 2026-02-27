using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class ReceiptLineItem : MonoBehaviour, IPointerClickHandler
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

    // ✨ [NEW] 흔들림 효과 설정
    [Header("Shake Effect")]
    public float shakeThreshold = 15f; // 이 시간(초)부터 흔들리기 시작
    public float shakeStrength = 3f;   // 흔들림 강도
    private bool isShaking = false;    // 현재 흔들리고 있는지 체크
    private Tween shakeTween;          // 트윈 제어용

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

        // ✨ Setup 시 초기화
        isShaking = false;
        if (targetGraphic != null)
        {
            targetGraphic.transform.localPosition = Vector3.zero; // 위치 정렬
            targetGraphic.color = safeColor;
        }
    }

    private void Update()
    {
        // ** 여기 영수증 시간이 안흘러서 테스트 불가. 일단 주석 처리 해둘테니 필요할때 바꾸는 걸로
        //if (TutorialManager.IsFreeze) return;
        DateTime now = GameClock.gameTime;

        TimeSpan elapsed = now - orderStartTime;

        //Debug.Log($"[영수증 {receipt.OrderID}] 경과 시간: {elapsed.TotalMinutes:F2}분 / 제한: {cookTimeSeconds / 60f}분");
        float elapsedSeconds = (float)elapsed.TotalSeconds;
        float remainingSeconds = cookTimeSeconds - elapsedSeconds; // 남은 시간 계산

        // ✨ [수정] 15분 남았을 때부터 색상 변경 로직
        if (targetGraphic != null && cookTimeSeconds > 0)
        {
            // 경고 시작 시간 (15분 = 900초)
            // 만약 전체 제한시간이 15분보다 짧다면, 시작하자마자 붉어지기 시작합니다.
            float warningThreshold = 15f * 60f;

            if (remainingSeconds > warningThreshold)
            {
                // 1. 아직 15분 넘게 남음 -> 안전 색상 유지
                targetGraphic.color = safeColor;
            }
            else
            {
                // 2. 15분 이하로 남음 -> 붉게 변하기 시작
                // 남은 시간이 15분일 때 ratio = 0 (Safe)
                // 남은 시간이 0분일 때 ratio = 1 (Danger)
                float ratio = 1f - (remainingSeconds / warningThreshold);
                ratio = Mathf.Clamp01(ratio); // 0~1 사이로 제한

                targetGraphic.color = Color.Lerp(safeColor, dangerColor, ratio);
            }
        }

        // --- 2. ✨ [NEW] 긴박한 흔들림 효과 (마감 임박!) ---
        // targetGraphic(배경 이미지)만 흔들어야 전체 UI 좌표가 안 꼬입니다.
        if (targetGraphic != null)
        {
            if (remainingSeconds <= shakeThreshold && remainingSeconds > 0)
            {
                if (!isShaking)
                {
                    StartShaking();
                }
            }
            else
            {
                if (isShaking)
                {
                    StopShaking();
                }
            }
        }

        if (elapsed.TotalMinutes >= cookTimeSeconds / 60f)
        {
            // 🔥 [전화] 시간 초과 실패 전화 걸기
            if (PhoneCallManager.Instance != null)
                PhoneCallManager.Instance.TriggerCall(FailReason.Timeout);

            AudioManager.Instance.PlaySFX(118);

            // ✨ [NEW] 타임아웃 로그 기록
            if (GameDataLogger.Instance != null)
            {
                GameDataLogger.Instance.CountFail("Timeout");
            }

            ReceiptLineManager.Instance.RecordFailedReceipt(receipt);
            manager.RemoveReceipt(this);
            return;
        }
    }
    private void StartShaking()
    {
        isShaking = true;
        // targetGraphic만 흔들어야 영수증 슬롯 자체의 정렬(Grid)이 안 망가집니다.
        // LoopType.Yoyo를 써서 계속 흔듭니다.
        shakeTween = targetGraphic.transform.DOShakePosition(1f, shakeStrength, 10, 90, false, true)
            .SetLoops(-1, LoopType.Restart);
    }

    // ✨ 흔들기 멈춤
    private void StopShaking()
    {
        isShaking = false;
        if (shakeTween != null) shakeTween.Kill();

        // 위치 원상복구 (중요)
        if (targetGraphic != null)
            targetGraphic.transform.localPosition = Vector3.zero;
    }

    private void OnDestroy()
    {
        if (shakeTween != null) shakeTween.Kill();
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

    // ✨ [NEW] 마우스 클릭 이벤트를 감지하는 함수 (IPointerClickHandler 구현)
    public void OnPointerClick(PointerEventData eventData)
    {
        // 마우스 우클릭(Right Click)이 감지되었을 때
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CancelReceipt();
        }
    }

    // ✨ [NEW] 휴지통에 드래그했을 때와 완벽히 동일한 동작을 수행하는 함수
    private void CancelReceipt()
    {
        // 1. 휴지통 버리는 효과음 재생 (기존 TrashBinSlot과 동일)
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(114);

        // 2. 데이터 로그에 '휴지통(Trash)' 실패로 기록
        if (GameDataLogger.Instance != null)
        {
            GameDataLogger.Instance.CountFail("Trash");
        }

        // 3. 취소 내역에 기록 (어제 우리가 만든 Canceled 리스트에 쏙 들어갑니다!)
        if (ReceiptLineManager.Instance != null)
        {
            ReceiptLineManager.Instance.RecordCanceledReceipt(receipt);

            // 4. 영수증 목록에서 제거 및 UI 파괴
            ReceiptLineManager.Instance.RemoveReceipt(this);
        }

        Debug.Log($"[시스템] 우클릭으로 영수증({receipt.OrderID})을 휴지통에 버렸습니다.");
    }
}
