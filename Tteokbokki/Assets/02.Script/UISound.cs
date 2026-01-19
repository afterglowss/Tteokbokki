using UnityEngine;
using UnityEngine.EventSystems; // 클릭 감지를 위해 필수
using UnityEngine.UI;

public class UISound : MonoBehaviour, IPointerClickHandler
{
    [Header("Sound Settings")]
    public int soundID = 107; // 기본값 107 (클릭음)
    public bool playOnlyIfInteractable = true; // 버튼이 비활성화(Grayed out) 상태면 소리 안 나게

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 만약 이 오브젝트가 Button이나 Toggle 컴포넌트를 가지고 있다면, interactable 상태인지 체크
        if (playOnlyIfInteractable)
        {
            var btn = GetComponent<Selectable>(); // Button, Toggle, Slider 등의 부모 클래스
            if (btn != null && !btn.interactable) return; // 비활성화 상태면 소리 X
        }

        // 2. 소리 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundID);
        }
    }
}