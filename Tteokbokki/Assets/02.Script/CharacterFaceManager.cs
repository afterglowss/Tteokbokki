using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; // 만약 Image 사용할 경우 필요

public class CharacterFaceManager : MonoBehaviour
{
    public static CharacterFaceManager Instance { get; private set; }
    [Header("표정을 표시할 이미지 컴포넌트")]
    public Image faceImage; // UI용
    // public SpriteRenderer faceRenderer; // 만약 SpriteRenderer 쓸 경우 대체 가능

    [Header("표정 이름과 스프라이트 매핑")]
    public List<Sprite> faceSprites;

    private Dictionary<string, Sprite> faceDictionary = new();

    void Awake()
    {
        // 자동 등록: Sprite 이름으로 Dictionary 구성
        foreach (var sprite in faceSprites)
        {
            faceDictionary[sprite.name] = sprite;
        }
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetFace(string expressionName)
    {
        if (faceImage == null) return;

        // 1. 만약 이름이 비어있으면 (독백 시) 이미지 컴포넌트만 끕니다.
        // 이렇게 하면 오브젝트는 살아있어서 자식인 텍스트(lineText) 에러가 안 납니다!
        if (string.IsNullOrEmpty(expressionName))
        {
            faceImage.enabled = false;
            return;
        }

        // 2. 표정을 세팅할 때는 다시 이미지를 활성화합니다.
        faceImage.enabled = true;

        if (faceDictionary.TryGetValue(expressionName, out var sprite))
        {
            faceImage.sprite = sprite;
        }
        else
        {
            Debug.LogWarning($"표정 '{expressionName}'에 해당하는 스프라이트가 없습니다.");
        }
    }
}
