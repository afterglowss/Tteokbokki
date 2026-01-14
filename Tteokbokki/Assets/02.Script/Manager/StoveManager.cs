using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StoveManager : MonoBehaviour
{
    public static StoveManager Instance { get; private set; }
    public StoveSlot[] stoves;
    public TextMeshProUGUI resultText;

    private StoveSlot selectedSlot;

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
            // 선택 해제 시 UI도 비워줌
            PlayerWokManager.Instance.UpdateUI(null);
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
}