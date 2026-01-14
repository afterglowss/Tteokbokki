using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IngredientSpriteData
{
    public string ingredientName;
    public Sprite icon;
}

public class IngredientSpriteManager : MonoBehaviour
{
    public static IngredientSpriteManager Instance;

    // 인스펙터에서 재료 이름과 이미지를 짝지어 넣을 리스트
    public List<IngredientSpriteData> spriteList;

    private Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();

    private void Awake()
    {
        Instance = this;
        // 리스트를 딕셔너리로 변환 (검색 속도 향상)
        foreach (var data in spriteList)
        {
            if (!spriteDict.ContainsKey(data.ingredientName))
            {
                spriteDict.Add(data.ingredientName, data.icon);
            }
        }
    }

    public Sprite GetSprite(string name)
    {
        if (spriteDict.TryGetValue(name, out Sprite sprite))
        {
            return sprite;
        }
        Debug.LogWarning($"[이미지 누락] '{name}'에 해당하는 이미지가 없습니다.");
        return null; // 또는 기본 이미지 반환
    }
}