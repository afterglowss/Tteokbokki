using UnityEngine;
using Yarn.Unity;

public class TutorialManager : MonoBehaviour
{
    private DialogueRunner dialogueRunner;

    void Start()
    {
        //dialogueRunner = FindObjectOfType<DialogueRunner>();
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

    // 선택된 소스 반환
    static string SelectedSauce = "임시소스";

    [YarnFunction("getSelectedSauce")]
    public static string GetSelectedSauce()
    {
        return SelectedSauce;
    }
}
