using System;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class TutorialManager : MonoBehaviour
{
    private DialogueRunner dialogueRunner;
    private ReceiptLineManager receiptLineManager;
    private bool hasSpawned = false;

    void Start()
    {
    }


    // 다이얼로그 시작
    public void OnObjectDialogueStart(string objectNode)
    {
        dialogueRunner.StartDialogue(objectNode);
    }

    // 다이얼로그 중지
    public void DialogueStop()
    {
        dialogueRunner.Stop();
    }

    // 오브젝트 하이라이트 켜기
    [YarnCommand("highlight")]
    public static void Highlight(string objName)
    {
        GameObject obj = GameObject.Find("TutorialCanvas")?.transform.Find(objName)?.gameObject;
        if (obj != null)
            obj.SetActive(true);
        else
            Debug.LogWarning("Highlight 대상 오브젝트를 찾을 수 없음: " + objName);
    }

    // 오브젝트 비활성화
    [YarnCommand("objectDelete")]
    public static void ObjectDelete(string objName)
    {
        GameObject obj = GameObject.Find("TutorialCanvas")?.transform.Find(objName)?.gameObject;
        if (obj != null)
            obj.SetActive(false);
        else
            Debug.LogWarning("ObjectDelete 대상 오브젝트를 찾을 수 없음: " + objName);
    }

    // 튜토리얼용 영수증 발행
    [YarnCommand("spawnTutorialReceipt")]
    public static void SpawnTutorialReceipt()
    {
        // 필요한 매니저들 찾기
        var receiptManager = GameObject.FindObjectOfType<ReceiptLineManager>();
        var dialogueRunner = GameObject.FindObjectOfType<DialogueRunner>();
        if (receiptManager == null || dialogueRunner == null)
        {
            Debug.LogError("ReceiptLineManager 또는 DialogueRunner를 찾을 수 없습니다.");
            return;
        }

        // 이미 영수증이 있으면 생성하지 않음
        if (receiptManager.GetReceiptSlots().Count > 0) return;

        // 영수증 생성
        Receipt tutorialReceipt = new Receipt(DateTime.Now, 1);
        receiptManager.AddNewReceipt(tutorialReceipt);

        // 마지막으로 생성된 ReceiptLineItem 가져오기
        var lastReceiptItem = receiptManager.GetReceiptSlots()[receiptManager.GetReceiptSlots().Count - 1];
        var btn = lastReceiptItem.GetComponent<Button>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                // 클릭 시 Yarn 노드로 이동
                dialogueRunner.StartDialogue("AddIngredientStep");
            });
        }
    }
    [YarnFunction("getSawTutorial")]
    public static bool GetSawTutorial()
    {
        return PlayerPrefs.GetInt("SawTutorial", 0) == 1;
    }

    // 튜토리얼 끝났을 때 호출
    [YarnCommand("setSawTutorial")]
    public static void SetSawTutorial()
    {
        PlayerPrefs.SetInt("SawTutorial", 1);
        PlayerPrefs.Save();
    }
}
