using DG.Tweening;
using SaveData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ReceiptLineManager : MonoBehaviour
{
    public static ReceiptLineManager Instance { get; private set; }

    public GameObject receiptPrefab;   // 영수증 UI 프리팹
    public Transform receiptLineParent; // 영수증들이 매달릴 부모 (줄)

    //private List<ReceiptLineItem> activeReceipts = new();
    private List<Receipt> missedReceipts = new List<Receipt>();     // 놓친 영수증 저장해둘 리스트
    public List<Receipt> GetMissedReceipts()
    {
        //Debug.Log($"실패한 영수증 개수: {missedReceipts.Count}");
        return new List<Receipt>(missedReceipts); // 반환값 추가
    }
    public void ClearMissedReceipts() => missedReceipts.Clear();

    private List<Receipt> successfulReceipts = new();
    public List<Receipt> GetSuccessfulReceipts() => new(successfulReceipts);
    public void ClearSuccessfulReceipts() => successfulReceipts.Clear();

    public float cookLimitMinutes = 30f;

    public ReceiptPopup receiptPopup;
    public CombinedIngredientManager combinedIngredientManager;

    public int maxSlots = 15;
    private List<ReceiptLineItem> receiptSlots = new();  // 고정된 순서 유지
    public List<ReceiptLineItem> GetReceiptSlots() => new(receiptSlots); // 슬롯 리스트 반환

    [Header("Grid Layout Settings")]
    public int gridColumns = 3;      // ✨ 사용자가 말한 대로 3으로 설정 (Inspector 확인 필수)
    public float slotSpacingX = 160f;
    public float slotSpacingY = 200f;
    public Vector2 startOffset = new Vector2(50f, -50f);

    private void Update()
    {
        // 키보드가 연결되어 있는지 확인 (안전장치)
        if (Keyboard.current == null) return;

        if (GameClock.isPaused) return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            NavigateReceipts(1, 0);
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            NavigateReceipts(-1, 0);
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            NavigateReceipts(0, 1);   // 위로 (행 감소)
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            NavigateReceipts(0, -1);  // 아래로 (행 증가)

        // (선택사항) 엔터키로 선택하기 기능 추가
        // if (Keyboard.current.enterKey.wasPressedThisFrame) SelectCurrentHovered(); 
    }

    private void NavigateReceipts(int xDir, int yDir)
    {
        // 1. 현재 선택된 영수증이 있는지 확인
        var activeReceipt = ReceiptStateManager.Instance.ActiveReceipt;
        int currentIndex = -1;

        // 현재 활성화된 영수증의 슬롯 인덱스 찾기
        if (activeReceipt != null)
        {
            for (int i = 0; i < receiptSlots.Count; i++)
            {
                if (receiptSlots[i] != null && receiptSlots[i].GetReceipt() == activeReceipt)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        // 2. 만약 선택된 게 없다면? -> 방향키 누르면 0번(첫 번째) 자동 선택
        if (currentIndex == -1)
        {
            if (receiptSlots.Count > 0 && receiptSlots[0] != null)
            {
                SelectReceiptAtIndex(0);
            }
            return;
        }

        // 3. 다음 인덱스 계산 (그리드 논리 적용)
        int targetIndex = currentIndex;

        // 좌우 이동 (행을 넘어가지 않도록 막음)
        if (xDir != 0)
        {
            int currentRow = currentIndex / gridColumns;
            int targetColIndex = targetIndex + xDir;

            // 같은 행 안에서만 이동 가능하게 체크
            if (targetColIndex >= 0 && targetColIndex < receiptSlots.Count &&
                (targetColIndex / gridColumns) == currentRow)
            {
                targetIndex = targetColIndex;
            }
        }

        // 상하 이동 (열을 유지한 채 인덱스만 +/- Col)
        // yDir: 1이면 위(인덱스 감소), -1이면 아래(인덱스 증가) 
        // (보통 UI 좌표계와 리스트 인덱스는 반대라 헷갈릴 수 있음. 여기선 '위'가 0번 인덱스 쪽이라고 가정)
        if (yDir != 0)
        {
            // 위쪽 화살표(1) -> 인덱스 감소 (-3)
            // 아래쪽 화살표(-1) -> 인덱스 증가 (+3)
            int indexStep = (yDir > 0) ? -gridColumns : gridColumns;
            int tempIndex = currentIndex + indexStep;

            if (tempIndex >= 0 && tempIndex < receiptSlots.Count)
            {
                targetIndex = tempIndex;
            }
        }

        // 4. 유효한 슬롯이면 선택 실행
        if (targetIndex != currentIndex && receiptSlots[targetIndex] != null)
        {
            SelectReceiptAtIndex(targetIndex);
        }
    }

    // ✨ [NEW] 인덱스로 영수증 선택 (마우스 클릭과 동일한 효과)
    public void SelectReceiptAtIndex(int index)
    {
        if (index < 0 || index >= receiptSlots.Count) return;
        var item = receiptSlots[index];
        if (item == null) return;

        // 아이템 내부의 OnClick 로직을 그대로 수행하거나, 직접 매니저를 부름
        // 여기서는 ReceiptLineItem의 OnClick 내용을 모방해서 직접 실행
        var receipt = item.GetReceipt();

        // 1. 상태 매니저 업데이트 (아웃라인 등)
        ReceiptStateManager.Instance.SetActiveReceipt(receipt);

        // 2. 재료 합산창 표시
        if (combinedIngredientManager != null)
            combinedIngredientManager.DisplayAllCombinedIngredients(receipt);

        // 3. 팝업 표시
        if (receiptPopup != null)
            receiptPopup.Show(receipt);

        // (선택사항) 효과음 재생
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(107); // 틱 소리
    }

    public void ClearAllReceipts()
    {
        foreach (var item in receiptSlots)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        receiptSlots.Clear();
        pendingReceipts.Clear();
        UpdatePendingCountUI();  // 대기중 표시 초기화

        // ✨ [추가] 모든 영수증을 지울 때(다음 날 넘어갈 때), 보여주던 레시피 텍스트도 비웁니다.
        if (combinedIngredientManager != null)
        {
            combinedIngredientManager.ClearIngredientsText();
        }

        // ✨ [추가] 혹시 영수증 팝업이 켜져있다면 닫아줍니다.
        if (receiptPopup != null)
        {
            receiptPopup.Close();
        }
    }
    private Queue<Receipt> pendingReceipts = new();
    public float slotSpacing = 160f;  // 슬롯 간 거리


    public TextMeshProUGUI pendingCountText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 방지
            return;
        }
        Instance = this;
    }

    public void AddNewReceipt(Receipt receipt)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(113);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(117);

        if (receiptSlots.Count >= maxSlots)
        {
            pendingReceipts.Enqueue(receipt);  // 대기열에 보관
            UpdatePendingCountUI();           // 대기중 표시 갱신
            return;
        }

        CreateAndAddReceiptUI(receipt);
    }

    public void RemoveReceiptByOrderID(int orderID)
    {
        var target = receiptSlots.Find(r => r.GetReceipt().OrderID == orderID);
        if (target != null)
        {
            RemoveReceipt(target);
        }
    }

    public void RemoveReceipt(ReceiptLineItem item)
    {
        Receipt receipt = item.GetReceipt();

        //// 모든 메뉴가 조리중이거나 완료된 상태여야 성공해서 삭제된 영수증
        //bool isUncompleted = !StoveManager.AllMenusHandledStatic(receipt);

        //if (isUncompleted)
        //{
        //    missedReceipts.Add(receipt);
        //}

        if (ReceiptStateManager.Instance.ActiveReceipt == receipt)
        {
            ReceiptStateManager.Instance.ClearActiveReceipt();
            receiptPopup.Close(); // 팝업 닫기

            // ✨ [추가] 보고 있던 영수증이 삭제되면 텍스트도 지움
            if (combinedIngredientManager != null)
            {
                combinedIngredientManager.ClearIngredientsText();
            }
        }

        receiptSlots.Remove(item);
        Destroy(item.gameObject);

        StartCoroutine(DelayedReposition());

        if (pendingReceipts.Count > 0)
        {
            var nextReceipt = pendingReceipts.Dequeue();
            CreateAndAddReceiptUI(nextReceipt);
            UpdatePendingCountUI();  // UI 갱신
        }
    }
    public void RecordSuccessfulReceipt(Receipt receipt)
    {
        successfulReceipts.Add(receipt);
        Debug.Log($"[기록] 성공 영수증: {receipt.OrderID}");
    }

    public void RecordFailedReceipt(Receipt receipt)
    {
        if (missedReceipts.Contains(receipt)) return;
        missedReceipts.Add(receipt);
        Debug.Log($"[기록] 실패 영수증: {receipt.OrderID}");
    }

    private IEnumerator DelayedReposition()
    {
        yield return null;  // 한 프레임 대기
        RepositionAll();    // 이후 정확한 위치로 수동 정렬
    }

    private void CreateAndAddReceiptUI(Receipt receipt)
    {
        var obj = Instantiate(receiptPrefab, receiptLineParent);
        var lineItem = obj.GetComponent<ReceiptLineItem>();
        lineItem.Setup(receipt, cookLimitMinutes, this, receiptPopup, combinedIngredientManager);

        receiptSlots.Add(lineItem);
        RepositionAll();
    }


    private void UpdatePendingCountUI()
    {
        int count = pendingReceipts.Count;
        pendingCountText.text = count > 0 ? $"대기 중: {count}건" : "";
    }

    public Vector3 GetGridPosition(int index)
    {
        int col = index % gridColumns;
        int row = index / gridColumns;

        float xPos = startOffset.x + (col * slotSpacingX);
        float yPos = startOffset.y - (row * slotSpacingY);

        return new Vector3(xPos, yPos, 0f);
    }

    public void RepositionAll()
    {
        int count = receiptSlots.Count;

        for (int i = 0; i < count; i++)
        {
            var item = receiptSlots[i];
            if (item == null || item.gameObject == null) continue;

            // 인덱스 갱신은 무조건 수행
            item.CurrentSlotIndex = i;

            // 드래그 중인 아이템은 물리적 이동(Tween)에서 제외
            if (item.IsBeingDragged) continue;

            RectTransform rt = item.GetComponent<RectTransform>();
            Vector3 targetPosition = GetGridPosition(i); // 이전 답변에서 만든 함수

            // ✨ 핵심: 현재 진행 중인 트윈이 있다면 즉시 중단하고(Kill) 새로운 목적지로 보냄
            // 이렇게 해야 "이전 위치로 가려던 관성"이 사라지고 "최신 위치"로 즉시 턴합니다.
            rt.DOKill();
            rt.DOAnchorPos(targetPosition, 0.3f).SetEase(Ease.OutCubic);
        }
    }

    public string GetTodaySuccessfulReceiptsText()
    {
        DateTime today = GameClock.gameTime.Date;
        if (successfulReceipts.Count == 0)
            return $"[{today:yyyy-MM-dd}] 성공한 영수증이 없습니다.\n";

        string result = $"=== [성공한 영수증] {today:yyyy-MM-dd} ===\n\n";
        int totalBase = 0;

        foreach (var receipt in successfulReceipts)
        {
            result += receipt.GetReceiptText() + "\n";
            totalBase += receipt.GetTotalPrice();
        }

        result += "--------------------------------\n";
        result += $"주문 기본 합계: {totalBase:N0}원\n";

        // ✨ [NEW] 보너스 금액 표시 추가
        int bonus = DailyBonusManager.Instance.TodayAccumulatedBonus;
        if (bonus > 0)
        {
            result += $"<color=#D95400>+ 추가 보너스 수익: {bonus:N0}원</color>\n";
        }

        result += $"================================\n";
        result += $"총 최종 수익: {totalBase + bonus:N0}원\n"; // 최종 합계

        return result;
    }

    public string GetTodayMissedReceiptsText()  // 오늘 날짜의 실패한 영수증 텍스트로 가져오기
    {
        DateTime today = GameClock.gameTime.Date;
        if (missedReceipts.Count == 0)
            return $"[{today:yyyy-MM-dd}] 실패한 영수증이 없습니다.\n";

        string result = $"=== [실패한 영수증] {today:yyyy-MM-dd} ===\n\n";
        int total = 0;

        foreach (var receipt in missedReceipts)
        {
            result += receipt.GetReceiptText() + "\n";
            total += receipt.GetTotalPrice();
        }

        result += $"총 손실 금액: {total:N0}원\n";
        return result;
    }

    // [NEW] 총 성공 금액 반환 함수 (UI가 리스트를 몰라도 됨)
    public int GetTotalSuccessfulAmount()
    {
        return successfulReceipts.Sum(r => r.GetTotalPrice());
    }

    // [NEW] 총 실패(손실) 금액 반환 함수
    public int GetTotalMissedAmount()
    {
        return missedReceipts.Sum(r => r.GetTotalPrice());
    }

    public void RestoreMissed(List<Receipt> list)
    {
        foreach (var r in list)
            missedReceipts.Add(r);
    }

    public void RestoreSuccessful(List<Receipt> list)
    {
        foreach (var r in list)
            successfulReceipts.Add(r);
    }
    public List<ReceiptSlotSaveData> GetCurrentReceiptSlots()
    {
        var result = new List<ReceiptSlotSaveData>();

        for (int i = 0; i < receiptSlots.Count; i++)
        {
            var receiptItem = receiptSlots[i];

            if (receiptItem != null && receiptItem.GetReceipt() != null)
            {
                var data = new ReceiptSlotSaveData
                {
                    slotIndex = i,
                    receiptData = ReceiptSystem.ToData(receiptItem.GetReceipt()),
                    remainingTime = receiptItem.GetRemainingTime(),
                    cookLimitTime = receiptItem.GetLimitTime()
                };

                result.Add(data);
            }
        }

        return result;
    }
    public void RestoreReceiptSlots(List<ReceiptSlotSaveData> savedSlots)
    {
        // 1. 기존 슬롯 초기화
        ClearAllReceipts();

        foreach (var slotData in savedSlots)
        {
            // 2. ReceiptData → Receipt 복원
            Receipt restoredReceipt = ReceiptSystem.FromData(slotData.receiptData);

            // 3. 복원된 Receipt를 지정된 슬롯에 추가
            AddNewReceipt(restoredReceipt, slotData.cookLimitTime, slotData.slotIndex);

            // 4. 남은 시간 덮어쓰기 (타이머 복원)
            var receiptItem = receiptSlots[slotData.slotIndex];
            receiptItem.OverrideRemainingTime(slotData.remainingTime);
        }

        RepositionAll();
    }
    public void AddNewReceipt(Receipt receipt, float cookTime, int slotIndex)
    {
        while (receiptSlots.Count <= slotIndex)
        {
            receiptSlots.Add(null); // 빈 슬롯 추가
        }
        if (receiptSlots[slotIndex] != null)
        {
            Destroy(receiptSlots[slotIndex].gameObject);
        }
        GameObject go = Instantiate(receiptPrefab, receiptLineParent);
        ReceiptLineItem item = go.GetComponent<ReceiptLineItem>();
        item.Setup(receipt, cookTime, this, receiptPopup, combinedIngredientManager);
        receiptSlots[slotIndex] = item;
    }

    public List<ReceiptData> GetPendingReceiptsData()
    {
        return ReceiptSystem.ConvertToDataList(pendingReceipts.ToList());
    }
    public void RestorePendingReceipts(List<ReceiptData> dataList)
    {
        pendingReceipts.Clear();

        foreach (var data in dataList)
        {
            Receipt r = ReceiptSystem.FromData(data);
            pendingReceipts.Enqueue(r);
        }

        UpdatePendingCountUI(); // 대기 중 숫자 UI 업데이트
    }
    // ✨ [NEW] 마감 시 남아있는 모든 영수증을 실패 처리하는 함수
    public void FailAllActiveReceipts()
    {
        // 1. 화면에 매달려 있는 영수증들 실패 처리
        foreach (var item in receiptSlots)
        {
            if (item != null)
            {
                RecordFailedReceipt(item.GetReceipt());
            }
        }

        // 2. 대기열(아직 화면에 안 나온)에 있는 영수증들 실패 처리
        foreach (var receipt in pendingReceipts)
        {
            RecordFailedReceipt(receipt);
        }

        Debug.Log("[마감] 남아있던 모든 영수증을 실패 처리했습니다.");
    }

    public void UpdateSelectionOutlines(Receipt activeReceipt)
    {
        foreach (var item in receiptSlots)
        {
            if (item == null) continue;

            // 내 영수증이 activeReceipt와 같은지 비교
            bool isSelected = (item.GetReceipt() == activeReceipt);

            // 상태 적용
            item.SetHighlight(isSelected);
        }
    }
}
