using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;
using Unity.VisualScripting;

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

    private Vector3 originalPosition;
    private Transform originalParent;

    private int originalSiblingIndex;

    public int CurrentSlotIndex { get; set; }  // ����Ʈ �� �ڽ��� ��ġ �ε���

    public bool IsBeingDragged { get; private set; }

    private bool isTweening = false;

    public void CachePosition()
    {
        originalPosition = GetComponent<RectTransform>().anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
    }

    public void ReturnToOriginalPosition(float duration = 0.25f)
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        isTweening = true;
        GetComponent<RectTransform>().DOAnchorPos(originalPosition, duration).SetEase(Ease.OutCubic)
            .OnComplete(() => isTweening = false);
    }

    // �巡�� ���� �� ȣ��
    public void OnBeginDrag()
    {
        if (isTweening) return;  // �̹� Ʈ���� ���� ���̸� ����

        GetComponent<RectTransform>().DOComplete();

        IsBeingDragged = true;
        CachePosition();
    }
    public void OnEndDrag()
    {
        IsBeingDragged = false;
    }

    public void Setup(Receipt receipt, float cookMinutes, ReceiptLineManager manager, ReceiptPopup popup, CombinedIngredientManager ingredientManager)
    {
        this.receipt = receipt;
        this.manager = manager;
        this.receiptPopup = popup;  // ������ ����
        this.combinedIngredientManager = ingredientManager;  // ������ ����
        cookTimeSeconds = cookMinutes * 60f;
        orderIDText.text = $"�ֹ���ȣ: {receipt.OrderID}";
        orderStartTime = receipt.OrderDateTime;

        CachePosition();    // �巡�� ���� �ڸ� ���

        receiptButton.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        DateTime now = GameClock.gameTime;

        TimeSpan elapsed = now - orderStartTime;

        //Debug.Log($"[������ {receipt.OrderID}] ��� �ð�: {elapsed.TotalMinutes:F2}�� / ����: {cookTimeSeconds / 60f}��");

        if (elapsed.TotalMinutes >= cookTimeSeconds / 60f)
        {
            //Debug.LogWarning($"[������ {receipt.OrderID}] �ð��� �ʰ��Ǿ� �����˴ϴ�.");
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

    public Vector3 GetSlotPosition(float spacing)
    {
        return new Vector3(-CurrentSlotIndex * spacing, 0f, 0f);
    }
    public float GetRemainingTime()
    {
        TimeSpan elapsed = GameClock.gameTime - orderStartTime;
        return Mathf.Max(0f, cookTimeSeconds - (float)elapsed.TotalSeconds);
    }

    public float GetLimitTime()
    {
        return cookTimeSeconds;
    }
    public void OverrideRemainingTime(float remaining)
    {
        // 1. ��ȿ�� �˻�
        if (cookTimeSeconds <= 0f)
        {
            Debug.LogWarning("cookTimeSeconds�� ���� �������� �ʾҰų� 0�Դϴ�.");
            return;
        }

        // 2. ���� �ð� �� Ŭ����
        float clampedRemaining = Mathf.Clamp(remaining, 0f, cookTimeSeconds);

        // 3. ���� �ð� �����Ͽ� ����
        orderStartTime = GameClock.gameTime.AddSeconds(-(cookTimeSeconds - clampedRemaining));

        // 4. ����� �α� (����)
        // Debug.Log($"���ѽð�: {cookTimeSeconds}, �����ð�: {clampedRemaining}, ����� ���۽ð�: {orderStartTime}");
    }

}
