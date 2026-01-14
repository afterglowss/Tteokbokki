using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
using SaveData; // StoveSlotSaveData 사용을 위해 필요할 수 있음 (네임스페이스 확인 필요)

public class StoveSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;

    // ✨ Image 컴포넌트 제어 (Sprite 교체용)
    public Image wokImage;
    public GameObject selectedHighlight;
    public Button cookButton;

    [Header("Wok Sprites")]
    // ✨ 상태별 웍 이미지 스프라이트
    public Sprite wokEmptySprite;       // 빈 웍
    public Sprite wokIngredientsSprite; // 재료가 담긴 웍
    public Sprite wokLidSprite;         // 뚜껑 덮인 웍 (조리 중)

    [Header("Spawn Settings")]
    public Transform cookedFoodSpawnPoint;
    public GameObject cookedFoodPrefab;

    // --- 내부 상태 변수 ---
    private float cookTimeSeconds;
    private float cookTimeRemaining;
    private bool isCooking = false;
    private bool isCooked = false;

    private bool isDraggingWok = false;

    private GameObject spawnedFood;
    private Dictionary<string, int> currentIngredients;
    private Dictionary<string, int> pendingIngredients = new Dictionary<string, int>();

    private Action<Dictionary<string, int>> onCookComplete;
    private StoveManager stoveManager;

    private Canvas canvas;
    private RectTransform wokRectTransform;
    private Vector3 originalIconPos;
    private Transform originalIconParent;
    private CanvasGroup iconCanvasGroup;

    // 프로퍼티
    public bool IsCooking => isCooking;
    public bool IsCooked => isCooked;
    public bool HasPendingIngredients => pendingIngredients.Count > 0 && !isCooking && !isCooked;

    public void Initialize(StoveManager manager)
    {
        stoveManager = manager;
        var foundCanvas = GetComponentInParent<Canvas>();
        canvas = foundCanvas != null ? foundCanvas.rootCanvas : null;

        if (wokImage != null)
        {
            wokRectTransform = wokImage.GetComponent<RectTransform>();

            iconCanvasGroup = wokImage.GetComponent<CanvasGroup>();
            if (iconCanvasGroup == null) iconCanvasGroup = wokImage.gameObject.AddComponent<CanvasGroup>();

            // 초기 상태: 웍 숨김
            wokImage.gameObject.SetActive(false);
        }

        if (cookButton != null)
        {
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(TryStartCooking);
            cookButton.gameObject.SetActive(false);
        }
    }

    // --- 1. 화구 선택 로직 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDraggingWok) return;

        if (stoveManager != null)
        {
            stoveManager.SelectSlot(this);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);

        if (selected)
        {
            // 선택됨: 아무것도 없는 상태라면 '빈 웍' 보여주기
            if (!isCooking && !isCooked && pendingIngredients.Count == 0)
            {
                SetWokState(wokEmptySprite);
            }
            UpdateInfoUI();
        }
        else
        {
            // 선택 해제됨: 재료가 없는 상태('빈 웍')였다면 웍 숨기기
            if (!isCooking && !isCooked && pendingIngredients.Count == 0)
            {
                wokImage.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateInfoUI()
    {
        if (!stoveManager.IsSelected(this)) return;

        if (isCooking)
        {
            PlayerWokManager.Instance.UpdateUI(currentIngredients, "조리 중...");
        }
        else if (isCooked)
        {
            PlayerWokManager.Instance.UpdateUI(currentIngredients, "조리 완료!");
        }
        else
        {
            PlayerWokManager.Instance.UpdateUI(pendingIngredients, "준비 중");
        }
    }

    private void SetWokState(Sprite sprite)
    {
        if (wokImage != null)
        {
            wokImage.sprite = sprite;
            wokImage.gameObject.SetActive(true);
        }
    }

    // --- 2. 재료 추가 ---
    public void AddIngredient(string name)
    {
        if (isCooking || isCooked) return;

        if (!pendingIngredients.ContainsKey(name))
            pendingIngredients[name] = 0;

        pendingIngredients[name]++;

        // 재료가 들어갔으므로 '재료 담긴 웍' 이미지로 변경
        SetWokState(wokIngredientsSprite);

        timerText.text = "준비중";
        if (cookButton != null) cookButton.gameObject.SetActive(true);

        UpdateInfoUI();
    }

    public void ClearPending()
    {
        if (isCooking) return;

        pendingIngredients.Clear();

        // 재료 비우면 다시 '빈 웍' 상태로 (선택 여부에 따라 표시/숨김)
        if (stoveManager.IsSelected(this))
        {
            SetWokState(wokEmptySprite);
        }
        else
        {
            wokImage.gameObject.SetActive(false);
        }

        timerText.text = "대기중";
        if (cookButton != null) cookButton.gameObject.SetActive(false);

        UpdateInfoUI();
    }

    // --- 3. 조리 시작 ---
    public void TryStartCooking()
    {
        if (pendingIngredients.Count == 0)
        {
            Debug.LogWarning("재료가 없습니다.");
            return;
        }

        if (PlayerWokManager.Instance.CheckRecipe(pendingIngredients))
        {
            StartCooking(pendingIngredients, 5 * 60f, null);
            pendingIngredients.Clear();
            if (cookButton != null) cookButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("기본 재료가 부족합니다!");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "기본 재료가 부족합니다!", 1f);
        }
    }

    private void StartCooking(Dictionary<string, int> ingredients, float cookTime, Action<Dictionary<string, int>> onComplete)
    {
        currentIngredients = new Dictionary<string, int>(ingredients);
        cookTimeSeconds = cookTime;
        cookTimeRemaining = cookTime;
        this.onCookComplete = onComplete;

        isCooking = true;
        isCooked = false;

        // 조리 시작 -> '뚜껑 덮인 웍' 이미지로 변경
        SetWokState(wokLidSprite);
        if (cookButton != null) cookButton.gameObject.SetActive(false);

        UpdateInfoUI();
        UpdateTimerDisplay();
    }

    // --- 4. 드래그 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasPendingIngredients) return;

        isDraggingWok = true;

        originalIconPos = wokImage.transform.localPosition;
        originalIconParent = wokImage.transform.parent;

        // 캔버스 최상단으로 이동 (화면 앞으로 가져오기)
        if (canvas != null)
        {
            wokImage.transform.SetParent(canvas.transform);
        }

        // 크기 및 회전 초기화 (부모 변경 시 왜곡 방지)
        wokImage.transform.localScale = Vector3.one;
        wokImage.transform.localRotation = Quaternion.identity;

        // 레이캐스트 차단 해제 (드래그 중인 물체가 마우스 이벤트를 가로채지 않도록)
        if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingWok || canvas == null) return;

        // ✨ [핵심 수정] 영수증(ReceiptDragHandler)과 동일한 Delta 이동 방식 적용
        // 화면 좌표계나 카메라 설정에 상관없이 마우스 이동량만큼 UI를 이동시킵니다.
        if (wokRectTransform != null)
        {
            wokRectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingWok) return;

        isDraggingWok = false;

        if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = true;

        // 원래 부모와 위치로 복귀
        wokImage.transform.SetParent(originalIconParent);
        wokImage.transform.localPosition = originalIconPos;
        wokImage.transform.localScale = Vector3.one;

        // 상태에 따른 이미지 표시 갱신 (휴지통에 버려서 비워졌다면 빈 웍/숨김 처리)
        if (pendingIngredients.Count == 0 && !isCooking && !isCooked)
        {
            if (stoveManager.IsSelected(this))
                SetWokState(wokEmptySprite);
            else
                wokImage.gameObject.SetActive(false);
        }
    }

    // --- 5. 툴팁 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDraggingWok) return;

        if (isCooking && currentIngredients != null)
        {
            string tooltip = "조리 중:\n";
            foreach (var pair in currentIngredients)
            {
                tooltip += $"{pair.Key} x{pair.Value}\n";
            }
            TooltipManager.ShowFollowMouse(TooltipType.Info, tooltip);
        }
        else if (HasPendingIngredients)
        {
            string tooltip = "준비 중:\n";
            foreach (var pair in pendingIngredients)
            {
                tooltip += $"{pair.Key} x{pair.Value}\n";
            }
            TooltipManager.ShowFollowMouse(TooltipType.Info, tooltip);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Hide(TooltipType.Info);
    }

    // --- Update ---
    void Update()
    {
        if (!isCooking) return;

        cookTimeRemaining -= Time.deltaTime * (60f / 3f);
        if (cookTimeRemaining <= 0)
        {
            FinishCooking();
        }
        else
        {
            UpdateTimerDisplay();
        }
    }

    private void FinishCooking()
    {
        isCooking = false;
        isCooked = true;
        timerText.text = "완료!";

        // 조리 완료 -> 웍 이미지 사라짐
        wokImage.gameObject.SetActive(false);

        if (spawnedFood != null) Destroy(spawnedFood);

        spawnedFood = Instantiate(cookedFoodPrefab, cookedFoodSpawnPoint);
        spawnedFood.transform.localPosition = Vector3.zero;

        var foodUI = spawnedFood.GetComponent<CookedFoodUI>();
        if (foodUI != null)
        {
            foodUI.Initialize(currentIngredients);
            foodUI.originStoveSlot = this;
        }

        onCookComplete?.Invoke(currentIngredients);

        UpdateInfoUI();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(cookTimeRemaining / 60);
        int seconds = Mathf.FloorToInt(cookTimeRemaining % 60);
        timerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public void NotifyFoodPickedUp()
    {
        spawnedFood = null;
        ResetSlot();
    }

    public void ResetSlot()
    {
        isCooked = false;
        isCooking = false;
        currentIngredients = null;
        pendingIngredients.Clear();
        onCookComplete = null;

        wokImage.gameObject.SetActive(false);
        timerText.text = "대기중";

        SetSelected(false);

        if (spawnedFood != null)
        {
            Destroy(spawnedFood);
            spawnedFood = null;
        }

        if (cookButton != null) cookButton.gameObject.SetActive(false);
    }

    public Dictionary<string, int> GetPendingIngredientsCopy()
    {
        return pendingIngredients != null
            ? new Dictionary<string, int>(pendingIngredients)
            : new Dictionary<string, int>();
    }

    // 👇 여기부터가 복구된 세이브/로드 관련 필수 함수들입니다 👇

    // GameSaveManager에서 사용: 현재 조리 중인 재료 가져오기
    public Dictionary<string, int> GetRawIngredientsCopy() => currentIngredients != null ? new Dictionary<string, int>(currentIngredients) : new Dictionary<string, int>();

    // GameSaveManager에서 사용: 남은 조리 시간 가져오기
    public float GetCookTimeRemaining() => cookTimeRemaining;

    // GameSaveManager에서 사용: 세이브 데이터로부터 상태 복원 (이미지 로직 포함하여 수정됨)
    public void RestoreFromSave(StoveSlotSaveData data)
    {
        ResetSlot();  // 기존 상태 초기화

        if (data.isCooked)
        {
            // 조리 완료 상태 복원
            currentIngredients = new Dictionary<string, int>(data.currentIngredients);
            isCooked = true;
            isCooking = false;
            timerText.text = "완료!";

            // 완료 상태에서는 웍 이미지가 꺼져야 함 (음식이 생성되므로)
            wokImage.gameObject.SetActive(false);

            // 음식 생성
            if (spawnedFood != null) Destroy(spawnedFood);
            spawnedFood = Instantiate(cookedFoodPrefab, cookedFoodSpawnPoint);
            spawnedFood.transform.localPosition = Vector3.zero;

            var foodUI = spawnedFood.GetComponent<CookedFoodUI>();
            foodUI.Initialize(currentIngredients);
            foodUI.originStoveSlot = this;
        }
        else if (data.isCooking)
        {
            // 조리 중 상태 복원
            currentIngredients = new Dictionary<string, int>(data.currentIngredients);
            cookTimeSeconds = 5 * 60f; // 기본 조리시간
            cookTimeRemaining = data.cookTimeRemaining;
            isCooking = true;
            isCooked = false;

            // 조리 중 이미지 복원
            SetWokState(wokLidSprite);
            UpdateTimerDisplay();
        }
        else if (data.pendingIngredients != null && data.pendingIngredients.Count > 0)
        {
            // 대기 중(재료만 담긴) 상태 복원
            pendingIngredients = new Dictionary<string, int>(data.pendingIngredients);
            isCooking = false;
            isCooked = false;

            // 재료 담긴 이미지 복원
            SetWokState(wokIngredientsSprite);
            timerText.text = "준비중";

            if (cookButton != null) cookButton.gameObject.SetActive(true);
        }
        else
        {
            // 빈 상태
            wokImage.gameObject.SetActive(false);
        }
    }
}