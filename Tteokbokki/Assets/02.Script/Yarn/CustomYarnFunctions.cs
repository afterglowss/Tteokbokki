using UnityEngine;
using Yarn.Unity;

public class CustomYarnFunctions : MonoBehaviour
{
    [YarnFunction("setFace")]
    public static string SetFace(string expression)
    {
        CharacterFaceManager.Instance.SetFace(expression);
        return ""; // Yarn 함수는 반환이 필요함
    }
}
