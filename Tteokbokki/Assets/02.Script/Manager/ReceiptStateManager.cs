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
        DontDestroyOnLoad(gameObject);  // 씬 전환에도 유지 가능
    }

    private void Start()
    {
        receiptPopup.OnPopupClosed += () => {
            ClearActiveReceipt();
        };
    }

    public void SetActiveReceipt(Receipt receipt)
    {
        ActiveReceipt = receipt;
    }

    public void ClearActiveReceipt()
    {
        if (ActiveReceipt == null) return;
        ActiveReceipt = null;
        receiptPopup.gameObject.SetActive(false);
        combined.ClearIngredientsText();  // 재료 합산도 초기화
    }
}
