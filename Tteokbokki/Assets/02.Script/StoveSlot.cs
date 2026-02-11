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

    // ✨ [NEW] 조리 진행도 표시용 슬라이더
    public Slider cookProgressSlider;

    // ✨ Image 컴포넌트 제어 (Sprite 교체용)
    public Image wokImage;
    public Image wokOverlayImage;  // ✨ [NEW] 위에 겹쳐질 이미지 (완성될 상태, 서서히 나타남)
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

    private AudioSource boilingSoundSource;

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

        if (cookProgressSlider != null)
        {
            cookProgressSlider.gameObject.SetActive(false);
            cookProgressSlider.value = 0; // 초기화
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

        if (selected)
        {
            // 선택됨: 아무것도 없는 상태라면 '빈 웍' 보여주기
            if (!isCooking && !isCooked && pendingIngredients.Count == 0)
            {
                SetWokState(wokEmptySprite);
                AudioManager.Instance.PlaySFX(119);
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

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(102, 0.2f);

        pendingIngredients[name]++;

        // 재료가 들어갔으므로 '재료 담긴 웍' 이미지로 변경
        SetWokState(wokIngredientsSprite);

        if (timerText != null) timerText.text = "준비중";
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

        if (timerText != null) timerText.text = "대기중";
        if (cookButton != null) cookButton.gameObject.SetActive(false);

        UpdateInfoUI();
    }

    // --- 3. 조리 시작 ---
    public void TryStartCooking()
    {
        if (pendingIngredients.Count == 0) return;

        // ✨ [핵심] 기존 CheckRecipe 대신 IdentifyMenu를 호출해야 로그가 뜹니다!
        string menuResult = PlayerWokManager.Instance.IdentifyMenu(pendingIngredients);

        // 결과에 따라 처리
        if (menuResult == "Invalid")
        {
            Debug.LogWarning("필수 재료 부족 또는 소스 없음");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "기본 재료가 부족하거나 소스가 없습니다!", 1f);
            return;
        }

        // 조리 시작 (메뉴 이름을 넘겨줌)
        StartCooking(pendingIngredients, 5 * 60f, menuResult);

        pendingIngredients.Clear();
        if (cookButton != null) cookButton.gameObject.SetActive(false);
    }

    private void StartCooking(Dictionary<string, int> ingredients, float cookTime, string menuName)
    {
        currentIngredients = new Dictionary<string, int>(ingredients);
        cookTimeSeconds = cookTime;
        cookTimeRemaining = cookTime;

        isCooking = true;
        isCooked = false;

        // --- 이미지 세팅 ---
        // 1. 아래쪽 이미지: 공통된 '희멀건한' 시작 이미지
        wokImage.sprite = StoveManager.Instance.commonRawSprite;
        wokImage.gameObject.SetActive(true);

        // 2. 위쪽 오버레이 이미지: 목표 이미지 결정
        Sprite targetSprite;
        if (string.IsNullOrEmpty(menuName) || menuName == "Ruined")
        {
            targetSprite = StoveManager.Instance.ruinedCookingSprite;
        }
        else
        {
            // 매니저에게 메뉴 이름으로 이미지 달라고 요청
            targetSprite = StoveManager.Instance.GetCookingSprite(menuName);
        }

        // 3. 오버레이 초기화 (투명도 0)
        if (wokOverlayImage != null)
        {
            wokOverlayImage.sprite = targetSprite;
            wokOverlayImage.gameObject.SetActive(true);

            // 투명하게 시작
            Color c = wokOverlayImage.color;
            c.a = 0f;
            wokOverlayImage.color = c;
        }

        // ✨ [수정/확인] 조리 시작 시 슬라이더를 반드시 켜줘야 합니다!
        if (cookProgressSlider != null)
        {
            cookProgressSlider.gameObject.SetActive(true); // 👈 이 줄이 필수입니다!
            cookProgressSlider.value = 0f;
            cookProgressSlider.maxValue = 1f;
        }

        // 🔥 [사운드] 1. 조리 시작 '치이익' 소리는 화구별로 나도 됨 (ID 105)
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(105);

        // 🔥 [사운드] 2. 끓는 소리는 매니저에게 요청
        StoveManager.Instance.NotifyCookingStarted();

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

        // 진행률 (0 ~ 1)
        float progress = 0f;
        if (cookTimeSeconds > 0)
        {
            progress = 1f - (cookTimeRemaining / cookTimeSeconds);
            progress = Mathf.Clamp01(progress);
        }

        // 1. 오버레이 이미지 알파값 적용
        if (wokOverlayImage != null)
        {
            Color c = wokOverlayImage.color;
            c.a = progress;
            wokOverlayImage.color = c;
        }

        // ✨ [NEW] 슬라이더 진행률 갱신
        if (cookProgressSlider != null)
        {
            cookProgressSlider.value = progress;
        }

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
        if (isCooking) StoveManager.Instance.NotifyCookingEnded();

        isCooking = false;
        isCooked = true;
        if (timerText != null) timerText.text = "완료!";

        // ✨ [NEW] 조리 완료 시 슬라이더 숨김
        if (cookProgressSlider != null) cookProgressSlider.gameObject.SetActive(false);

        // 화구 이미지는 숨김 (CookedFoodUI가 대신 보여줌)
        if (wokImage != null) wokImage.gameObject.SetActive(false);
        if (wokOverlayImage != null) wokOverlayImage.gameObject.SetActive(false);

        if (spawnedFood != null) Destroy(spawnedFood);

        spawnedFood = Instantiate(cookedFoodPrefab, cookedFoodSpawnPoint);
        spawnedFood.transform.localPosition = Vector3.zero;

        // ✨ [핵심 수정] 메뉴 이름을 식별하여 매니저에게 '완성된 이미지'를 요청
        string menuName = PlayerWokManager.Instance.IdentifyMenu(currentIngredients);
        Sprite finishedSprite;

        if (menuName == "Invalid" || menuName == "Ruined")
        {
            // 재료가 이상하거나 망했으면 '망한 완성 이미지' 사용
            finishedSprite = StoveManager.Instance.ruinedFinishedSprite;
        }
        else
        {
            // 정상 메뉴라면 해당 메뉴의 '완성 이미지' 요청
            finishedSprite = StoveManager.Instance.GetFinishedSprite(menuName);
        }

        // CookedFoodUI 초기화 및 이미지 전달
        var foodUI = spawnedFood.GetComponent<CookedFoodUI>();
        if (foodUI != null)
        {
            // 여기서 finishedSprite가 CookedFoodUI의 WokImage로 들어감
            foodUI.Initialize(currentIngredients, finishedSprite);
            foodUI.originStoveSlot = this;
        }

        // 사운드 재생
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(106);

        onCookComplete?.Invoke(currentIngredients);
        UpdateInfoUI();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(cookTimeRemaining / 60);
        int seconds = Mathf.FloorToInt(cookTimeRemaining % 60);
        if (timerText != null) timerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public void NotifyFoodPickedUp()
    {
        spawnedFood = null;
        ResetSlot();
    }

    public void ResetSlot()
    {
        // 🔥 [사운드] 요리 도중에 리셋(초기화)되는 경우에도 소리 카운트를 줄여야 함
        if (isCooking)
        {
            StoveManager.Instance.NotifyCookingEnded();
        }
        isCooked = false;
        isCooking = false;
        currentIngredients = null;
        pendingIngredients.Clear();
        onCookComplete = null;

        if (wokImage != null) wokImage.gameObject.SetActive(false);
        if (wokOverlayImage != null) wokOverlayImage.gameObject.SetActive(false); // 이거 추가!

        // ✨ [NEW] 리셋 시 슬라이더 숨김
        if (cookProgressSlider != null) cookProgressSlider.gameObject.SetActive(false);

        if (timerText != null) timerText.text = "대기중";

        // ✨ [핵심 수정] 단순히 내 상태만 끄는 게 아니라, 매니저에게 해제 요청을 보냅니다.
        // 내가 현재 선택된 슬롯이라면 -> 매니저를 통해 정식으로 해제 (UI 갱신 포함)
        if (stoveManager != null && stoveManager.IsSelected(this))
        {
            stoveManager.DeselectCurrentSlot();
        }
        else
        {
            // 선택된 상태가 아니었다면 그냥 비주얼만 끔
            SetSelected(false);
        }

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
            if (timerText != null) timerText.text = "완료!";

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

            // ✨ [NEW] 로드 시 조리 중이면 슬라이더 켜기
            if (cookProgressSlider != null)
            {
                cookProgressSlider.gameObject.SetActive(true);
                cookProgressSlider.maxValue = 1f;
                cookProgressSlider.value = 1f - (cookTimeRemaining / cookTimeSeconds);
            }

            // 🔥 [사운드] 로드 시 '조리 중'이었다면 끓는 소리 다시 켜주기 (선택 사항)
            // 만약 로드했을 때 조용하길 원하면 이 부분은 빼셔도 됩니다.
            if (AudioManager.Instance != null)
                boilingSoundSource = AudioManager.Instance.PlayLoopSFX(101);
        }
        else if (data.pendingIngredients != null && data.pendingIngredients.Count > 0)
        {
            // 대기 중(재료만 담긴) 상태 복원
            pendingIngredients = new Dictionary<string, int>(data.pendingIngredients);
            isCooking = false;
            isCooked = false;

            // 재료 담긴 이미지 복원
            SetWokState(wokIngredientsSprite);
            if (timerText != null) timerText.text = "준비중";

            if (cookButton != null) cookButton.gameObject.SetActive(true);
        }
        else
        {
            // 빈 상태
            wokImage.gameObject.SetActive(false);
        }
    }

    //튜토리얼용
    public void StartTutorialCook()
    {
        currentIngredients = new Dictionary<string, int>(pendingIngredients);
        pendingIngredients.Clear();

        // --- 수정된 부분 ---
        float speedMultiplier = (60f / 3f); // 현재 웍의 배속 (20)
        float targetRealTime = 5f;         // 현실 시간 (10초 아님 -5초)

        // 인게임 타이머 기준으로는 200초를 넣어줘야 현실에서 10초 동안 흐릅니다.
        cookTimeSeconds = targetRealTime * speedMultiplier;
        cookTimeRemaining = cookTimeSeconds;
        // ------------------

        isCooking = true;
        isCooked = false;

        if (wokImage != null)
        {
            wokImage.sprite = wokLidSprite;
            wokImage.gameObject.SetActive(true);
        }

        // ✨ [NEW] 튜토리얼 조리 시에도 슬라이더 켜기
        if (cookProgressSlider != null)
        {
            cookProgressSlider.gameObject.SetActive(true);
            cookProgressSlider.value = 0f;
            cookProgressSlider.maxValue = 1f;
        }

        if (cookButton != null) cookButton.gameObject.SetActive(false);
        UpdateInfoUI();
    }

}