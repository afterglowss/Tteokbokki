using UnityEngine;
using UnityEngine.UI;

public class IngredientButton : MonoBehaviour
{
    public string ingredientName;
    public PlayerWokManager playerWokManager;

    public void OnButtonClick()
    {
        playerWokManager.AddIngredient(ingredientName);
        //Debug.Log($"{this.name} is clicked!");
    }
}