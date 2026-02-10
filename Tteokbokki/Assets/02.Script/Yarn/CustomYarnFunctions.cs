using UnityEngine;
using Yarn.Unity;

public class CustomYarnFunctions : MonoBehaviour
{
    [YarnFunction("setFace")]
    public static string SetFace(string expression)
    {
        // 표정만 바꿉니다. 켜고 끄는 건 Presenter가 알아서 합니다.
        if (CharacterFaceManager.Instance != null)
        {
            CharacterFaceManager.Instance.SetFace(expression);
        }
        return "";
    }
}
