using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ReceiptLineManager : MonoBehaviour
{
    public GameObject receiptPrefab;   // 영수증 UI 프리팹
    public Transform receiptLineParent; // 영수증들이 매달릴 부모 (줄)

    //private List<ReceiptLineItem> activeReceipts = new();
    private List<Receipt> missedReceipts = new List<Receipt>();     // 놓친 영수증 저장해둘 리스트
    public List<Receipt> GetMissedReceipts() => new List<Receipt>(missedReceipts);
    public void ClearMissedReceipts() => missedReceipts.Clear();

    public float cookLimitMinutes = 30f;

    public ReceiptPopup receiptPopup;
    public CombinedIngredientManager combinedIngredientManager;

    public int maxSlots = 15;
    private List<ReceiptLineItem> receiptSlots = new();  // 고정된 순서 유지
    private Queue<Receipt> pendingReceipts = new();
    public float slotSpacing = 160f;  // 슬롯 간 거리


    public TextMeshProUGUI pendingCountText;

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

        // 모든 메뉴가 조리중이거나 완료된 상태여야 성공해서 삭제된 영수증
        bool isUncompleted = !StoveManager.AllMenusHandledStatic(receipt);

        if (isUncompleted)
        {
            missedReceipts.Add(receipt);
        }

        if (ReceiptStateManager.Instance.ActiveReceipt == receipt)
        {
            ReceiptStateManager.Instance.ClearActiveReceipt();
            receiptPopup.Close(); // 팝업 닫기
        }

        //activeReceipts.Remove(item);
        receiptSlots.Remove(item);
        Destroy(item.gameObject);

        if (pendingReceipts.Count > 0)
        {
            var nextReceipt = pendingReceipts.Dequeue();
            CreateAndAddReceiptUI(nextReceipt);
            UpdatePendingCountUI();  // UI 갱신
        }

        RepositionAll();  // 나머지 영수증들 이동
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


    //public void ClearAllReceipts()
    //{
    //    foreach (var item in activeReceipts)
    //    {
    //        Destroy(item.gameObject);
    //    }
    //    activeReceipts.Clear();
    //}
    private void RepositionAll()
    {
        for (int i = 0; i < receiptSlots.Count; i++)
        {
            var item = receiptSlots[i];
            if (item == null || item.gameObject == null) continue;
            Vector3 targetPosition = new Vector3(-i * slotSpacing, 0f, 0f);  // 오른쪽에서 왼쪽으로
            item.transform.localPosition = targetPosition;
        }
    }

}
