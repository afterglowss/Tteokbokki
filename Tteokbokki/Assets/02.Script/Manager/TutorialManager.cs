using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;


public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public static bool IsFreeze { get; private set; } = true;
    public bool IsTutorial { get; private set; } = false;

    [Serializable]
    public struct TutorialObjectMapping
    {
        public string objectID;   // Yarn에서 부를 별명 (예: "SuccessTitle")
        public GameObject target; // 실제 오브젝트 (꺼져있어도 됨)
    }


    [Header("하이라이트 레이어 설정")]
    [SerializeField] private GameObject darkOverlay;
    [SerializeField] private GameObject yellowOutlinePrefab;

    [Header("가이드 UI 설정")]
    [SerializeField] private RectTransform guideLayer;
    [SerializeField] private GameObject mousePrefeb;

    private GameObject _currentDragGuide;

    [Header("인게임 매니저 참조")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private ReceiptLineManager receiptLineManager;
    [SerializeField] private PackagingAreaManager packagingArea;
    [SerializeField] private StoveSlot targetStoveSlot;
    [SerializeField] private Button endDayButton;
    [SerializeField] private Button paymentButton;

    [Header("마감 패널 상세 참조")]
    [SerializeField] private GameObject endOfDayParent;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject closingPanel;

    [Header("UI 및 배경 전용")]
    [SerializeField] private Image backgroundDisplay;
    [SerializeField] private Texture2D desktopTexture;
    public CanvasGroup gameplayUI;

    // --- 내부 데이터 관리 ---
    private Dictionary<string, int> _tutorialTally = new Dictionary<string, int>();
    private List<HighlightData> _activeHighlights = new List<HighlightData>();
    private GameObject _lastTarget;

    private struct HighlightData
    {
        public GameObject target;
        public GameObject outlineInstance;
        public Canvas addedCanvas;
        public GraphicRaycaster addedRaycaster;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeTutorial();
        IsTutorial = true;
    }

    private void Start()
    {
        // 시계 UI 숨기기
        if (GameClock.Instance != null && GameClock.Instance.dateTimeText != null)
        {
            GameClock.Instance.dateTimeText.gameObject.SetActive(false);
        }

        if (OrderSpawner.Instance != null)
        {
            OrderSpawner.Instance.StopSpawning();
        }
    }

    private void OnDisable()
    {
        // 씬이 바뀌거나 오브젝트가 꺼질 때 모든 시간 흐름을 정상화합니다.
        IsFreeze = false;
        GameClock.Resume();
    }

    private void OnDestroy()
    {
        IsFreeze = false;
        GameClock.Resume();
    }

    private void InitializeTutorial()
    {
        // 1. 얀 함수 등록 (GameSaveManager와 연결)
        //dialogueRunner?.AddFunction("getSawTutorial", () => GameSaveManager.Instance.IsTutorialCompleted);

        OrderSpawner.Instance?.StopSpawning();
        darkOverlay?.SetActive(false);

        // GameClock 일시정지
        GameClock.Pause();
        SetSystemFreeze(true);

        InitKeyboardForTutorial();

        if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.StartDialogue("TutorialStart");
        }
    }

    public void SetSystemFreeze(bool isFreeze)
    {
        // 1. 기존 로직 (상태 저장)
        IsFreeze = isFreeze;

        // 2. 물리적 클릭 차단 추가 
        if (gameplayUI != null)
        {
            gameplayUI.blocksRaycasts = !isFreeze;
        }
    }

    // ==========================================
    // 5. 튜토리얼 상태 관리
    // ==========================================

    [YarnCommand("setIsTutorial")]
    public static void SetIsTutorial(bool active)
    {
        if (Instance != null) Instance.IsTutorial = active;
    }

    [YarnCommand("loadScene")]
    public static void LoadScene(string sceneName)
    {
        Debug.Log($"[튜토리얼] {sceneName} 씬으로 이동합니다.");

        if (Instance != null) Instance.IsTutorial = false;
        // GameSaveManager.Instance.SetTutorialComplete();

        // 다음 씬이 멈추지 않도록 시간 설정을 완전히 초기화합니다.
        GameClock.Resume();

        SceneManager.LoadScene(sceneName);
    }

    // ==========================================
    // 1. 하이라이트 및 UI 제어
    // ==========================================

    // 1. 얀에서 이름으로 부를 때 (기존 방식 유지)
    [YarnCommand("highlight")]
    public static void Highlight(string objName)
    {
        GameObject target = GameObject.Find(objName);
        if (target != null) Highlight(target); // 아래 함수를 호출하게 바꿉니다.
    }

    // 2. 🚨 [추가] 특정 오브젝트를 직접 넣을 때 (에러 방지용)
    public static void Highlight(GameObject target)
    {
        var inst = Instance;
        if (inst == null || target == null) return;

        // 이미 하이라이트 되어 있다면 중복 방지
        if (inst._activeHighlights.Exists(h => h.target == target)) return;

        // Canvas Override (그리드 유지용)
        Canvas canvas = target.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5;

        GraphicRaycaster raycaster = target.AddComponent<GraphicRaycaster>();

        HighlightData data = new HighlightData
        {
            target = target,
            addedCanvas = canvas,
            addedRaycaster = raycaster,
            outlineInstance = null
        };

        if (inst.darkOverlay != null) inst.darkOverlay.SetActive(true);

        // 2. [수정] 노란 테두리 생성 로직
        if (inst.yellowOutlinePrefab != null)
        {
            GameObject outline = Instantiate(inst.yellowOutlinePrefab, target.transform);
            data.outlineInstance = outline;

            // 테두리도 무대 위로 (마스크 돌파)
            Canvas outlineCanvas = outline.AddComponent<Canvas>();
            outlineCanvas.overrideSorting = true;
            outlineCanvas.sortingOrder = 6;

            if (outline.TryGetComponent<Image>(out var img)) img.raycastTarget = false;

            RectTransform targetRT = target.GetComponent<RectTransform>();
            RectTransform outlineRT = outline.GetComponent<RectTransform>();

            if (outlineRT != null && targetRT != null)
            {
                // 🔥 [핵심] 피벗이 어디든 상관없이 '부모의 모서리'에 앵커를 박습니다.
                outlineRT.anchorMin = Vector2.zero; // (0, 0) 좌하단
                outlineRT.anchorMax = Vector2.one;  // (1, 1) 우상단

                // 🔥 타겟 버튼의 피벗을 그대로 복사해서 위치 엇나감을 방지합니다.
                outlineRT.pivot = targetRT.pivot;

                // 🔥 '0'으로 초기화하면 버튼 크기와 1:1로 일치하게 됩니다.
                outlineRT.anchoredPosition = Vector2.zero;

                // 🔥 이제 이 값만 조절해서 테두리 두께(여백)를 결정합니다.
                float padding = 6f;
                outlineRT.offsetMin = new Vector2(-padding, -padding); // 좌측, 하단 여백
                outlineRT.offsetMax = new Vector2(padding, padding);   // 우측, 상단 여백

                outlineRT.localScale = Vector3.one;
            }
        }

        inst._activeHighlights.Add(data);
        inst.SetSystemFreeze(true);
    }

    // 2. 개별 해제 (범용: 이름만 주면 하이라이트랑 클릭 권한만 딱 회수)
    [YarnCommand("unhighlight")]
    public static void Unhighlight(string objName = "")
    {
        var inst = Instance;
        if (inst == null) return;

        // 이름이 없으면 전체 해제, 이름이 있으면 해당 오브젝트만 해제
        if (string.IsNullOrEmpty(objName))
        {
            for (int i = inst._activeHighlights.Count - 1; i >= 0; i--)
                inst.ClearHighlightData(inst._activeHighlights[i]);
            inst._activeHighlights.Clear();
            if (inst.darkOverlay != null) inst.darkOverlay.SetActive(false);
        }
        else
        {
            var data = inst._activeHighlights.Find(x => x.target != null && x.target.name == objName);
            if (data.target != null)
            {
                inst.ClearHighlightData(data);
                inst._activeHighlights.Remove(data);
            }
        }
    }

    // 컴포넌트 파괴용 내부 헬퍼
    private void ClearHighlightData(HighlightData data)
    {
        if (data.outlineInstance != null) Destroy(data.outlineInstance);
        if (data.addedRaycaster != null) Destroy(data.addedRaycaster);
        if (data.addedCanvas != null) Destroy(data.addedCanvas);
    }
    // 내부 헬퍼: 특정 버튼의 하이라이트(클릭 권한)만 회수
    private static void RemoveHighlightComps(string objName)
    {
        var h = Instance._activeHighlights.Find(x => x.target != null && x.target.name.Contains(objName));
        if (h.target != null)
        {
            if (h.outlineInstance != null) Destroy(h.outlineInstance);
            if (h.addedRaycaster != null) Destroy(h.addedRaycaster);
            if (h.addedCanvas != null) Destroy(h.addedCanvas);
            Instance._activeHighlights.Remove(h);
        }
    }


    // ==========================================
    // 2. 배경 및 사운드 설정
    // ==========================================

    [YarnCommand("set_bg")]
    public static void SetBackground(string bgName)
    {
        if (Instance == null || Instance.backgroundDisplay == null) return;

        // 1. 빈 문자열("")이거나 "None"이면 false, 그 외(Start 등)는 true
        bool shouldActive = !string.IsNullOrEmpty(bgName) && bgName != "None";
        Instance.backgroundDisplay.gameObject.SetActive(shouldActive);
    }

    [YarnCommand("playSFX")]
    public static void PlaySFX(int id) => AudioManager.Instance?.PlaySFX(id);


    // ==========================================
    // 진행 함수들
    // ==========================================

    [YarnCommand("spawnTutorialReceipt")]
    public static void SpawnTutorialReceipt()
    {
        if (Instance == null || Instance.receiptLineManager == null) return;

        // 1. 영수증 생성 및 데이터 설정
        Receipt tutorialReceipt = new Receipt(DateTime.Now, 1);
        tutorialReceipt.AddOrder("군자 떡볶이", new Dictionary<string, int>());
        Instance.receiptLineManager.AddNewReceipt(tutorialReceipt);

        // 2. 생성된 영수증 오브젝트 찾기
        var slots = Instance.receiptLineManager.GetReceiptSlots();
        if (slots.Count > 0)
        {
            var lastItem = slots[slots.Count - 1];
            // 🚨 이름을 바꿔서 나중에 Highlight나 GameObject.Find로 찾기 쉽게 만듭니다.
            lastItem.gameObject.name = "TutorialReceipt";

            // 🚨 [수정] 여기서 RemoveAllListeners()를 절대 하지 않습니다! 
            // 영수증 클릭 시 원래 창이 뜨는 기능은 그대로 유지해야 하니까요.
        }
    }

    [YarnCommand("spawnWok")]
    public static void SpawnWok()
    {
        var inst = Instance;
        if (inst == null || inst.targetStoveSlot == null) return;

        // 1. StoveManager에게 먼저 알려서 시스템 상태를 업데이트합니다.
        StoveManager.Instance.SelectSlot(inst.targetStoveSlot);

        // 2. 그 다음 직접 SetSelected를 호출하여 비주얼을 갱신합니다.
        inst.targetStoveSlot.SetSelected(true);
    }

    // 매개변수로 웍의 데이터를 받도록 수정
    private bool CheckRecipeCondition(Dictionary<string, int> wokData)
    {
        return wokData.GetValueOrDefault("떡", 0) >= 2 &&
               wokData.GetValueOrDefault("오뎅", 0) >= 2 &&
               wokData.GetValueOrDefault("파", 0) >= 1 &&
               wokData.GetValueOrDefault("양배추", 0) >= 1;
    }


    // ==========================================
    // Wait For - 플레이어 상호작용 함수들
    // ==========================================

    [YarnCommand("waitForReceiptClick")]
    public static IEnumerator WaitForReceiptClick()
    {
        var inst = Instance;
        if (inst == null) yield break;

        GameObject receipt = GameObject.Find("TutorialReceipt");
        if (receipt == null) yield break;

        Button btn = receipt.GetComponent<Button>();
        if (btn == null) yield break;

        bool isClicked = false;
        // 튜토리얼 전용 리스너만 살짝 추가
        UnityEngine.Events.UnityAction tutorialAction = () => isClicked = true;

        btn.onClick.AddListener(tutorialAction);
        inst.SetSystemFreeze(false); // 클릭할 수 있게 해동

        // 클릭할 때까지 Yarn Runner 대기
        yield return new WaitUntil(() => isClicked);

        // 볼일 끝났으니 리스너 제거
        btn.onClick.RemoveListener(tutorialAction);

        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }


    [YarnCommand("waitIngredients")]
    public static IEnumerator WaitIngredients()
    {
        var inst = Instance;
        if (inst == null) yield break;

        AllowIngredientKey("떡");
        AllowIngredientKey("오뎅");
        AllowIngredientKey("파");
        AllowIngredientKey("양배추");

        // 🚨 여기서 해동하지 않습니다! 
        // highlight된 버튼들만 자기 자신의 GraphicRaycaster 덕분에 클릭이 되는 상태입니다.

        bool tteokDone = false, odengDone = false, onionDone = false, cabbageDone = false;

        while (true)
        {
            var wok = inst.targetStoveSlot.GetPendingIngredientsCopy();

            // 떡 2개 완료 체크 -> 완료 
            if (!tteokDone && wok.GetValueOrDefault("떡", 0) >= 2)
            {
                RemoveHighlightComps("떡_Button");
                tteokDone = true;
                ConstrainIngredientKey("떡");
            }
            // 오뎅 2개 완료 체크
            if (!odengDone && wok.GetValueOrDefault("오뎅", 0) >= 2)
            {
                RemoveHighlightComps("오뎅_Button");
                odengDone = true;
                ConstrainIngredientKey("오뎅");
            }
            // 파, 양배추도 같은 방식으로 처리
            if (!onionDone && wok.GetValueOrDefault("파", 0) >= 1)
            {
                RemoveHighlightComps("파_Button");
                onionDone = true;
                ConstrainIngredientKey("파");
            }
            if (!cabbageDone && wok.GetValueOrDefault("양배추", 0) >= 1)
            {
                RemoveHighlightComps("양배추_Button");
                cabbageDone = true;
                ConstrainIngredientKey("양배추");
            }

            if (inst.CheckRecipeCondition(wok)) break;
            yield return null;
        }

        Unhighlight();
        yield return new WaitForEndOfFrame();
    }
    
    [YarnCommand("waitSauce")]
    public static IEnumerator WaitSauce()
    {
        var inst = Instance;
        // inst.targetStoveSlot이 인스펙터에서 제대로 할당되어 있는지 확인
        if (inst == null || inst.targetStoveSlot == null) yield break;

        AllowIngredientKey("군자 소스");

        // 1. [해동] 키보드 단축키가 먹히려면 시스템 잠금이 풀려있어야 합니다.
        inst.SetSystemFreeze(false);

        // 2. [하이라이트] 유저가 어디를 봐야 할지 알려줍니다.
        Highlight("군자 소스_Button");


        // 3. 버튼 리스너 다 버리고, 화구 내부의 재료 리스트를 직접 감시합니다.
        yield return new WaitUntil(() => {
            // 화구에 담긴 재료 데이터를 복사해서 가져옵니다.
            var wokData = inst.targetStoveSlot.GetPendingIngredientsCopy();

            // (키보드 단축키로 넣어도 wokData에는 데이터가 기록되므로 즉시 감지됩니다.)
            return wokData.GetValueOrDefault("군자 소스", 0) >= 1;
        });

        // 4. [동결 및 정리] 다음 대화를 위해 다시 잠그고 하이라이트를 끕니다.
        inst.SetSystemFreeze(true);
        ConstrainIngredientKey("군자 소스");
        Unhighlight();

        yield return new WaitForEndOfFrame();
    }


    [YarnCommand("waitStartBoiling")]
    public static IEnumerator WaitStartBoiling()
    {
        var inst = Instance;

        KeyboardInputManager.Instance.AllowKey(Key.Space);

        if (inst == null || inst.targetStoveSlot == null) yield break;

        Highlight("CookButton");

        inst.SetSystemFreeze(false);
        yield return new WaitUntil(() => inst.targetStoveSlot.IsCooking);

        Debug.Log("[튜토리얼] 조리 시작 감지 완료!");

        inst.SetSystemFreeze(true);
        KeyboardInputManager.Instance.ConstrainKey(Key.Space);
        Unhighlight();

        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForBoiled")]
    public static IEnumerator WaitForBoiled()
    {
        if (Instance?.targetStoveSlot == null) yield break;

        // 조리가 진행되려면 시간이 흘러야 하므로 시간 및 클럭 해제
        // GameClock.Resume();

        // 1. 요리가 '시작'될 때까지 대기
        yield return new WaitUntil(() => Instance.targetStoveSlot.IsCooking);

        // 2. 요리가 '끝날' 때까지 대기
        while (Instance.targetStoveSlot.IsCooking) yield return null;
        
        // 조리 완료 후 다시 시간 정지
        // GameClock.Pause();

        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForPacking")]
    public static IEnumerator WaitForPacking()
    {
        var inst = Instance;
        if (inst == null) yield break;

        // 1. 출발지(화구)와 목적지(포장 슬롯) 오브젝트 탐색
        // 화구는 인스펙터에 할당된 targetStoveSlot을 우선 사용합니다.
        GameObject wok = inst.targetStoveSlot != null ? inst.targetStoveSlot.gameObject : GameObject.Find("Wok_Object");
        GameObject slot = GameObject.Find("PackagingSlot") ?? GameObject.Find("PackagingSlot(Clone)");

        // 2. 가이드 시작 (하이라이트 및 손가락 애니메이션 자동 실행)
        Coroutine guide = inst.ShowDragGuide(wok, slot);

        // 조작 허용
        inst.SetSystemFreeze(false);

        bool isPacked = false;

        // 3. 어떤 포장 슬롯이든 음식이 들어올 때까지 대기
        while (!isPacked)
        {
            // 씬 내의 모든 PackagingSlot 컴포넌트를 탐색
            var allSlots = UnityEngine.Object.FindObjectsByType<PackagingSlot>(FindObjectsSortMode.None);
            foreach (var s in allSlots)
            {
                if (s.HasAnyFood())
                {
                    isPacked = true;
                    break;
                }
            }
            yield return null;
        }

        Debug.Log("[튜토리얼] 음식 포장대 안착 확인!");

        // 4. 가이드 종료 및 정리 (하이라이트 해제 포함)
        inst.HideDragGuide(guide);

        // 다시 조작 잠금
        inst.SetSystemFreeze(true);

        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForReceiptAttached")]
    public static IEnumerator WaitForReceiptAttached()
    {
        var inst = Instance;
        if (inst == null) yield break;

        // 오브젝트 대기
        yield return new WaitUntil(() => GameObject.Find("TutorialReceipt") != null);

        GameObject receipt = GameObject.Find("TutorialReceipt");
        GameObject target = GameObject.Find("CookedFoodUI") ?? GameObject.Find("CookedFoodUI(Clone)");

        // 가이드 시작 (헬퍼 함수 사용)
        Coroutine guide = inst.ShowDragGuide(receipt, target);

        inst.SetSystemFreeze(false);

        // 판정 대기
        yield return new WaitUntil(() => {
            GameObject r = GameObject.Find("TutorialReceipt");
            if (r == null) return true;

            Transform p = r.transform.parent;
            if (p == null) return false;

            return p.name.Contains("Cooked") || p.name.Contains("Food") || p.name.Contains("Slot");
        });

        // 가이드 종료 및 정리 (헬퍼 함수 사용)
        inst.HideDragGuide(guide);

        inst.SetSystemFreeze(true);
        yield return new WaitForEndOfFrame();
    }


    [YarnCommand("waitForPayment")]
    public static IEnumerator WaitForPayment()
    {
        var inst = Instance;
        if (inst == null || inst.paymentButton == null) yield break;

        bool isClicked = false;
        // 기존 리스너를 지우지 않고, 튜토리얼용 체크 함수만 정의합니다.
        UnityEngine.Events.UnityAction tutorialAction = () => isClicked = true;

        // 리스너 추가 (기존 기능 + 튜토리얼 신호)
        inst.paymentButton.onClick.AddListener(tutorialAction);
        inst.SetSystemFreeze(false);

        // 플레이어가 누를 때까지 대기
        yield return new WaitUntil(() => isClicked);

        //튜토리얼 리스너만 제거 (원래 기능은 보존)
        inst.paymentButton.onClick.RemoveListener(tutorialAction);

        // 상점 모드 설정 로직
        var shopUI = FindAnyObjectByType<IngredientShopUI>();
        if (shopUI != null) shopUI.isTutorialMode = true;

        // 만약 수동으로 켜줘야 하는 패널이 있다면 유지
        if (inst.endOfDayParent != null) inst.endOfDayParent.SetActive(true);

        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForEndDay")]
    public static IEnumerator WaitForEndDay()
    {
        var inst = Instance;
        if (inst == null || inst.endDayButton == null) yield break;

        bool isClicked = false;
        UnityEngine.Events.UnityAction tutorialAction = () => isClicked = true;

        //활성화
        inst.endDayButton.gameObject.SetActive(true);
        Highlight(inst.endDayButton.gameObject);

        // 기존 기능을 방해하지 않고 튜토리얼 신호만 추가
        inst.endDayButton.onClick.AddListener(tutorialAction);
        inst.SetSystemFreeze(false);

        yield return new WaitUntil(() => isClicked);
        //시간 on
        // GameClock.Resume();

        if (GameClock.Instance != null && GameClock.Instance.dateTimeText != null)
        {
            GameClock.Instance.dateTimeText.gameObject.SetActive(true);
        }

        // 리스너 제거 및 정리
        inst.endDayButton.onClick.RemoveListener(tutorialAction);

        inst.SetSystemFreeze(true);
        Unhighlight();

        // GameManager에 있는 endOfDayPanel 오브젝트 상태를 감시합니다.
        if (GameManager.Instance != null && GameManager.Instance.endOfDayPanel != null)
        {
            // 패널이 SetActive(true) 될 때까지 대기
            yield return new WaitUntil(() => GameManager.Instance.endOfDayPanel.activeSelf);

            // 애니메이션이 있다면 살짝 여유를 줍니다. (0.2~0.5초)
            yield return new WaitForSecondsRealtime(0.5f);

            // 5. 마감 패널이 떴으니 다시 시간 정지!
            GameClock.Pause();
            Debug.Log("[튜토리얼] 마감 패널 감지 완료. 시간을 다시 멈춥니다.");
        }

        inst.endDayButton.gameObject.SetActive(false);
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForMaraPlusClick")]
    public static IEnumerator WaitForMaraPlusClick()
    {
        var inst = Instance;
        if (inst == null) yield break;


        // 2. 부모(마라 소스)부터 확실히 찾기
        GameObject maraObj = GameObject.Find("마라 소스") ?? GameObject.Find("마라 소스(Clone)");

        // transform.Find는 자식의 자식을 경로로 한 번에 찾을 수 있습니다.
        Transform plusBtnTrans = maraObj.transform.Find("Image_CountBackGround/Button_Plus");

        if (plusBtnTrans == null)
        {
            Debug.LogError("사장님, 마라 소스 안에 'Image_CountBackGround/Button_Plus'가 없는데요? 경로 확인하세요!");
            yield break;
        }

        GameObject plusBtnObj = plusBtnTrans.gameObject;
        Button btn = plusBtnObj.GetComponent<Button>();

        // 4. 이름이 아닌 '오브젝트'를 직접 던져서 하이라이트!
        Highlight(plusBtnObj);

        bool isClicked = false;
        UnityEngine.Events.UnityAction action = () => isClicked = true;
        btn.onClick.AddListener(action);
        inst.SetSystemFreeze(false);

        yield return new WaitUntil(() => isClicked);

        btn.onClick.RemoveListener(action);
        Unhighlight(); // 클릭 끝났으니 정리
        inst.SetSystemFreeze(true);
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForOrderClick")]
    public static IEnumerator WaitForOrderClick()
    {
        var inst = Instance;
        var shopUI = UnityEngine.Object.FindAnyObjectByType<IngredientShopUI>();
        if (inst == null || shopUI == null) yield break;

        bool isOrdered = false;
        inst.SetSystemFreeze(false);

        UnityEngine.Events.UnityAction action = null;
        action = () => {
            isOrdered = true;
            shopUI.OnShopProcessFinished.RemoveListener(action);
        };
        shopUI.OnShopProcessFinished.AddListener(action);

        // ✨ 주문 버튼을 누를 때까지 대기
        yield return new WaitUntil(() => isOrdered);
        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForChecking")]
    public static IEnumerator WaitForChecking()
    {
        var inst = Instance;
        GameObject receiptObj = GameObject.Find("Toggle_CheckReceipt");
        GameObject ingredientObj = GameObject.Find("Toggle_CheckIngredient");
        if (receiptObj == null || ingredientObj == null) yield break;

        Toggle t1 = receiptObj.GetComponent<Toggle>();
        Toggle t2 = ingredientObj.GetComponent<Toggle>();

        inst.SetSystemFreeze(false);
        yield return new WaitUntil(() => t1.isOn && t2.isOn);
        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForClosing")]
    public static IEnumerator WaitForClosing()
    {
        var inst = Instance;
        if (inst == null) yield break;

        GameObject closeBtnObj = GameObject.Find("Button_NextDay");
        if (closeBtnObj == null) yield break;

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        bool isClicked = false;

        // 🚨 튜토리얼 전용 리스너를 이름이 있는 액션으로 정의합니다.
        UnityEngine.Events.UnityAction tutorialAction = null;
        tutorialAction = () => isClicked = true;

        // 1. 리스너 추가 및 시스템 해제
        closeBtn.onClick.AddListener(tutorialAction);
        inst.SetSystemFreeze(false);

        // 2. 클릭될 때까지 대기
        yield return new WaitUntil(() => isClicked);

        // 클릭 직후 튜토리얼 전용 리스너만 제거! (기존 게임 로직은 보존)
        closeBtn.onClick.RemoveListener(tutorialAction);

        // 3. 테두리 즉시 제거 (셔터가 내려오기 전)
        Unhighlight();

        // 4. 셔터 애니메이션 시간 대기
        Debug.Log("[튜토리얼] 다음 날 버튼 클릭 확인. 셔터 대기 중...");
        yield return new WaitForSeconds(1.0f);

        inst.SetSystemFreeze(true);
        yield return new WaitForEndOfFrame();
    }

    //=============================
    // 튜토리얼 가이드 (드래그)
    //=============================

    // 드래그 가이드를 활성화하고 대상들을 하이라이트하는 함수
    private Coroutine ShowDragGuide(GameObject start, GameObject end)
    {
        if (start == null || end == null) return null;

        // 가이드 레이어 설정 (Canvas 및 SortingOrder 확인)
        SetupGuideLayer();

        // 기존 가이드 오브젝트가 있다면 제거 후 새로 생성
        if (_currentDragGuide != null) Destroy(_currentDragGuide);
        _currentDragGuide = Instantiate(mousePrefeb, guideLayer);

        // 출발지와 목적지 오브젝트 하이라이트
        Highlight(start.name);
        Highlight(end.name);

        // 이동 애니메이션 코루틴 시작 및 반환
        return StartCoroutine(ActionGuideRoutine(start, end));
    }

    // 가이드 애니메이션과 오브젝트를 정리하는 함수
    private void HideDragGuide(Coroutine guideCoroutine)
    {
        if (guideCoroutine != null) StopCoroutine(guideCoroutine);

        if (_currentDragGuide != null)
        {
            Destroy(_currentDragGuide);
            _currentDragGuide = null;
        }

        // 모든 하이라이트 해제
        Unhighlight();
    }

    // 가이드 레이어의 UI 우선순위를 설정하는 함수
    private void SetupGuideLayer()
    {
        if (guideLayer == null) return;

        // Canvas 컴포넌트가 없으면 추가하고 설정을 변경
        Canvas canvas = guideLayer.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = guideLayer.gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 7; // 하이라이트 레이어보다 위에 표시
    }

    // 시작 지점에서 목적지까지 아이콘을 반복 이동시키는 코루틴
    private IEnumerator ActionGuideRoutine(GameObject start, GameObject end)
    {
        while (_currentDragGuide != null && start != null && end != null)
        {
            float elapsed = 0f;
            float duration = 1.2f;

            while (elapsed < duration && _currentDragGuide != null && start != null && end != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _currentDragGuide.transform.position = Vector3.Lerp(start.transform.position, end.transform.position, t);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    [YarnCommand("complete_tutorial")]
    public void CompleteTutorialAndLoadMain()
    {
        if (KeyboardInputManager.Instance != null)
            KeyboardInputManager.Instance.AllowAllKeys();

        // 1.봤음 도장 찍기
        PlayerPrefs.SetInt("SawTutorial", 1);
        PlayerPrefs.Save();

        // 2. 직접 깼다고 플래그 세우기
        GameLoadFlags.isTutorialJustFinished = true; // 이게 true여야 셔터 애니메이션 나옴
        GameLoadFlags.shouldLoadFromSave = false;

        SceneManager.LoadScene("MainScene");
    }

    [YarnFunction("getSawTutorial")]
    public static bool GetSawTutorial()
    {
        return PlayerPrefs.HasKey("SawTutorial") && PlayerPrefs.GetInt("SawTutorial") == 1;
    }

    // ✨ [NEW] Yarn에서 호출: 튜토리얼 스킵하고 메인으로 가기
    [YarnCommand("skip_tutorial")]
    public void SkipTutorial()
    {
        Debug.Log("[Tutorial] 유저 선택으로 튜토리얼 스킵 -> 메인으로 이동");

        if (KeyboardInputManager.Instance != null)
            KeyboardInputManager.Instance.AllowAllKeys();

        // 플래그 설정: 스킵했으므로 '직접 깬 것' 아님
        GameLoadFlags.isTutorialJustFinished = false;
        GameLoadFlags.shouldLoadFromSave = false;

        // 메인 씬 로드 -> GameManager가 "스킵했네?" 하고 기본 세팅+셔터OFF로 시작함
        SceneManager.LoadScene("MainScene");
    }

    [YarnCommand("time_stop")]
    public void TimeStop()
    {
        GameClock.Pause();
        Debug.Log("[튜토리얼] 시간 정지 명령 실행");
        //SetSystemFreeze(true);
    }
    [YarnCommand("time_resume")]
    public void TimeResume()
    {
        GameClock.Resume();
        Debug.Log("[튜토리얼] 시간 재개 명령 실행");
        //SetSystemFreeze(true);
    }

    // 🛠️ 헬퍼 함수: 재료 이름으로 키를 찾아 허용 목록에 추가
    private static void AllowIngredientKey(string ingredientName)
    {
        // IngredientStockManager가 관리하는 키 매핑을 뒤져서 찾음
        var keys = IngredientStockManager.Instance.GetAllRegisteredKeys();
        foreach (var key in keys)
        {
            if (IngredientStockManager.Instance.GetIngredientByKey(key) == ingredientName)
            {
                KeyboardInputManager.Instance.AllowKey(key);
                break;
            }
        }
    }

    private static void ConstrainIngredientKey(string ingredientName)
    {
        var keys = IngredientStockManager.Instance.GetAllRegisteredKeys();
        foreach (var key in keys)
        {
            if (IngredientStockManager.Instance.GetIngredientByKey(key) == ingredientName)
            {
                KeyboardInputManager.Instance.ConstrainKey(key);
                break;
            }
        }
    }

    // 🛠️ 헬퍼 함수: 튜토리얼 입력 모드 초기화
    private static void InitKeyboardForTutorial()
    {
        KeyboardInputManager.Instance.SetTutorialMode(true);
    }

    public void OnExitButtonClick()
    {
        // 하이라이트 제거
        Unhighlight();

        // Yarn 대화가 진행 중이었다면 강제 종료
        if (dialogueRunner != null) dialogueRunner.Stop();

        // 시작 화면으로
        LoadScene("StartScene");

    }
}