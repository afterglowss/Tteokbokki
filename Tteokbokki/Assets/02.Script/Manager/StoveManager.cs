using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ✨ 메뉴 이름과 조리 중 이미지를 연결하는 데이터
[System.Serializable]
public struct MenuCookingVisual
{
    public string menuName;      // 예: "마라 군자 떡볶이"
    public Sprite cookingSprite; // 색이 변한 조리 중 이미지
    public Sprite finishedSprite; // ✨ [NEW] 조리 완료된 웍 이미지 (CookedFoodUI 용)
}

public class StoveManager : MonoBehaviour
{
    public static StoveManager Instance { get; private set; }
    public StoveSlot[] stoves;
    public TextMeshProUGUI resultText;

    [Header("Visual Settings")]
    public Sprite commonRawSprite;   // 조리 시작 직후 (희멀건한 상태)
    public Sprite ruinedCookingSprite; // ✨ [이름변경] 조리 중 망한 이미지 (Overlay용)
    public Sprite ruinedFinishedSprite; // ✨ [NEW] 조리 완료된 망한 이미지 (CookedFoodUI용)
    public List<MenuCookingVisual> menuVisuals; // 메뉴별 조리 이미지 리스트

    private StoveSlot selectedSlot;

    // 🔥 [사운드] 전역 끓는 소리 관리를 위한 변수
    private AudioSource globalBoilingSource;
    private int activeCookingCount = 0; // 현재 요리 중인 화구 개수

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        foreach (var slot in stoves)
        {
            slot.Initialize(this);
        }
    }

    public void SelectSlot(StoveSlot slot)
    {
        if (selectedSlot == slot)
        {
            DeselectCurrentSlot();
            return;
        }

        // 기존 슬롯 선택 해제
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        // 새 슬롯 선택
        selectedSlot = slot;
        selectedSlot.SetSelected(true);
    }

    public void DeselectCurrentSlot()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            PlayerWokManager.Instance.ClearUI();
            selectedSlot = null;
        }
    }

    public bool HasSelectedSlot() => selectedSlot != null;

    // 유틸리티: 현재 이 슬롯이 선택된 상태인지 확인 (StoveSlot에서 호출)
    public bool IsSelected(StoveSlot slot) => selectedSlot == slot;

    // ✨ [NEW] 선택된 슬롯에 재료 추가
    public void AddIngredientToSelectedSlot(string ingredientName)
    {
        if (selectedSlot != null)
        {
            selectedSlot.AddIngredient(ingredientName);
        }
        else
        {
            Debug.LogWarning("선택된 화구가 없어 재료를 넣을 수 없습니다.");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "화구를 먼저 선택해주세요!", 1f);
        }
    }

    // ✨ [NEW] 선택된 슬롯 조리 시도
    public void TryCookSelectedSlot()
    {
        if (selectedSlot != null)
        {
            selectedSlot.TryStartCooking();
        }
        else
        {
            Debug.LogWarning("선택된 화구가 없습니다.");
        }
    }

    public void ClearAllStoves()
    {
        foreach (var slot in stoves)
        {
            if (slot != null) slot.ResetSlot();
        }
    }

    public StoveSlot GetSelectedSlot()
    {
        return selectedSlot;
    }

    // ✨ [NEW] 메뉴 이름으로 스프라이트를 찾아주는 함수
    public Sprite GetCookingSprite(string menuName)
    {
        foreach (var visual in menuVisuals)
        {
            if (visual.menuName == menuName)
                return visual.cookingSprite;
        }

        Debug.LogWarning($"[StoveManager] '{menuName}'에 해당하는 이미지를 찾을 수 없습니다! Ruined 이미지를 반환합니다. Inspector를 확인하세요.");
        // 리스트에 없으면 기본적으로 망한 스프라이트 반환 (혹은 null)
        return ruinedCookingSprite; // 없으면 망한 오버레이 반환
    }

    // ✨ [NEW] 메뉴 이름으로 '완성된 웍 이미지'를 찾아주는 함수
    public Sprite GetFinishedSprite(string menuName)
    {
        foreach (var visual in menuVisuals)
        {
            if (visual.menuName == menuName)
                return visual.finishedSprite;
        }

        Debug.LogWarning($"[StoveManager] '{menuName}' 완성 이미지를 찾을 수 없습니다! 망한 이미지를 반환합니다.");
        return ruinedFinishedSprite; // 없으면 망한 완성본 반환
    }

    // 🔥 [사운드] 화구가 "저 요리 시작해요!"라고 보고하는 함수
    public void NotifyCookingStarted()
    {
        if (activeCookingCount == 0)
        {
            // 아무것도 안 끓이다가 이제 끓기 시작함 -> 소리 켬 (ID 101)
            if (AudioManager.Instance != null)
                globalBoilingSource = AudioManager.Instance.PlayLoopSFX(101);
        }
        activeCookingCount++;
    }

    // 🔥 [사운드] 화구가 "저 요리 끝났어요(혹은 취소)"라고 보고하는 함수
    public void NotifyCookingEnded()
    {
        activeCookingCount--;

        if (activeCookingCount <= 0)
        {
            // 더 이상 끓는 화구가 없음 -> 소리 끔
            activeCookingCount = 0; // 혹시 모를 음수 방지
            if (globalBoilingSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoopSFX(globalBoilingSource);
                globalBoilingSource = null;
            }
        }
    }
}