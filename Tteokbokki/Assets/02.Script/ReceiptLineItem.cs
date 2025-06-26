using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    public void Setup(Receipt receipt, float cookMinutes, ReceiptLineManager manager, ReceiptPopup popup, CombinedIngredientManager ingredientManager)
    {
        this.receipt = receipt;
        this.manager = manager;
        this.receiptPopup = popup;  // ������ ����
        this.combinedIngredientManager = ingredientManager;  // ������ ����
        cookTimeSeconds = cookMinutes * 60f;
        orderIDText.text = $"�ֹ���ȣ: {receipt.OrderID}";
        orderStartTime = receipt.OrderDateTime;

        receiptButton.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        DateTime now = GameClock.gameTime;

        TimeSpan elapsed = now - orderStartTime;

        if (elapsed.TotalMinutes >= cookTimeSeconds / 60f)
        {
            manager.RemoveReceipt(this);
            return;
        }
    }

    
    private void OnClick()
    {
        if (receiptPopup == null || combinedIngredientManager == null)
        {
            Debug.LogError("ReceiptPopup �Ǵ� CombinedIngredientManager�� ������� �ʾҽ��ϴ�.");
            return;
        }

        ReceiptStateManager.Instance.SetActiveReceipt(receipt);
        combinedIngredientManager.DisplayAllCombinedIngredients(receipt);
        receiptPopup.Show(receipt);
    }
    public Receipt GetReceipt() { return receipt; }
}
