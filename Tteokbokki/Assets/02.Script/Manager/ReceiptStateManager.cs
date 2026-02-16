using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReceiptStateManager : MonoBehaviour
{
    public static ReceiptStateManager Instance { get; private set; }

    public Receipt ActiveReceipt { get; private set; }  // 현재 활성화된 영수증

    public ReceiptPopup receiptPopup;
    public CombinedIngredientManager combined;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);  // 씬 전환에도 유지 가능
    }

    private void Start()
    {
        if (receiptPopup != null)
        {
            receiptPopup.OnPopupClosed -= OnPopupClosedHandler; // 중복 방지
            receiptPopup.OnPopupClosed += OnPopupClosedHandler;
        }
    }

    private void OnPopupClosedHandler()
    {
        ClearActiveReceipt();
    }

    public void SetActiveReceipt(Receipt receipt)
    {
        ActiveReceipt = receipt;

        // ✨ [NEW] 선택 상태가 변경되었으니 비주얼(아웃라인) 갱신 요청
        if (ReceiptLineManager.Instance != null)
        {
            ReceiptLineManager.Instance.UpdateSelectionOutlines(receipt);
        }
    }

    public void ClearActiveReceipt()
    {
        if (ActiveReceipt == null) return;
        ActiveReceipt = null;
        receiptPopup.gameObject.SetActive(false);
        combined.ClearIngredientsText();  // 재료 합산도 초기화

        // ✨ [NEW] 선택이 해제되었으니 모든 아웃라인 끄기 (null 전달)
        if (ReceiptLineManager.Instance != null)
        {
            ReceiptLineManager.Instance.UpdateSelectionOutlines(null);
        }
    }
}
