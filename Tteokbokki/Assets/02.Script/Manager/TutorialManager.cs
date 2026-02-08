using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    [SerializeField] private RectTransform focusLayer;
    [SerializeField] private GameObject darkOverlay;
    [SerializeField] private GameObject yellowOutlinePrefab;

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
    public CanvasGroup gameplayUI;

    // --- 내부 데이터 관리 ---
    private Dictionary<string, int> _tutorialTally = new Dictionary<string, int>();
    private List<HighlightData> _activeHighlights = new List<HighlightData>();
    private GameObject _lastTarget;

    private struct HighlightData
    {
        public GameObject target;
        public Transform originalParent;
        public int originalIndex;
        public GameObject outlineInstance;
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
        SetSystemFreeze(true);

        if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.StartDialogue("TutorialStart");
        }
    }

    public void SetSystemFreeze(bool isFreeze)
    {
        // 1. 기존 로직 (상태 저장)
        IsFreeze = isFreeze;

        // 2. 물리적 클릭 차단 추가 (9번 버그 해결 핵심)
        if (gameplayUI != null)
        {
            // 프리즈 상태면(isFreeze: true) -> 클릭 차단(interactable: false / blocksRaycasts: false)
            // 프리즈 해제면(isFreeze: false) -> 클릭 허용(true)
            // gameplayUI.interactable = !isFreeze;
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

        // CompleteTutorial
        if (Instance != null) Instance.IsTutorial = false;
        GameSaveManager.Instance.SetTutorialComplete();

        // 셔터가 내려가 있는 상태에서 씬을 전환합니다.
        // 씬이 바뀌면 새로운 씬의 GameManager가 셔터를 다시 올리게 됩니다.
        SceneManager.LoadScene(sceneName);
    }

    // ==========================================
    // 1. 하이라이트 및 UI 제어
    // ==========================================

    [YarnCommand("highlight")]
    public static void Highlight(string objName)
    {
        var inst = Instance;
        if (inst == null) return;

        // 1. 이름으로 오브젝트 찾기
        GameObject target = GameObject.Find(objName);

        // 2. 찾았다면 실제 하이라이트 로직(ApplyHighlight) 실행
        if (target != null)
        {
            inst.ApplyHighlight(target);
        }
        else
        {
            Debug.LogWarning($"[튜토리얼] {objName}을(를) 찾을 수 없습니다.");
        }
    }

    private void ApplyHighlight(GameObject target)
    {
        if (target == null) return;

        // 🚨 [방어막 1] 자기 자신이 이미 하이라이트 리스트에 있는지 확인
        if (_activeHighlights.Exists(h => h.target == target)) return;

        // 🚨 [방어막 2] 부모 중 누군가가 이미 하이라이트 되어 있는지 확인
        // (부모가 이미 빛나고 있다면 자식은 또 빛날 필요가 없습니다!)
        if (_activeHighlights.Exists(h => target.transform.IsChildOf(h.target.transform)))
        {
            Debug.Log($"[하이라이트] {target.name}의 부모가 이미 하이라이트 중이라 스킵합니다.");
            return;
        }

        Canvas.ForceUpdateCanvases();
        Vector3 originalWorldPos = target.transform.position;

        HighlightData data = new HighlightData
        {
            target = target,
            originalParent = target.transform.parent,
            originalIndex = target.transform.GetSiblingIndex(),
            outlineInstance = null
        };

        target.transform.SetParent(focusLayer, true);
        target.transform.position = originalWorldPos;

        // 블랙 커튼 활성화 및 순서 정리
        if (darkOverlay != null)
        {
            darkOverlay.SetActive(true);
            darkOverlay.transform.SetAsFirstSibling();
        }

        if (yellowOutlinePrefab != null)
        {
            GameObject outline = Instantiate(yellowOutlinePrefab, focusLayer);
            outline.transform.SetSiblingIndex(1);
            data.outlineInstance = outline;

            RectTransform targetRT = target.GetComponent<RectTransform>();
            RectTransform outlineRT = outline.GetComponent<RectTransform>();

            if (targetRT != null && outlineRT != null)
            {
                outlineRT.anchorMin = targetRT.anchorMin;
                outlineRT.anchorMax = targetRT.anchorMax;
                outlineRT.pivot = targetRT.pivot;
                outlineRT.sizeDelta = targetRT.sizeDelta + new Vector2(15, 15);
                outlineRT.position = targetRT.position;
                outlineRT.localScale = Vector3.one;
            }
        }

        _activeHighlights.Add(data);
        SetSystemFreeze(false);
    }

    [YarnCommand("unhighlight")]
    public static void Unhighlight()
    {
        var inst = Instance;
        if (inst == null) return;

        // 역순으로 돌면서 모든 하이라이트 해제
        for (int i = inst._activeHighlights.Count - 1; i >= 0; i--)
        {
            var h = inst._activeHighlights[i];

            if (h.outlineInstance != null) Destroy(h.outlineInstance);

            if (h.target != null && h.originalParent != null)
            {
                // 원래 부모와 순서로 복구
                h.target.transform.SetParent(h.originalParent, true);
                h.target.transform.SetSiblingIndex(h.originalIndex);
            }
        }

        inst._activeHighlights.Clear();
        inst.darkOverlay.SetActive(false);
        inst.SetSystemFreeze(true);

        Canvas.ForceUpdateCanvases();
    }

    [YarnCommand("highlightSources")]
    public static void HighlightSources()
    {
        var inst = Instance;
        if (inst == null) return;

        // 🚨 개별 아이템이 아니라, 소스들이 담긴 '판(Grid)'을 통째로 찾습니다.
        GameObject gridNew = GameObject.Find("Grid_New");

        if (gridNew != null)
        {
            Debug.Log("[튜토리얼] Grid_New를 통째로 하이라이트합니다.");
            inst.ApplyHighlight(gridNew);
        }
        else
        {
            Debug.LogWarning("[튜토리얼] Grid_New 오브젝트를 찾을 수 없습니다! 이름을 확인해주세요.");
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


    [YarnCommand("startBoiling")]
    public static void StartBoiling()
    {
        // [수정] 오디오 매니저를 통해 요리 시작 소리(101)를 재생합니다.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(101);
        }

        Instance?.targetStoveSlot?.StartTutorialCook();
    }

    // ==========================================
    // Wait For - 함수들
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
        // 🚨 튜토리얼 전용 리스너만 살짝 추가
        UnityEngine.Events.UnityAction tutorialAction = () => isClicked = true;

        btn.onClick.AddListener(tutorialAction);
        inst.SetSystemFreeze(false); // 클릭할 수 있게 해동

        // 🚨 클릭할 때까지 Yarn Runner 대기
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

        inst.SetSystemFreeze(false);
        Debug.Log("[튜토리얼] 재료 투입 대기 중...");

        // 🚨 [수정] 조건이 맞을 때까지 여기서 Yarn Runner를 붙잡아둡니다.
        yield return new WaitUntil(() => {
            var currentInWok = inst.targetStoveSlot.GetPendingIngredientsCopy();
            return inst.CheckRecipeCondition(currentInWok);
        });

        Debug.Log("[튜토리얼] 재료 투입 완료!");
        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitSauce")]
    public static IEnumerator WaitSauce()
    {
        var inst = Instance;
        if (inst == null) yield break;

        inst.SetSystemFreeze(false);
        bool sauceAdded = false;

        // 소스 버튼 리스너 설정
        foreach (var ib in FindObjectsByType<IngredientButton>(FindObjectsSortMode.None))
        {
            if (ib.ingredientName == "군자 소스")
            {
                Button btn = ib.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    if (IsFreeze) return;
                    sauceAdded = true;
                });
            }
        }

        // 🚨 소스를 넣을 때까지 대기
        yield return new WaitUntil(() => sauceAdded);

        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForBoiled")]
    public static IEnumerator WaitForBoiled()
    {
        if (Instance?.targetStoveSlot == null) yield break;

        // 1. 요리가 '시작'될 때까지 대기
        yield return new WaitUntil(() => Instance.targetStoveSlot.IsCooking);

        // 2. 요리가 '끝날' 때까지 대기
        while (Instance.targetStoveSlot.IsCooking) yield return null;

        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForPacking")]
    public static IEnumerator WaitForPacking()
    {
        var inst = Instance;
        if (inst == null) yield break;

        // 1. 조작 허용: 음식을 드래그해서 옮겨야 하니까요!
        inst.SetSystemFreeze(false);

        bool isPacked = false;

        // 2. 어떤 포장 슬롯이든 음식이 들어올 때까지 대기
        // (사장님이 하이라이트한 PackagingSlot 오브젝트를 포함한 모든 슬롯 대상)
        while (!isPacked)
        {
            var allSlots = UnityEngine.Object.FindObjectsByType<PackagingSlot>(FindObjectsSortMode.None);
            foreach (var slot in allSlots)
            {
                if (slot.HasAnyFood()) // PackagingSlot에 구현되어 있는 메서드 활용
                {
                    isPacked = true;
                    break;
                }
            }
            yield return null; // 한 프레임 쉼
        }

        Debug.Log("[튜토리얼] 음식 포장대 안착 확인!");

        // 3. 다시 조작 잠금 및 하이라이트 해제
        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForReceiptAttached")]
    public static IEnumerator WaitForReceiptAttached()
    {
        var inst = Instance;
        if (inst == null) yield break;

        inst.SetSystemFreeze(false);

        // 1. 영수증이 씬에 나타날 때까지 먼저 대기
        yield return new WaitUntil(() => GameObject.Find("TutorialReceipt") != null);
        Debug.Log("[튜토리얼] 영수증 발견됨. 부착 대기 중...");

        // 2. 영수증이 '진짜 집(음식)'을 찾을 때까지 대기
        yield return new WaitUntil(() => {
            GameObject receipt = GameObject.Find("TutorialReceipt");

            // 영수증이 파괴되었다면 (성공적으로 붙인 뒤 삭제되는 로직일 경우) 완료
            if (receipt == null) return true;

            Transform currentParent = receipt.transform.parent;
            if (currentParent == null) return false;

            // 🚨 드래그 중이거나 하이라이트 레이어에 있을 때는 '미완료' 상태입니다.
            // 부모 이름이 "Cooked", "Food", "Box", "Slot" 등으로 바뀌었을 때만 성공으로 간주합니다.
            bool isAttached = currentParent.name.Contains("Cooked") ||
                              currentParent.name.Contains("Food") ||
                              currentParent.name.Contains("Slot");

            return isAttached;
        });

        Debug.Log("[튜토리얼] 영수증 부착 완료 로그 확인!");

        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForPayment")]
    public static IEnumerator WaitForPayment()
    {
        var inst = Instance;
        if (inst == null || inst.paymentButton == null) yield break;

        bool isClicked = false;
        // 🚨 기존 리스너를 지우지 않고, 튜토리얼용 체크 함수만 정의합니다.
        UnityEngine.Events.UnityAction tutorialAction = () => isClicked = true;

        // 리스너 추가 (기존 기능 + 튜토리얼 신호)
        inst.paymentButton.onClick.AddListener(tutorialAction);
        inst.SetSystemFreeze(false);

        // 플레이어가 누를 때까지 대기
        yield return new WaitUntil(() => isClicked);

        // 🚨 [핵심] 볼일 끝났으니 튜토리얼 리스너만 제거 (원래 기능은 보존)
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
    public static IEnumerator WaitForEndDay() // void에서 IEnumerator로 변경
    {
        var inst = Instance;
        if (inst == null || inst.endDayButton == null) yield break;

        bool isClicked = false;
        UnityEngine.Events.UnityAction tutorialAction = () => isClicked = true;

        // 기존 기능을 방해하지 않고 튜토리얼 신호만 추가
        inst.endDayButton.onClick.AddListener(tutorialAction);
        inst.SetSystemFreeze(false);

        yield return new WaitUntil(() => isClicked);

        // 리스너 제거 및 정리
        inst.endDayButton.onClick.RemoveListener(tutorialAction);

        inst.SetSystemFreeze(true);
        Unhighlight();
        yield return new WaitForEndOfFrame();
    }

    [YarnCommand("waitForAnySourceClick")]
    public static IEnumerator WaitForAnySourceClick()
    {
        var inst = Instance;
        if (inst == null) yield break;

        inst.SetSystemFreeze(false);
        string sauceName = "";

        yield return new WaitUntil(() =>
        {
            var items = FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
            var selected = items.FirstOrDefault(i =>
                i.selectToggle.isOn &&
                (i.gameObject.name.Contains("소스") || i.gameObject.name.ToLower().Contains("sauce"))
            );

            if (selected != null)
            {
                sauceName = selected.gameObject.name;
                return true;
            }
            return false;
        });

        inst.dialogueRunner.VariableStorage.SetValue("$SelectedSource", sauceName);

        // 🚨 [수정] 여기서 StartDialogue를 부르지 않습니다.
        inst.SetSystemFreeze(true);
        Unhighlight();
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

        // 🚨 [중요] 클릭 직후 튜토리얼 전용 리스너만 제거! (기존 게임 로직은 보존)
        closeBtn.onClick.RemoveListener(tutorialAction);

        // 3. ✨ 테두리 즉시 제거 (셔터가 내려오기 전)
        Unhighlight();

        // 4. 셔터 애니메이션 시간 대기
        Debug.Log("[튜토리얼] 다음 날 버튼 클릭 확인. 셔터 대기 중...");
        yield return new WaitForSeconds(1.0f);

        inst.SetSystemFreeze(true);
        yield return new WaitForEndOfFrame();
    }
}