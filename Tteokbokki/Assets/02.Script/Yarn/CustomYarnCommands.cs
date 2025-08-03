using UnityEngine;
using Yarn.Unity;

public class CustomYarnCommands : MonoBehaviour
{
    [YarnCommand("waitInput")]
    public static void WaitInputCommand()
    {
        WaitInputManager.ForceNextLineManual = true;
    }
}
