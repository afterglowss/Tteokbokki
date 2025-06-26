using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReceiptLineItem : MonoBehaviour
{
    public Button receiptButton;
    public TextMeshProUGUI orderIDText;
    public float cookTimeSeconds;

    private Receipt receipt;
    private ReceiptLineManager manager;
    private float elapsedTime = 0f;
    private ReceiptPopup receiptPopup;
    private CombinedIngredientManager combinedIngredientManager;

    public void Setup(Receipt receipt, float cookMinutes, ReceiptLineManager manager, ReceiptPopup popup, CombinedIngredientManager ingredientManager)
    {
        this.receipt = receipt;
        this.manager = manager;
        this.receiptPopup = popup;  // 의존성 주입
        this.combinedIngredientManager = ingredientManager;  // 의존성 주입
        cookTimeSeconds = cookMinutes * 60f;
        orderIDText.text = $"주문번호: {receipt.OrderID}";

        receiptButton.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime * (60f / 3f);

        float progress = Mathf.Clamp01(elapsedTime / cookTimeSeconds);
        transform.localPosition = new Vector3(progress * 800f, transform.localPosition.y, 0);

        if (elapsedTime >= cookTimeSeconds)
        {
            manager.RemoveReceipt(this);
        }
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
}
