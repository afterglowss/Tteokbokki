using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
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

    private void RepositionAll()
    {
        int count = receiptSlots.Count;

        RectTransform rectTransform = GetComponent<RectTransform>();

        // 시작 기준점을 오른쪽 끝으로 이동시키기 위한 x 오프셋
        float offsetX = rectTransform.rect.width;

        for (int i = 0; i < count; i++)
        {
            var item = receiptSlots[i];
            if (item == null || item.gameObject == null) continue;

            if (item.IsBeingDragged) continue;

            RectTransform rt = item.GetComponent<RectTransform>();

            Vector3 targetPosition = new Vector3(-i * slotSpacing + offsetX, rectTransform.position.y, 0f);
            item.CurrentSlotIndex = i;

            rt.DOAnchorPos(targetPosition, 0.3f).SetEase(Ease.OutCubic);
            //item.transform.localPosition = targetPosition;
        }
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

}
