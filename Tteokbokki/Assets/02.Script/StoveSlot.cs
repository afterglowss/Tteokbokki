using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public class StoveSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject wokIcon;
    public GameObject selectedHighlight;
    public Button cookButton;

    [Header("Spawn Settings")]
    public Transform cookedFoodSpawnPoint;
    public GameObject cookedFoodPrefab;

    // --- 내부 상태 변수 ---
    private float cookTimeSeconds;
    private float cookTimeRemaining;
    private bool isCooking = false;
    private bool isCooked = false;

    // 생성된 음식 오브젝트 참조
    private GameObject spawnedFood;

    // 현재 조리 중인 재료 (조리 시작 후 확정된 데이터)
    private Dictionary<string, int> currentIngredients;

    // 조리 전 담고 있는 재료 (대기 중인 데이터)
    private Dictionary<string, int> pendingIngredients = new Dictionary<string, int>();

    private Action<Dictionary<string, int>> onCookComplete;
    private StoveManager stoveManager;

    // 드래그 관련 변수
    private Canvas canvas;
    private Vector3 originalIconPos;
    private Transform originalIconParent;
    private CanvasGroup iconCanvasGroup;

    // --- 프로퍼티 ---
    public bool IsCooking => isCooking;
    public bool IsCooked => isCooked;
    // 재료가 담겨있고, 조리중이 아니고, 완료된 음식도 없는 상태여야 드래그 가능
    public bool HasPendingIngredients => pendingIngredients.Count > 0 && !isCooking && !isCooked;

    public void Initialize(StoveManager manager)
    {
        stoveManager = manager;
        // 드래그 처리를 위해 Canvas 찾기
        canvas = GetComponentInParent<Canvas>();

        // Wok Icon 초기 설정
        if (wokIcon != null)
        {
            iconCanvasGroup = wokIcon.GetComponent<CanvasGroup>();
            if (iconCanvasGroup == null) iconCanvasGroup = wokIcon.AddComponent<CanvasGroup>();

            // 처음엔 꺼둠 (빈 상태)
            wokIcon.SetActive(false);
        }
        if (cookButton != null)
        {
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(TryStartCooking); // 버튼 누르면 조리 시도
            cookButton.gameObject.SetActive(false); // 처음엔 숨김
        }
    }

    // --- 1. 화구 선택 로직 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (stoveManager != null)
        {
            stoveManager.SelectSlot(this);
            // 선택될 때 내 재료 상태를 UI에 표시하라고 알림
            PlayerWokManager.Instance.UpdateUI(pendingIngredients);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);

        // 선택되었을 때만 UI 업데이트
        if (selected)
        {
            PlayerWokManager.Instance.UpdateUI(pendingIngredients);
        }
    }

    // --- 2. 재료 추가 및 관리 (StoveManager가 호출) ---
    public void AddIngredient(string name)
    {
        // 조리 중이거나 음식이 나와있으면 재료 추가 불가
        if (isCooking || isCooked) return;

        if (!pendingIngredients.ContainsKey(name))
            pendingIngredients[name] = 0;

        pendingIngredients[name]++;

        // 재료가 들어갔으니 웍 아이콘 표시
        wokIcon.SetActive(true);
        timerText.text = "준비중";

        if (cookButton != null) cookButton.gameObject.SetActive(true);

        // 현재 선택된 상태라면 UI 즉시 갱신
        if (stoveManager.IsSelected(this))
        {
            PlayerWokManager.Instance.UpdateUI(pendingIngredients);
        }
    }

    public void ClearPending()
    {
        // 조리 중에는 비울 수 없음
        if (isCooking) return;

        pendingIngredients.Clear();
        wokIcon.SetActive(false); // 재료 없으면 숨김
        timerText.text = "대기중";

        if (cookButton != null) cookButton.gameObject.SetActive(false);

        if (stoveManager.IsSelected(this))
        {
            PlayerWokManager.Instance.UpdateUI(pendingIngredients);
        }
    }

    // --- 3. 조리 시작 시도 (조리 버튼이 호출) ---
    public void TryStartCooking()
    {
        if (pendingIngredients.Count == 0)
        {
            Debug.LogWarning("재료가 없습니다.");
            return;
        }

        // 레시피 검증 (PlayerWokManager의 검증 로직 사용)
        if (PlayerWokManager.Instance.CheckRecipe(pendingIngredients))
        {
            // 펜딩 재료를 확정 재료로 넘기고 조리 시작
            StartCooking(pendingIngredients, 5 * 60f, null);
            pendingIngredients.Clear(); // 펜딩은 비움

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

        wokIcon.SetActive(true);
        if (cookButton != null) cookButton.gameObject.SetActive(false);
        UpdateTimerDisplay();
    }

    // --- 4. 드래그 앤 드롭 (휴지통 버리기용) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasPendingIngredients) return; // 재료 없거나 요리중이면 드래그 불가

        // 웍 아이콘만 떼어서 드래그하는 연출
        originalIconPos = wokIcon.transform.localPosition;
        originalIconParent = wokIcon.transform.parent;

        wokIcon.transform.SetParent(canvas.transform); // 캔버스 최상단으로 이동
        if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = false; // 드랍 감지를 위해 레이캐스트 끄기
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!HasPendingIngredients) return;
        wokIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!HasPendingIngredients) return;

        if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = true;

        // 드랍 처리는 TrashBinSlot에서 수행됨.
        // 여기서 별도 처리가 안 되었으면(휴지통이 아니면) 원위치 복귀
        // 만약 TrashBinSlot에서 ClearPending()을 호출했다면 wokIcon은 꺼지게(SetActive false) 됨.

        wokIcon.transform.SetParent(originalIconParent);
        wokIcon.transform.localPosition = originalIconPos;
    }

    // --- Update 및 조리 완료 로직 ---
    void Update()
    {
        if (!isCooking) return;

        cookTimeRemaining -= Time.deltaTime * (60f / 3f); // 3배속 (게임 설정에 따라 조절)
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
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(cookTimeRemaining / 60);
        int seconds = Mathf.FloorToInt(cookTimeRemaining % 60);
        timerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public void ResetSlot()
    {
        isCooked = false;
        isCooking = false;
        currentIngredients = null;
        pendingIngredients.Clear(); // 펜딩도 초기화
        onCookComplete = null;
        wokIcon.SetActive(false);
        timerText.text = "대기중";
        SetSelected(false);

        if (spawnedFood != null)
        {
            Destroy(spawnedFood);
            spawnedFood = null;
        }

        if (cookButton != null) cookButton.gameObject.SetActive(false);
    }

    // --- 외부 접근용 Getter ---
    public Dictionary<string, int> GetRawIngredientsCopy() => currentIngredients != null ? new Dictionary<string, int>(currentIngredients) : new Dictionary<string, int>();

    // ✨ OrderChecker와 SaveManager에서 필요한 Getter
    public Dictionary<string, int> GetPendingIngredientsCopy()
    {
        return pendingIngredients != null
            ? new Dictionary<string, int>(pendingIngredients)
            : new Dictionary<string, int>();
    }

    public float GetCookTimeRemaining() => cookTimeRemaining;

    // --- 툴팁 로직 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 조리 중일 때 툴팁
        if (isCooking && currentIngredients != null)
        {
            string tooltip = "조리 중:\n";
            foreach (var pair in currentIngredients)
            {
                tooltip += $"{pair.Key} x{pair.Value}\n";
            }
            TooltipManager.ShowFollowMouse(TooltipType.Info, tooltip);
        }
        // ✨ 대기 중일 때(재료 담는 중) 툴팁
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

    // --- 세이브/로드 복원 로직 ---
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
            wokIcon.SetActive(true);

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
            cookTimeSeconds = 5 * 60f; // 기본 조리시간 (필요 시 저장 데이터에 포함)
            cookTimeRemaining = data.cookTimeRemaining;
            isCooking = true;
            isCooked = false;

            wokIcon.SetActive(true);
            UpdateTimerDisplay();
        }
        else if (data.pendingIngredients != null && data.pendingIngredients.Count > 0)
        {
            // ✨ 대기 중(재료만 담긴) 상태 복원
            pendingIngredients = new Dictionary<string, int>(data.pendingIngredients);
            isCooking = false;
            isCooked = false;
            wokIcon.SetActive(true);
            timerText.text = "준비중";

            if (cookButton != null) cookButton.gameObject.SetActive(true);
        }
        else
        {
            // 빈 상태
            wokIcon.SetActive(false);
        }
    }
}