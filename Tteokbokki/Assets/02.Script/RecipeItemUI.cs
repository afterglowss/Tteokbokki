using UnityEngine;
using UnityEngine.UI; // ✨ Image 컴포넌트를 사용하기 위해 추가
using TMPro;

public class RecipeItemUI : MonoBehaviour
{
    public Image finishedFoodImage;          // ✨ [NEW] 완성된 요리 이미지
    public TextMeshProUGUI menuNameText;
    public TextMeshProUGUI ingredientsText;

    // ✨ [수정] 매개변수에 Sprite 추가
    public void Setup(string menuName, string recipeContent, Sprite foodSprite)
    {
        if (menuNameText != null) menuNameText.text = menuName;
        if (ingredientsText != null) ingredientsText.text = recipeContent;

        // ✨ 이미지 적용 로직
        if (finishedFoodImage != null)
        {
            if (foodSprite != null)
            {
                finishedFoodImage.sprite = foodSprite;
                finishedFoodImage.gameObject.SetActive(true);
            }
            else
            {
                // 혹시 이미지가 없는 메뉴라면 엑스박스 방지용으로 꺼둠
                finishedFoodImage.gameObject.SetActive(false);
            }
        }
    }
}