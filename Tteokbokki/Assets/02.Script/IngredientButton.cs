using UnityEngine;
using UnityEngine.UI;

public class IngredientButton : MonoBehaviour
{
    public PlayerWokManager playerWokManager;

    public void OnButtonClick()
    {
        playerWokManager.AddIngredient(this.name);
        //Debug.Log($"{this.name} is clicked!");
    }
}