using DG.Tweening;
using SaveData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ReceiptLineManager : MonoBehaviour
{
    public static ReceiptLineManager Instance { get; private set; }

    public GameObject receiptPrefab;   // 영수증 UI 프리팹
    public Transform receiptLineParent; // 영수증들이 매달릴 부모 (줄)

    //private List<ReceiptLineItem> activeReceipts = new();
    private List<Receipt> missedReceipts = new List<Receipt>();     // 놓친 영수증 저장해둘 리스트
    public List<Receipt> GetMissedReceipts()
    {
        Debug.Log($"실패한 영수증 개수: {missedReceipts.Count}");
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
    public int gridColumns = 5;      // 한 줄에 몇 개의 영수증을 둘 것인가?
    public float slotSpacingX = 160f; // 가로 간격 (기존 slotSpacing 대체)
    public float slotSpacingY = 200f; // 세로 간격
    public Vector2 startOffset = new Vector2(50f, -50f); // 시작 위치 (왼쪽 위 여백)

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

    public string GetTodaySuccessfulReceiptsText()  // 오늘 날짜의 성공한 영수증 텍스트로 가져오기
    {
        DateTime today = GameClock.gameTime.Date;
        if (successfulReceipts.Count == 0)
            return $"[{today:yyyy-MM-dd}] 성공한 영수증이 없습니다.\n";

        string result = $"=== [성공한 영수증] {today:yyyy-MM-dd} ===\n\n";
        int total = 0;

        foreach (var receipt in successfulReceipts)
        {
            result += receipt.GetReceiptText() + "\n";
            total += receipt.GetTotalPrice();
        }

        result += $"총 성공 금액: {total:N0}원\n";
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

}
