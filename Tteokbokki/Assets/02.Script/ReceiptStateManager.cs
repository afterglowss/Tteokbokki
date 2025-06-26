using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReceiptStateManager : MonoBehaviour
{
    public static ReceiptStateManager Instance { get; private set; }

    public Receipt ActiveReceipt { get; private set; }  // 현재 활성화된 영수증

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

    public void SetActiveReceipt(Receipt receipt)
    {
        ActiveReceipt = receipt;
    }

    public void ClearActiveReceipt()
    {
        ActiveReceipt = null;
        FindObjectOfType<ReceiptPopup>()?.Close();
        FindObjectOfType<CombinedIngredientManager>()?.ClearIngredientsText();  // 재료 합산도 초기화
    }
}
