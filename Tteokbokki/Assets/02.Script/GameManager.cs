using System;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ReceiptLineManager receiptLineManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EndOfDay()
    {
        var missed = receiptLineManager.GetMissedReceipts();
        var successful = receiptLineManager.GetSuccessfulReceipts();
        DateTime today = GameClock.gameTime.Date;

        ReceiptManager.SaveMissedReceipts(missed, today);
        ReceiptManager.SaveSuccessfulReceipts(successful, today);

        // �Ǹ� �Ѿ� ���
        int successTotal = successful.Sum(r => r.GetTotalPrice());
        int missedTotal = missed.Sum(r => r.GetOrders().Sum(o => o.TotalPrice));

        // ���� ����
        PlayerWalletManager.Instance.DeductDailyTaxes(successTotal);

        // �α� ���
        Debug.Log($"[����] ���� �ֹ� {successful.Count}�� / �� �Ǹűݾ�: {successTotal:N0}��");
        Debug.Log($"[����] �̿Ϸ� �ֹ� {missed.Count}�� / �ս� �ݾ�: {missedTotal:N0}��");
        Debug.Log($"[����] ���� {Mathf.RoundToInt(successTotal * PlayerWalletManager.Instance.taxRate):N0}�� ����");

        // �ʱ�ȭ
        receiptLineManager.ClearMissedReceipts();
        receiptLineManager.ClearSuccessfulReceipts(); // ���� ��ϵ� �ʱ�ȭ!
    }
}
