using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReceiptStateManager : MonoBehaviour
{
    public static ReceiptStateManager Instance { get; private set; }

    public Receipt ActiveReceipt { get; private set; }  // ���� Ȱ��ȭ�� ������

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

    public void SetActiveReceipt(Receipt receipt)
    {
        ActiveReceipt = receipt;
    }

    public void ClearActiveReceipt()
    {
        ActiveReceipt = null;
        FindObjectOfType<ReceiptPopup>()?.Close();
        FindObjectOfType<CombinedIngredientManager>()?.ClearIngredientsText();  // ��� �ջ굵 �ʱ�ȭ
    }
}
