using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // ✨ [필수] New Input System 사용

public class KeyboardInputManager : MonoBehaviour
{
    public static KeyboardInputManager Instance { get; private set; } // 싱글톤 추가 (TutorialManager에서 접근용)

    // ✨ [NEW] 튜토리얼용 키 제한 시스템
    private HashSet<Key> allowedKeys = new HashSet<Key>();
    private bool isTutorialMode = false;

    private bool wasShiftHeld = false; // ✨ [NEW] Shift 키 이전 상태 저장용

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    // ✨ 튜토리얼 모드 켜기/끄기
    public void SetTutorialMode(bool active)
    {
        isTutorialMode = active;
        allowedKeys.Clear(); // 켜거나 끌 때 일단 초기화
    }

    // ✨ 특정 키 허용하기 (튜토리얼 단계별로 호출)
    public void AllowKey(Key key)
    {
        if (!allowedKeys.Contains(key)) allowedKeys.Add(key);
    }

    public void ConstrainKey(Key key)
    {
        if (allowedKeys.Contains(key)) allowedKeys.Remove(key);
    }

    // ✨ 모든 키 허용 (튜토리얼 끝날 때)
    public void AllowAllKeys()
    {
        allowedKeys.Clear();
        isTutorialMode = false;
    }
    void Update()
    {
        // 0. 키보드가 연결되어 있는지 확인 (안전장치)
        if (Keyboard.current == null) return;

        // ✨ [핵심 수정] 튜토리얼 중이면 시간 정지 여부와 상관없이 작동해야 함
        if (!isTutorialMode)
        {
            // 평소에는 일시정지면 입력 차단
            if (GameClock.isPaused) return;
        }

        // ✨ [NEW] Shift 키 눌림 감지 (왼쪽, 오른쪽 Shift 모두 대응)
        bool isShiftHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

        // 상태가 변했을 때만 UI 업데이트 한 번 실행 (성능 최적화)
        if (isShiftHeld != wasShiftHeld)
        {
            wasShiftHeld = isShiftHeld;
            if (IngredientStockManager.Instance != null)
            {
                IngredientStockManager.Instance.UpdateShiftUI(isShiftHeld);
            }
        }

        // 1. 화구 선택 (1 ~ 5)
        HandleStoveSelection();

        // 2. 재료 투입 (Q~M)
        HandleIngredientInput(isShiftHeld);

        // 3. 조리 시작 (Space Bar)
        // ✨ Input.GetKeyDown(KeyCode.Space) -> Keyboard.current.spaceKey.wasPressedThisFrame
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // 튜토리얼 모드라면 Space 키가 허용되었는지 확인
            if (!isTutorialMode || allowedKeys.Contains(Key.Space))
            {
                PlayerWokManager.Instance.OnCookButtonPressed();
            }
        }

        // 4. ESC (튜토리얼 중엔 보통 막음)
        if (!isTutorialMode && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (StoveManager.Instance.HasSelectedSlot())
                StoveManager.Instance.DeselectCurrentSlot();
        }
    }

    private void HandleStoveSelection()
    {
        // 튜토리얼 중에는 화구 선택 키(1~5)도 허용된 경우만 작동
        CheckAndSelect(Key.Digit1, Key.Numpad1, 0);
        CheckAndSelect(Key.Digit2, Key.Numpad2, 1);
        CheckAndSelect(Key.Digit3, Key.Numpad3, 2);
        CheckAndSelect(Key.Digit4, Key.Numpad4, 3);
        CheckAndSelect(Key.Digit5, Key.Numpad5, 4);
    }

    private void CheckAndSelect(Key digit, Key numpad, int index)
    {
        bool isPressed = Keyboard.current[digit].wasPressedThisFrame || Keyboard.current[numpad].wasPressedThisFrame;

        if (isPressed)
        {
            // 튜토리얼 모드일 땐 허용 목록에 있어야만 통과
            if (isTutorialMode)
            {
                if (!allowedKeys.Contains(digit) && !allowedKeys.Contains(numpad)) return;
            }
            SelectStove(index);
        }
    }

    private void SelectStove(int index)
    {
        if (StoveManager.Instance != null)
        {
            StoveManager.Instance.SelectStoveByIndex(index);
        }
    }

    private void HandleIngredientInput(bool isShiftHeld)
    {
        if (IngredientStockManager.Instance == null) return;

        // ✨ KeyCode 대신 Key 리스트를 가져옵니다.
        List<Key> registeredKeys = IngredientStockManager.Instance.GetAllRegisteredKeys();

        foreach (Key key in registeredKeys)
        {
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                // ✨ Shift가 눌려있고, 지금 누른 키가 Q, W, E 등이라면 -> 입력 무시 (기능 상실)
                if (isShiftHeld && IngredientStockManager.ShiftSubstituteMap.ContainsValue(key))
                {
                    continue;
                }

                if (isTutorialMode && !allowedKeys.Contains(key)) continue;

                string ingredientName = IngredientStockManager.Instance.GetIngredientByKey(key);
                if (!string.IsNullOrEmpty(ingredientName))
                {
                    TryAddIngredient(ingredientName);
                }
            }
        }

        if (isShiftHeld)
        {
            foreach (var kvp in IngredientStockManager.ShiftSubstituteMap)
            {
                Key originalKey = kvp.Key;   // 예: T
                Key shiftKey = kvp.Value;    // 예: Q

                // Shift 누른 채로 Q가 눌렸다면?
                if (Keyboard.current[shiftKey].wasPressedThisFrame)
                {
                    if (isTutorialMode && (!allowedKeys.Contains(originalKey) && !allowedKeys.Contains(shiftKey))) continue;

                    // Q를 눌렀지만 T에 해당하는 재료를 꺼내옵니다.
                    string ingredientName = IngredientStockManager.Instance.GetIngredientByKey(originalKey);
                    if (!string.IsNullOrEmpty(ingredientName))
                    {
                        TryAddIngredient(ingredientName);
                    }
                }
            }
        }
    }

    private void TryAddIngredient(string ingredientName)
    {
        // ✨ 0. 에러 메시지에 띄울 번역된 재료 이름 미리 준비!
        string transName = TextTranslator.GetIngredientName(ingredientName);

        // 1. 화구 선택 확인
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            // ✨ 번역 적용: "화구를 먼저 선택해주세요 (1~5)"
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_SelectStove_Key"), 1f);
            return;
        }

        // 2. 화구 상태 확인
        var selectedSlot = StoveManager.Instance.GetSelectedSlot();
        if (selectedSlot.IsCooking || selectedSlot.IsCooked)
        {
            // ✨ 번역 적용: "조리 중에는 재료를 넣을 수 없습니다!"
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_StoveInUse"), 1f);
            return;
        }

        // 3. 화구 내 재료 최대 개수 제한 체크
        if (!selectedSlot.CanAddIngredient(ingredientName))
        {
            // ✨ 번역 적용: "{0}은(는) 한 화구에 최대 10개까지만 넣을 수 있습니다!"
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_MaxIngredient", transName), 1f);
            return;
        }

        // 4. 재고 차감 및 투입
        if (IngredientStockManager.Instance.UseIngredient(ingredientName))
        {
            StoveManager.Instance.AddIngredientToSelectedSlot(ingredientName);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(100);
        }
        else
        {
            // ✨ 번역 적용: "{0} 재고가 부족합니다!"
            TooltipManager.ShowFollowMouse(TooltipType.UI, TextTranslator.GetUIText("Warning_OutOfStock", transName), 1f);
        }
    }
}