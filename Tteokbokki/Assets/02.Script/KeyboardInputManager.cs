using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // ✨ [필수] New Input System 사용

public class KeyboardInputManager : MonoBehaviour
{
    void Update()
    {
        // 0. 키보드가 연결되어 있는지 확인 (안전장치)
        if (Keyboard.current == null) return;

        // UI 입력 중이거나 일시정지면 무시
        if (GameClock.isPaused) return;

        // 1. 화구 선택 (1 ~ 5)
        HandleStoveSelection();

        // 2. 재료 투입 (Q~M)
        HandleIngredientInput();

        // 3. 조리 시작 (Space Bar)
        // ✨ Input.GetKeyDown(KeyCode.Space) -> Keyboard.current.spaceKey.wasPressedThisFrame
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlayerWokManager.Instance.OnCookButtonPressed();
        }

        // 4. (선택사항) ESC로 선택 해제
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (StoveManager.Instance.HasSelectedSlot())
                StoveManager.Instance.DeselectCurrentSlot();
        }
    }

    private void HandleStoveSelection()
    {
        // ✨ Input.GetKeyDown(KeyCode.Alpha1) -> Keyboard.current.digit1Key.wasPressedThisFrame
        // ✨ KeyCode.Keypad1 -> Keyboard.current.numpad1Key.wasPressedThisFrame

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SelectStove(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SelectStove(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SelectStove(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) SelectStove(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame) SelectStove(4);
    }

    private void SelectStove(int index)
    {
        if (StoveManager.Instance != null)
        {
            StoveManager.Instance.SelectStoveByIndex(index);
        }
    }

    private void HandleIngredientInput()
    {
        if (IngredientStockManager.Instance == null) return;

        // ✨ KeyCode 대신 Key 리스트를 가져옵니다.
        List<Key> registeredKeys = IngredientStockManager.Instance.GetAllRegisteredKeys();

        foreach (Key key in registeredKeys)
        {
            // ✨ New Input System 방식의 키 입력 감지
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                string ingredientName = IngredientStockManager.Instance.GetIngredientByKey(key);

                if (!string.IsNullOrEmpty(ingredientName))
                {
                    TryAddIngredient(ingredientName);
                }
            }
        }
    }

    private void TryAddIngredient(string ingredientName)
    {
        // 1. 화구 선택 확인
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, "화구를 먼저 선택해주세요 (1~5)", 1f);
            return;
        }

        // 2. 화구 상태 확인
        var selectedSlot = StoveManager.Instance.GetSelectedSlot();
        if (selectedSlot.IsCooking || selectedSlot.IsCooked)
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, "조리 중에는 재료를 넣을 수 없습니다!", 1f);
            return;
        }

        // 3. 재고 차감 및 투입
        if (IngredientStockManager.Instance.UseIngredient(ingredientName))
        {
            StoveManager.Instance.AddIngredientToSelectedSlot(ingredientName);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(100);
        }
        else
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, $"{ingredientName} 재고가 부족합니다!", 1f);
        }
    }
}