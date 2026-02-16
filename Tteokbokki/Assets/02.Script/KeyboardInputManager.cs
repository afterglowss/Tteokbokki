using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // ✨ [필수] New Input System 사용

public class KeyboardInputManager : MonoBehaviour
{
    public static KeyboardInputManager Instance { get; private set; } // 싱글톤 추가 (TutorialManager에서 접근용)

    // ✨ [NEW] 튜토리얼용 키 제한 시스템
    private HashSet<Key> allowedKeys = new HashSet<Key>();
    private bool isTutorialMode = false;

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

        // 1. 화구 선택 (1 ~ 5)
        HandleStoveSelection();

        // 2. 재료 투입 (Q~M)
        HandleIngredientInput();

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
                if (isTutorialMode && !allowedKeys.Contains(key)) continue;

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