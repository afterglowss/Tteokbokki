using UnityEngine;

public class MainSceneInitializer : MonoBehaviour
{
    private void Start()
    {
        if (GameLoadFlags.shouldLoadFromSave)
        {
            GameSaveManager.Instance.LoadGame();
            GameLoadFlags.shouldLoadFromSave = false;
        }
    }
}
