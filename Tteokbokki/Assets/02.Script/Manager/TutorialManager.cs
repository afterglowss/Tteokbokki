using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;
using static UnityEngine.InputManagerEntry;


public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    // 시스템 조작 가능 여부를 결정하는 변수입니다.
    public static bool IsFreeze { get; private set; } = true;
    public bool IsTutorial { get; private set; } = false;

    [Header("하이라이트 레이어 설정")]
    [SerializeField] private RectTransform focusLayer;
    [SerializeField] private GameObject darkOverlay;
    [SerializeField] private RectTransform yellowOutline;

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
    [SerializeField] private RawImage backgroundDisplay;
    [SerializeField] private Texture2D desktopTexture;
    [SerializeField] private Texture2D mainShopTexture;

    // --- 내부 데이터 관리 ---
    private Dictionary<string, int> _tutorialTally = new Dictionary<string, int>();
    private List<HighlightData> _activeHighlights = new List<HighlightData>();
    private GameObject _lastTarget;

    private struct HighlightData
    {
        public GameObject target;
        public Transform originalParent;
        public int originalIndex;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeTutorial();
    }
    private void InitializeTutorial()
    {
        // 1. 얀 함수 등록 (GameSaveManager와 연결)
        dialogueRunner?.AddFunction("getSawTutorial", () => GameSaveManager.Instance.IsTutorialCompleted);

        OrderSpawner.Instance?.StopSpawning();
        darkOverlay?.SetActive(false);
        yellowOutline?.gameObject.SetActive(false);

        if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.StartDialogue("TutorialStart");
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

    public void SetSystemFreeze(bool isFreeze) => IsFreeze = isFreeze;

    // ==========================================
    // 1. 하이라이트 및 UI 제어 (IEnumerator)
    // ==========================================
    [YarnCommand("highlight")]
    public static void Highlight(string objName) // static 추가
    {
        var inst = Instance;
        if (inst == null) return;

        GameObject target = GameObject.Find(objName);
        if (target == null) return;

        if (inst._activeHighlights.Exists(h => h.target == target)) return;

        HighlightData data = new HighlightData
        {
            target = target,
            originalParent = target.transform.parent,
            originalIndex = target.transform.GetSiblingIndex()
        };
        inst._activeHighlights.Add(data);

        target.transform.SetParent(inst.focusLayer);
        inst.yellowOutline.gameObject.SetActive(true);
        inst.yellowOutline.position = target.transform.position;
        inst.darkOverlay.SetActive(true);
        inst.SetSystemFreeze(false);
    }

    [YarnCommand("unhighlight")]
    public static void Unhighlight()
    {
        var inst = Instance;
        if (inst == null) return;

        foreach (var h in inst._activeHighlights)
        {
            if (h.target != null && h.originalParent != null)
            {
                // 원래 부모로 복구
                h.target.transform.SetParent(h.originalParent);
                h.target.transform.SetSiblingIndex(h.originalIndex);
            }
        }
        inst._activeHighlights.Clear();
        inst.darkOverlay.SetActive(false);
        inst.yellowOutline.gameObject.SetActive(false);
        inst.SetSystemFreeze(true);
    }
    [YarnCommand("waitForEndDay")]
    public static void WaitForEndDay() // static을 붙여야 'Target' 에러가 안 납니다.
    {
        var inst = Instance;
        if (inst == null || inst.endDayButton == null) return;

        // 1. 기존에 버튼에 붙어있던 기능들 싹 정리 (중복 실행 방지)
        inst.endDayButton.onClick.RemoveAllListeners();

        // 2. 버튼을 누르면 실행될 동작 딱 하나만 등록
        inst.endDayButton.onClick.AddListener(() =>
        {
            Unhighlight(); // 윤곽선 끄기
            inst.dialogueRunner.StartDialogue("ShowEndDayStep"); // 다음 노드 시작
        });

        // 3. 버튼을 누를 수 있게 시스템 잠금 해제
        inst.SetSystemFreeze(false);
    }

    [YarnCommand("waitForPayment")]
    public static IEnumerator WaitForPayment()
    {
        var inst = Instance;
        if (inst == null || inst.paymentButton == null) yield break;

        bool isClicked = false;
        UnityEngine.Events.UnityAction action = null;
        action = () =>
        {
            isClicked = true;
            // 클릭 감지 즉시 리스너 제거 (중복 실행 방지)
            inst.paymentButton.onClick.RemoveListener(action);
        };
        inst.paymentButton.onClick.AddListener(action);

        inst.SetSystemFreeze(false); // 버튼을 누를 수 있게 프리즈 해제

        // 1. 플레이어가 세금 버튼을 누를 때까지 여기서 대기합니다.
        yield return new WaitUntil(() => isClicked);

        // 2. [중요] 상점이 열리기 직전에 튜토리얼 모드를 미리 켭니다.
        // 그래야 상점의 SafePopulate()가 돌아갈 때 PopulateTutorialShop()을 찾아갑니다.
        var shopUI = UnityEngine.Object.FindAnyObjectByType<IngredientShopUI>();
        if (shopUI != null)
        {
            shopUI.isTutorialMode = true;
        }

        // 3. UI 정리
        Unhighlight();
        if (inst.endOfDayParent != null) inst.endOfDayParent.SetActive(true);

        // 4. 아주 짧은 대기 후 즉시 다음 노드('ShowShopStep') 시작!
        yield return new WaitForSeconds(0.1f);
        inst.dialogueRunner.StartDialogue("ShowShopStep");

        // 5. 다시 시스템 프리즈 (대화 창에 집중)
        inst.SetSystemFreeze(true);
    }

    // -----------------------------
    // -----------------------------

    // 2. 소스들만 찾아서 하이라이트 (수정)
    [YarnCommand("highlightSources")]
    public static void HighlightSources()
    {
        var items = UnityEngine.Object.FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
        foreach (var item in items)
        {
            // 상점 아이템 생성 시 obj.name = data.Name; 로 설정되어 있어야 합니다.
            if (item.gameObject.name.Contains("소스"))
            {
                // 사용자님의 메서드: string을 받으므로 .name을 넘깁니다.
                Highlight(item.gameObject.name);
            }
        }
    }

    [YarnCommand("waitForAnySourceClick")]
    public static IEnumerator WaitForAnySourceClick()
    {
        var inst = Instance;
        inst.SetSystemFreeze(false);
        string sauceName = "";

        // 플레이어가 소스 중 하나를 체크할 때까지 대기
        yield return new WaitUntil(() =>
        {
            var items = UnityEngine.Object.FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
            // 신규 입고(Locked) 구역에 있는 소스 중 체크된 것 찾기
            var selected = items.FirstOrDefault(i => i.selectToggle.isOn && i.gameObject.name.Contains("소스"));

            if (selected != null)
            {
                sauceName = selected.gameObject.name;
                return true;
            }
            return false;
        });

        // 얀 스크립트에 선택한 소스 이름 전달
        inst.dialogueRunner.VariableStorage.SetValue("$SelectedSource", sauceName);

        Unhighlight();
        inst.SetSystemFreeze(true);

        // 선택 완료 후 다음 대사 노드로 이동
        inst.dialogueRunner.StartDialogue("BuySauceStep");
    }

    // ==========================================
    // 2. 배경 및 사운드 설정
    // ==========================================

    [YarnCommand("set_bg")]
    public static IEnumerator SetBackground(string bgName)
    {
        if (Instance?.backgroundDisplay == null) yield break;
        Instance.backgroundDisplay.texture = (bgName == "Desktop") ? Instance.desktopTexture : Instance.mainShopTexture;
        Instance.backgroundDisplay.color = Color.white;
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("playSFX")]
    public static void PlaySFX(int id) => AudioManager.Instance?.PlaySFX(id);

    // ==========================================
    // 4. 게임플레이 로직 (요리, 영수증, 포장 등)
    // ==========================================

    [YarnCommand("spawnTutorialReceipt")]
    public static void SpawnTutorialReceipt()
    {
        if (Instance == null || Instance.receiptLineManager == null) return;
        Receipt tutorialReceipt = new Receipt(DateTime.Now, 1);
        tutorialReceipt.AddOrder("군자 떡볶이", new Dictionary<string, int>());
        Instance.receiptLineManager.AddNewReceipt(tutorialReceipt);

        var slots = Instance.receiptLineManager.GetReceiptSlots();
        if (slots.Count > 0)
        {
            var lastItem = slots[slots.Count - 1];
            lastItem.gameObject.name = "TutorialReceipt";
            var btn = lastItem.GetComponent<Button>();
            btn?.onClick.RemoveAllListeners();
            btn?.onClick.AddListener(() =>
            {
                if (IsFreeze) return;
                Instance.receiptLineManager.combinedIngredientManager?.DisplayAllCombinedIngredients(lastItem.GetReceipt());
                Unhighlight();
                Instance.dialogueRunner.StartDialogue("AddIngredientStep");
            });
        }
    }

    [YarnCommand("waitIngredients")]
    public static void WaitIngredients()
    {
        if (Instance == null) return;
        Instance._tutorialTally.Clear();
        Instance.SetSystemFreeze(false);
        foreach (var ib in FindObjectsByType<IngredientButton>(FindObjectsSortMode.None))
        {
            ib.GetComponent<Button>()?.onClick.AddListener(() => Instance.OnIngredientClick(ib.ingredientName));
        }
    }

    private void OnIngredientClick(string name)
    {
        if (IsFreeze || targetStoveSlot == null) return;

        // 1. [중요] 버튼을 누르면 '실제 웍'에 재료를 넣습니다.
        targetStoveSlot.AddIngredient(name);

        // 2. [수정] 웍에 담긴 실제 개수를 가져와서 체크합니다.
        var currentInWok = targetStoveSlot.GetPendingIngredientsCopy();

        // 3. 조건 체크
        if (name == "군자 소스")
        {
            SetSystemFreeze(true);
            Unhighlight();
            dialogueRunner.StartDialogue("BoilingStep");
        }
        // 내 장부(_tutorialTally) 대신 웍의 데이터(currentInWok)를 넘겨줍니다.
        else if (CheckRecipeCondition(currentInWok))
        {
            SetSystemFreeze(true);
            Unhighlight();
            dialogueRunner.StartDialogue("AddSauceStep");
        }
    }

    // 매개변수로 웍의 데이터를 받도록 수정
    private bool CheckRecipeCondition(Dictionary<string, int> wokData)
    {
        return wokData.GetValueOrDefault("떡", 0) >= 2 &&
               wokData.GetValueOrDefault("오뎅", 0) >= 2 &&
               wokData.GetValueOrDefault("파", 0) >= 1 &&
               wokData.GetValueOrDefault("양배추", 0) >= 1;
    }

    [YarnCommand("waitSauce")]
    public static void WaitSauce() => Instance?.SetSystemFreeze(false);

    [YarnCommand("startBoiling")]
    public static void StartBoiling()
    {
        // [수정] 오디오 매니저를 통해 요리 시작 소리(404번)를 재생합니다.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(404);
        }

        Instance?.targetStoveSlot?.StartTutorialCook();
    }

    [YarnCommand("waitForDone")]
    public static IEnumerator WaitForDone()
    {
        if (Instance?.targetStoveSlot == null) yield break;
        while (Instance.targetStoveSlot.IsCooking) yield return null;
        Unhighlight();
    }

    [YarnCommand("waitForPacking")]
    public static IEnumerator WaitForPacking()
    {
        if (Instance?.packagingArea == null) yield break;
        bool detected = false;
        while (!detected)
        {
            foreach (var s in Instance.packagingArea.GetComponentsInChildren<PackagingSlot>())
            { if (s.HasAnyFood()) { detected = true; break; } }
            yield return null;
        }
        Unhighlight();
    }

    [YarnCommand("waitForReceipt")]
    public static IEnumerator WaitForReceipt()
    {
        GameObject receipt = GameObject.Find("TutorialReceipt");
        while (receipt != null) yield return null;
        Unhighlight();
        Instance.dialogueRunner?.StartDialogue("DeliveryStep");
    }

    [YarnCommand("spawnWok")]
    public static void SpawnWok()
    {
        var inst = Instance;
        if (inst == null || inst.targetStoveSlot == null) return;

        // 1. 화구 자체를 활성화
        inst.targetStoveSlot.OnPointerClick(null);
        inst.targetStoveSlot.SetSelected(true);

        // 2. [추가] StoveManager에게 현재 '선택된 화구'가 이것임을 강제 주입
        // StoveManager에 SelectSlot이 public으로 있으므로 이를 호출합니다.
        StoveManager.Instance.SelectSlot(inst.targetStoveSlot);
    }
    [YarnCommand("waitForOrderClick")]
    public static IEnumerator WaitForOrderClick()
    {
        var inst = Instance;
        // 씬에서 상점 UI를 직접 찾습니다. (Find 방식)
        var shopUI = UnityEngine.Object.FindAnyObjectByType<IngredientShopUI>();

        if (inst == null || shopUI == null)
        {
            Debug.LogError("[튜토리얼] 상점 UI를 찾을 수 없어 주문 대기를 시작할 수 없습니다.");
            yield break;
        }

        bool isOrdered = false;
        inst.SetSystemFreeze(false); // 버튼 클릭이 가능하도록 프리즈 해제

        // 주문 완료 시 실행될 액션 등록
        UnityEngine.Events.UnityAction action = null;
        action = () =>
        {
            isOrdered = true;
            shopUI.OnShopProcessFinished.RemoveListener(action);
        };

        shopUI.OnShopProcessFinished.AddListener(action);

        // 플레이어가 '주문하기'를 누를 때까지 여기서 대기합니다.
        yield return new WaitUntil(() => isOrdered);
        Debug.Log("[시스템] 주문 확인됨! 다음 노드로 이동 시도.");

        inst.SetSystemFreeze(true); // 대화 집중을 위해 다시 프리즈
        inst.SafeStartDialogue("CheckingStep");
    }
    [YarnCommand("waitForChecking")]
    public static IEnumerator WaitForChecking()
    {
        var inst = Instance;
        if (inst == null) yield break;

        // 1. 이름으로 직접 토글 오브젝트를 찾습니다.
        GameObject receiptObj = GameObject.Find("Toggle_CheckReceipt");
        GameObject ingredientObj = GameObject.Find("Toggle_CheckIngredient");

        if (receiptObj == null || ingredientObj == null)
        {
            Debug.LogError("[튜토리얼] 토글 오브젝트를 찾을 수 없습니다! 이름을 확인하세요.");
            yield break;
        }

        Toggle receiptToggle = receiptObj.GetComponent<Toggle>();
        Toggle ingredientToggle = ingredientObj.GetComponent<Toggle>();

        inst.SetSystemFreeze(false);

        // 2. 두 토글이 모두 켜질 때까지 대기
        yield return new WaitUntil(() => receiptToggle.isOn && ingredientToggle.isOn);

        inst.SetSystemFreeze(true);
        inst.SafeStartDialogue("ClosingStep");
    }

    [YarnCommand("waitForClosing")]
    public static IEnumerator WaitForClosing()
    {
        var inst = Instance;
        GameObject closeBtnObj = GameObject.Find("Button_NextDay");
        if (closeBtnObj == null) yield break;

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        inst.SetSystemFreeze(false);

        bool isClicked = false;
        closeBtn.onClick.AddListener(() => isClicked = true);

        // 1. 버튼 클릭 대기
        yield return new WaitUntil(() => isClicked);

        // 2. ? [추가] 셔터가 내려오는 시간(약 0.6~1초)만큼 잠깐 기다려줍니다.
        // 셔터가 쾅! 닫힌 뒤에 대사가 나오는 게 더 자연스럽기 때문입니다.
        yield return new WaitForSeconds(1.0f);

        inst.SetSystemFreeze(true); 
        inst.SafeStartDialogue("ClosedStep");
    }

    [YarnCommand("startScene")]
    public static void StartScene(string sceneName)
    {
        Debug.Log($"[튜토리얼] {sceneName} 씬으로 이동합니다.");

        // CompleteTutorial
        // if (Instance != null) Instance.IsTutorial = false;
        // GameSaveManager.Instance.SetTutorialComplete();

        // 셔터가 내려가 있는 상태에서 씬을 전환합니다.
        // 씬이 바뀌면 새로운 씬의 GameManager가 셔터를 다시 올리게 됩니다.
        // SceneManager.LoadScene(sceneName);
    }
    public void SafeStartDialogue(string nodeName)
    {
        if (dialogueRunner == null) return;

        // 1. 현재 TutorialManager에서 돌고 있는 다른 대기 코루틴이 있다면 중단
        StopAllCoroutines();
        Debug.Log("SafeStartDialogue;;");
        // 2. 대화 시작을 위한 안전 루틴 실행
        StartCoroutine(SafeStartRoutine(nodeName));
    }

    private IEnumerator SafeStartRoutine(string nodeName)
    {
        // 3. 만약 대화가 이미 실행 중이라면 (선택지 대기 포함) 무조건 중단
        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.Log($"[튜토리얼] 기존 대화를 강제 중단하고 '{nodeName}' 노드 준비...");
            dialogueRunner.Stop();

            // ? [핵심] Yarn VM이 중단 명령을 처리하고 내부 상태(선택지 등)를 
            // 완전히 비울 수 있도록 최소 1~2프레임을 기다려줍니다.
            yield return null;
            yield return null;
        }

        // 4. 이제 깨끗해진 상태에서 새 노드 시작
        dialogueRunner.StartDialogue(nodeName);
    }
}