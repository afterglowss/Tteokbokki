using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReceiptStateManager : MonoBehaviour
{
    public static ReceiptStateManager Instance { get; private set; }

    public Receipt ActiveReceipt { get; private set; }  // ���� Ȱ��ȭ�� ������

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
        DontDestroyOnLoad(gameObject);  // �� ��ȯ���� ���� ����
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
        combined.ClearIngredientsText();  // ��� �ջ굵 �ʱ�ȭ
    }
}
