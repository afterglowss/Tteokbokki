using DG.Tweening; // DOTween 필수
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfDayUIHandler : MonoBehaviour
{
    [Header("Black Curtain")]
    [SerializeField] private Image blackCurtainImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [Range(0f, 1f)][SerializeField] private float curtainMaxAlpha = 0.7f;

    [Header("Shutter Animation")]
    [SerializeField] private RectTransform shutterRect;
    [SerializeField] private float shutterMoveDuration = 0.6f;
    [SerializeField] private float shutterStayDelay = 0.5f;
    private float screenHeight;

    [Header("Animation References")]
    [SerializeField] private RectTransform mainWindowRect;
    [SerializeField] private Image[] macDecorationButtons;
    [SerializeField] private float windowPopDuration = 0.5f;

    [Header("Panels (순서대로 진행)")]
    [SerializeField] private GameObject panelSettlement;
    [SerializeField] private GameObject panelShop;
    [SerializeField] private GameObject panelClosing;

    [Header("Step 1: 정산 UI")]
    [SerializeField] private TextMeshProUGUI textSuccess;
    [SerializeField] private TextMeshProUGUI textMissed;
    [SerializeField] private TextMeshProUGUI textSuccessAmount;
    [SerializeField] private TextMeshProUGUI textMissedAmount;
    [SerializeField] private ScrollRect[] settlementScrollRect;
    [SerializeField] private TextMeshProUGUI textTotalAndTax;
    [SerializeField] private TextMeshProUGUI textNetIncome;
    [SerializeField] private Button buttonPayTaxAndNext;
    [SerializeField] private TextMeshProUGUI textPayButton;

    [Header("Step 2: 상점 UI")]
    [SerializeField] private TextMeshProUGUI lowStockTextUI;
    [SerializeField] private TextMeshProUGUI textLowStockCost;
    [SerializeField] private Button buttonOrderAndNext; // (외부 버튼용)

    [Header("Step 3: 마감 체크 UI")]
    [SerializeField] private Toggle checkReciptToggle;
    [SerializeField] private Toggle checkTaxToggle;
    [SerializeField] private Toggle checkIngredientToggle;
    [SerializeField] private Toggle checkAllToggle;
    [SerializeField] private Button buttonFinalClose;

    [Header("재료 상점 UI")]
    [SerializeField] private IngredientShopUI IngredientShop;

    private int currentTaxAmount = 0;
    private bool isTaxPaid = false;
    private bool isUpdating = false;

    // ✨ 현재 활성화된 패널 추적용
    private GameObject currentActivePanel;

    private bool hasShownBonusDialogue = false;

    public bool IsShutterAnimating { get; private set; } = false;
    private bool isShutterMode = false;

    private void Awake()
    {
        if (blackCurtainImage != null) blackCurtainImage.gameObject.SetActive(false);
        if (shutterRect != null) shutterRect.gameObject.SetActive(false);

        // ✨ 페이드 효과를 위해 CanvasGroup이 없다면 미리 추가해둠
        SetupCanvasGroup(panelSettlement);
        SetupCanvasGroup(panelShop);
        SetupCanvasGroup(panelClosing);
    }

    private void SetupCanvasGroup(GameObject panel)
    {
        if (panel != null && panel.GetComponent<CanvasGroup>() == null)
        {
            panel.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) screenHeight = canvas.GetComponent<RectTransform>().rect.height;
        else screenHeight = 1920f;

        buttonPayTaxAndNext.onClick.AddListener(OnPayTaxAndNextClicked);

        if (IngredientShop != null)
        {
            IngredientShop.OnShopProcessFinished.AddListener(OnOrderAndNextClicked);
        }
        if (buttonOrderAndNext != null)
        {
            buttonOrderAndNext.onClick.AddListener(OnOrderAndNextClicked);
        }

        buttonFinalClose.onClick.AddListener(OnFinalCloseClicked);

        checkAllToggle.onValueChanged.AddListener(OnToggleAllChanged);
        checkReciptToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
        checkTaxToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
        checkIngredientToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
    }

    private void OnEnable()
    {
        TooltipManager.HideAll();

        // ✨ [핵심 수정] 셔터 애니메이션 모드라면, 정산 데이터 초기화를 하지 않고 나갑니다.
        if (isShutterMode)
        {
            // 패널들이 혹시 켜져있다면 확실하게 끕니다.
            if (panelSettlement != null) panelSettlement.SetActive(false);
            if (panelShop != null) panelShop.SetActive(false);
            if (panelClosing != null) panelClosing.SetActive(false);
            return;
        }

        InitializeSettlementData(animate: true);

        hasShownBonusDialogue = false;

        // ✨ [핵심 수정] 켜지자마자 크기를 0으로 만듭니다!
        // 딜레이(0.1초) 동안 원본 크기로 보이는 것을 방지합니다.
        if (mainWindowRect != null) mainWindowRect.localScale = Vector3.zero;

        // 장식 버튼들도 같이 숨겨둡니다.
        if (macDecorationButtons != null)
        {
            foreach (var btn in macDecorationButtons)
            {
                if (btn != null) btn.transform.localScale = Vector3.zero;
            }
        }

        // ✨ [핵심] 창이 열리기 전에 스크롤뷰를 미리 투명하게 숨겨둡니다!
        // 이렇게 해야 창이 커지는 애니메이션(Pop) 중에 이상한 위치의 스크롤이 보이지 않습니다.
        if (settlementScrollRect != null)
        {
            foreach (var scroll in settlementScrollRect)
            {
                if (scroll == null) continue;
                CanvasGroup cg = scroll.GetComponent<CanvasGroup>();
                if (cg == null) cg = scroll.gameObject.AddComponent<CanvasGroup>();

                cg.alpha = 0f; // 👻 일단 숨어있어!
            }
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(109);

        if (blackCurtainImage != null)
        {
            blackCurtainImage.gameObject.SetActive(true);
            Color c = blackCurtainImage.color;
            blackCurtainImage.color = new Color(c.r, c.g, c.b, 0f);
            blackCurtainImage.DOFade(curtainMaxAlpha, fadeDuration).SetEase(Ease.OutQuad);
        }

        DOVirtual.DelayedCall(0.1f, PlayWindowOpenAnimation);

        // 첫 시작은 애니메이션 없이 바로 1단계 배치
        currentActivePanel = panelSettlement;

        panelSettlement.SetActive(true);
        ResetPanelTransform(panelSettlement); // 위치 초기화

        panelShop.SetActive(false);
        panelClosing.SetActive(false);
    }

    // ✨ 패널 위치/투명도 초기화 함수
    // ✨ [수정] 패널 초기화 (Y축 위치는 유지하고, X축만 0으로 정렬)
    private void ResetPanelTransform(GameObject panel)
    {
        if (panel == null) return;

        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Vector3.zero 대신, 현재 Y값은 유지하고 X만 0으로 맞춤
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
        }
        else
        {
            // RectTransform이 없는 경우에만 기존 방식 사용
            panel.transform.localPosition = Vector3.zero;
        }

        var cg = panel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
    }

    private void PlayWindowOpenAnimation()
    {
        if (mainWindowRect != null) mainWindowRect.localScale = Vector3.zero;
        foreach (var btn in macDecorationButtons) if (btn != null) btn.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        if (mainWindowRect != null) seq.Append(mainWindowRect.DOScale(1f, windowPopDuration).SetEase(Ease.OutBack));

        for (int i = 0; i < macDecorationButtons.Length; i++)
        {
            if (macDecorationButtons[i] != null)
            {
                seq.Insert(windowPopDuration * 0.7f + (i * 0.1f),
                    macDecorationButtons[i].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            }
        }

        // ✨ [핵심 수정] 애니메이션이 다 끝나고, 창 크기가 100%가 되었을 때 스크롤 초기화!
        seq.OnComplete(() =>
        {
            if (settlementScrollRect != null)
            {
                for (int i = 0; i < settlementScrollRect.Length; i++)
                {
                    // 아까 만든 '강력한 ResetScrollCoroutine'을 여기서 호출
                    StartCoroutine(ResetScrollCoroutine(settlementScrollRect[i]));
                }
            }
        });
    }

    private void InitializeSettlementData(bool animate = false)
    {
        if (IngredientStockManager.Instance == null || ReceiptLineManager.Instance == null || PlayerWalletManager.Instance == null)
        {
            Debug.LogWarning("[정산] 매니저가 아직 준비되지 않아 초기화를 건너뜁니다.");
            return;
        }

        // ... (기존 로직 동일) ...
        isTaxPaid = false;
        int successTotal = 0;
        int missedTotal = 0;
        int bonusTotal = 0; // ✨ 변수 추가

        if (ReceiptLineManager.Instance != null)
        {
            textSuccess.text = ReceiptLineManager.Instance.GetTodaySuccessfulReceiptsText();
            textMissed.text = ReceiptLineManager.Instance.GetTodayMissedReceiptsText();
            successTotal = ReceiptLineManager.Instance.GetTotalSuccessfulAmount();
            missedTotal = ReceiptLineManager.Instance.GetTotalMissedAmount();

            // ✨ [NEW] 보너스 금액 가져오기
            if (DailyBonusManager.Instance != null)
                bonusTotal = DailyBonusManager.Instance.TodayAccumulatedBonus;

            // ✨ [수정] 성공 금액 표시에 보너스 합산 (혹은 괄호로 표기)
            // 방법 1: 그냥 합쳐서 보여주기 (깔끔함)
            int finalSuccessTotal = successTotal + bonusTotal;

            if (textSuccessAmount != null)
            {
                // 예: "총 성공 금액: +18,000원" (보너스 포함됨)
                if (animate) AnimateMoneyText(textSuccessAmount, 0, finalSuccessTotal, "총 성공 금액", "#008000", "+");
                else textSuccessAmount.text = $"총 성공 금액: <color=#008000>+{finalSuccessTotal:N0}원</color>";

                // (선택사항) 만약 보너스를 따로 괄호로 적어주고 싶다면 위 코드 대신 아래 사용:
                /*
                string bonusStr = bonusTotal > 0 ? $" <size=70%>(보너스 +{bonusTotal:N0})</size>" : "";
                textSuccessAmount.text = $"총 성공 금액: <color=#008000>+{finalSuccessTotal:N0}원</color>{bonusStr}";
                */
            }
            if (textMissedAmount != null)
            {
                if (animate) AnimateMoneyText(textMissedAmount, 0, missedTotal, "총 손실 금액", "red", "-");
                else textMissedAmount.text = $"총 손실 금액: <color=red>-{missedTotal:N0}원</color>";
            }
        }

        int grossIncome = successTotal + bonusTotal;
        float taxRate = 0.1f;
        currentTaxAmount = Mathf.RoundToInt(grossIncome * taxRate);
        int netIncome = grossIncome - currentTaxAmount;

        textTotalAndTax.text = $"총 매출: {grossIncome:N0}원\n세금: <color=red>-{currentTaxAmount:N0}원</color>";

        if (animate && textNetIncome != null)
        {
            DOVirtual.Float(0, netIncome, 1.5f, (value) =>
            {
                textNetIncome.text = $"순수익: <color=#008000>+{(int)value:N0}원</color>";
            }).SetEase(Ease.OutCubic).SetDelay(0.5f);
        }
        else
        {
            textNetIncome.text = $"순수익: <color=#008000>+{netIncome:N0}원</color>";
        }

        buttonPayTaxAndNext.interactable = true;
        if (textPayButton != null) textPayButton.text = "세금 납부 및 다음 단계";
    }

    private void AnimateMoneyText(TextMeshProUGUI textUI, int start, int end, string prefix, string color, string sign)
    {
        DOVirtual.Float(start, end, 1f, (value) =>
        {
            textUI.text = $"{prefix}: <color={color}>{sign}{(int)value:N0}원</color>";
        }).SetEase(Ease.OutQuart).SetDelay(0.3f);
    }

    // ✨ [핵심] 패널 전환 애니메이션 (슬라이드 & 페이드)
    // ✨ [수정] 패널 전환 (기존 Y좌표 유지하도록 변경)
    // ✨ [확인] 지난번 수정했던 SwitchPanel (혹시 모르니 다시 확인)
    private void SwitchPanel(GameObject nextPanel, int stepForInit = -1)
    {
        if (currentActivePanel == nextPanel) return;

        float moveDistance = 500f;
        float duration = 0.4f;

        // 1. 현재 패널 퇴장
        if (currentActivePanel != null)
        {
            GameObject oldPanel = currentActivePanel;
            RectTransform oldRect = oldPanel.GetComponent<RectTransform>();
            CanvasGroup oldCg = oldPanel.GetComponent<CanvasGroup>();

            // 현재 Y위치 기억
            float originalY = oldRect.anchoredPosition.y;

            oldRect.DOAnchorPosX(-moveDistance, duration).SetEase(Ease.InQuad);
            if (oldCg != null) oldCg.DOFade(0f, duration);

            DOVirtual.DelayedCall(duration, () =>
            {
                oldPanel.SetActive(false);
                // 복구할 때도 Y위치는 원래대로 유지
                oldRect.anchoredPosition = new Vector2(0, originalY);
            });
        }

        // 2. 다음 패널 입장
        if (nextPanel != null)
        {
            nextPanel.SetActive(true);

            if (stepForInit == 3)
            {
                PrepareClosingChecklist();
                UpdateCloseButtonState();
            }

            RectTransform newRect = nextPanel.GetComponent<RectTransform>();
            CanvasGroup newCg = nextPanel.GetComponent<CanvasGroup>();

            // 시작 위치: X는 오른쪽(moveDistance), Y는 기존 설정 유지
            newRect.anchoredPosition = new Vector2(moveDistance, newRect.anchoredPosition.y);

            if (newCg != null) newCg.alpha = 0f;

            // X축만 0으로 이동 (Y축은 건드리지 않음)
            newRect.DOAnchorPosX(0, duration).SetEase(Ease.OutQuad);
            if (newCg != null) newCg.DOFade(1f, duration);

            currentActivePanel = nextPanel;
        }
    }

    // ✨ 버튼 타격감 연출 (Punch)
    private void PunchButton(Button btn)
    {
        if (btn != null)
        {
            // 크기가 살짝(0.1 정도) 줄어들었다가 띠용 하고 복구됨
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.2f, 10, 1);
        }
    }

    // ... (PrepareClosingChecklist, UpdateCloseButtonState, Toggle 관련 함수들 동일) ...
    private void PrepareClosingChecklist()
    {
        checkTaxToggle.isOn = isTaxPaid;
        checkReciptToggle.isOn = false;
        checkIngredientToggle.isOn = false;
        checkAllToggle.isOn = false;
    }
    private void UpdateCloseButtonState() => buttonFinalClose.interactable = checkAllToggle.isOn;
    private void OnToggleAllChanged(bool isOn)
    {
        if (isUpdating) return;
        isUpdating = true;
        checkReciptToggle.isOn = isOn;
        checkTaxToggle.isOn = isOn;
        checkIngredientToggle.isOn = isOn;
        isUpdating = false;
        UpdateCloseButtonState();
    }
    private void OnIndividualToggleChanged()
    {
        if (isUpdating) return;
        isUpdating = true;
        bool allOn = checkReciptToggle.isOn && checkTaxToggle.isOn && checkIngredientToggle.isOn;
        checkAllToggle.isOn = allOn;
        isUpdating = false;
        UpdateCloseButtonState();
    }

    // ✨ [수정] 최종 마감 버튼
    private void OnFinalCloseClicked()
    {
        if (!checkAllToggle.isOn)
        {
            PunchButton(buttonFinalClose); // 안 눌릴 때도 흔들어주기
            TooltipManager.ShowFollowMouse(TooltipType.UI, "모든 마감 항목을 확인해주세요!", 1f);
            return;
        }

        PunchButton(buttonFinalClose); // 눌림 효과

        IsShutterAnimating = true;

        Debug.Log("[마감] 영업 종료. 셔터 연출 시작");
        buttonFinalClose.interactable = false;

        Sequence closeSeq = DOTween.Sequence().SetUpdate(true);

        if (shutterRect != null)
        {
            shutterRect.DOKill();
            shutterRect.gameObject.SetActive(true);
            shutterRect.SetAsLastSibling();

            Image shutterImg = shutterRect.GetComponent<Image>();
            if (shutterImg != null)
            {
                Color c = shutterImg.color;
                shutterImg.color = new Color(c.r, c.g, c.b, 1f);
            }

            float height = shutterRect.rect.height;
            if (height == 0) height = 1920f;

            shutterRect.anchoredPosition = new Vector2(0, height);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(116);

            closeSeq.Append(shutterRect.DOAnchorPos(Vector2.zero, shutterMoveDuration).SetEase(Ease.OutBounce));
        }
        //Tutorial중이라면 중단
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorial)
        {
            Debug.Log("[튜토리얼] 셔터를 내린 채로 대기합니다.");
            // 여기서 return을 해서 뒷부분(GameManager.Instance.StartOfDay 호출 등)이 실행되지 않게 합니다.
            return;
        }

        if (mainWindowRect != null) closeSeq.Join(mainWindowRect.DOScale(0f, 0.3f));
        if (blackCurtainImage != null) closeSeq.Join(blackCurtainImage.DOFade(0f, 0.3f));

        // ✨ [핵심 수정] 셔터가 내려간 뒤, '다음 날 세팅(StartOfDay)'을 하고 나서 바로 저장!

        gameObject.SetActive(false);

        closeSeq.AppendCallback(() =>
        {

            // ✨ [NEW] "이제 정산 끝났어!"라고 명시적으로 알려줍니다.
            // 이걸 안 하면 SaveGame()이 "어 아직 정산 중인가 봐" 하고 마감 창 켜진 상태로 저장해버립니다.
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.IsSettlementPhase = false;
            }
            if (GameDataLogger.Instance != null)
            {
                GameDataLogger.Instance.SaveDailyLog();

                // ✨ (중요) 저장 후 내일 기록을 위해 초기화하는 함수가 있다면 여기서 호출!
                GameDataLogger.Instance.StartNewDayLog(PlayerWalletManager.Instance.CurrentBalance);
            }

            // 1. 날짜 변경, 화구 초기화 등 '내일' 준비
            GameManager.Instance.StartOfDay();

            // 2. 다음 날 상태로 자동 저장 (이제 clean한 상태로 저장됨)
            GameSaveManager.Instance.SaveGame();
            Debug.Log("[시스템] 다음 영업일 시작 상태로 자동 저장되었습니다.");
        });
        closeSeq.AppendInterval(shutterStayDelay);

        if (shutterRect != null)
        {
            float height = shutterRect.rect.height;
            closeSeq.Append(shutterRect.DOAnchorPos(new Vector2(0, height), shutterMoveDuration).SetEase(Ease.InQuad));
        }

        closeSeq.OnComplete(() =>
        {
            if (blackCurtainImage != null) blackCurtainImage.gameObject.SetActive(false);
            if (shutterRect != null) shutterRect.gameObject.SetActive(false);

            IsShutterAnimating = false;

            GameManager.Instance.StartDayGameplay();
        });
    }

    // ✨ [수정] 세금 납부 및 다음 단계
    private void OnPayTaxAndNextClicked()
    {
        PunchButton(buttonPayTaxAndNext);

        if (isTaxPaid)
        {
            GoToShopStep(); // 중복 코드 방지를 위해 함수로 분리하거나, 아래 로직 수행
            return;
        }

        if (PlayerWalletManager.Instance.Spend(currentTaxAmount))
        {
            Debug.Log($"[세금 납부] {currentTaxAmount:N0}원 납부 완료");

            // ✨ [NEW] 세금 납부 기록
            if (GameDataLogger.Instance != null)
            {
                GameDataLogger.Instance.AddTaxExpense(currentTaxAmount);
            }
            isTaxPaid = true;
            GoToShopStep(); // 상점 진입
        }
        else
        {
            Debug.LogWarning("[세금 납부 실패] 잔액이 부족합니다.");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "잔액이 부족하여 세금을 낼 수 없습니다!", 2f);
        }
    }

    // ✨ [NEW] 상점 진입 처리 및 대화 시작
    private void GoToShopStep()
    {
        IngredientShop.OpenShop();
        SwitchPanel(panelShop); // 패널 전환

        // ✨ 여기서 대화 시작! (상점 화면이 보일 때)
        if (!hasShownBonusDialogue)
        {
            hasShownBonusDialogue = true; // 한번만 실행되게 잠금

            // 튜토리얼 중이 아닐 때만 실행
            // (GameManager가 싱글톤이므로 바로 접근 가능)
            if (TutorialManager.Instance == null && GameManager.Instance.dialogueRunner != null)
            {
                // Yarn 대화 시작 ("내일의 보너스는...")
                GameManager.Instance.dialogueRunner.StartDialogue("TomorrowBonusLine");
            }
        }
    }

    // ✨ [수정] 상점 완료 및 다음 단계
    private void OnOrderAndNextClicked()
    {
        // 외부 버튼일 경우 Punch 효과, 내부 이벤트 호출일 경우 null 체크
        if (buttonOrderAndNext != null && buttonOrderAndNext.gameObject.activeSelf)
            PunchButton(buttonOrderAndNext);
        else if (IngredientShop != null && IngredientShop.orderButton != null)
            PunchButton(IngredientShop.orderButton);

        Debug.Log("[상점] 주문 단계 완료");
        SwitchPanel(panelClosing, stepForInit: 3); // ✨ 슬라이드 전환 (마감 체크 초기화 포함)
    }

    public void FillIngredientData()
    {
        IngredientStockManager.Instance.UpdateLowStockList();
        lowStockTextUI.text = IngredientStockManager.Instance.GetLowStockText();
        textLowStockCost.text = IngredientStockManager.Instance.GetLowStockCostSummaryText();
    }

    private IEnumerator ResetScrollCoroutine(ScrollRect scroll)
    {
        if (scroll == null || scroll.content == null) yield break;

        // 1. CanvasGroup 가져오기 (OnEnable에서 추가했겠지만 안전하게 확인)
        CanvasGroup cg = scroll.GetComponent<CanvasGroup>();
        if (cg == null) cg = scroll.gameObject.AddComponent<CanvasGroup>();

        // 🚨 중요: 이미 OnEnable에서 alpha가 0이 되어 있습니다.
        // 여기서 다시 0으로 만들 필요는 없지만, 확실히 하기 위해 유지해도 무방합니다.

        // 2. 물리 엔진 차단
        scroll.velocity = Vector2.zero;
        scroll.StopMovement();
        scroll.enabled = false;

        // 3. 데이터 반영 대기
        yield return null;

        // 4. 레이아웃 강제 갱신
        var contentRect = scroll.content;
        var tmps = contentRect.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var tmp in tmps) tmp.ForceMeshUpdate();

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        // 5. 📍 좌표 강제 고정 (안 보이는 상태에서 몰래 이동)
        // 맨 위(0)로 순간이동!
        Vector2 finalPos = contentRect.anchoredPosition;
        finalPos.y = 0f;
        contentRect.anchoredPosition = finalPos;

        // 6. ScrollRect 부활
        scroll.enabled = true;

        // 7. ✨ [피날레] 이제 위치가 완벽하니 스르륵 보여줍니다.
        cg.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
    }

    // ✨ [NEW] 1. 셔터를 올리고 가게 문을 여는 애니메이션 (튜토리얼 직후용)
    public void PlayOpenShutterAnimation()
    {
        isShutterMode = true;

        gameObject.SetActive(true); // 핸들러 자체 활성화

        // 1. 초기 상태 설정: 셔터와 암전 커튼이 꽉 닫혀있어야 함
        if (shutterRect != null)
        {
            shutterRect.gameObject.SetActive(true);
            shutterRect.anchoredPosition = Vector2.zero; // 화면 중앙(닫힘)
            shutterRect.SetAsLastSibling(); // 제일 앞으로
        }

        if (blackCurtainImage != null)
        {
            blackCurtainImage.gameObject.SetActive(true);
            Color c = blackCurtainImage.color;
            c.a = 1f; // 완전 불투명
            blackCurtainImage.color = c;
        }

        // 2. 애니메이션 시퀀스
        Sequence openSeq = DOTween.Sequence();

        // (1) 잠시 대기 (로딩 직후 깜빡임 방지)
        openSeq.AppendInterval(0.5f);

        // (2) 셔터 위로 올리기
        if (shutterRect != null)
        {
            float height = shutterRect.rect.height;
            if (height == 0) height = 1920f; // 안전장치

            // 위로 이동 (Y축 height만큼)
            openSeq.Append(shutterRect.DOAnchorPosY(height, shutterMoveDuration).SetEase(Ease.OutQuad));
        }

        // (3) 동시에 암전 커튼 페이드 아웃
        if (blackCurtainImage != null)
        {
            openSeq.Join(blackCurtainImage.DOFade(0f, 0.5f));
        }

        // 3. 종료 후 정리
        openSeq.OnComplete(() =>
        {
            if (shutterRect != null) shutterRect.gameObject.SetActive(false);
            if (blackCurtainImage != null) blackCurtainImage.gameObject.SetActive(false);

            // ✨ [핵심] 다 끝났으면 플래그 해제하고 오브젝트 끄기
            isShutterMode = false;
            gameObject.SetActive(false);
        });
    }

    // ✨ [NEW] 2. 셔터 없이 즉시 가게 열기 (스킵, 이어하기용)
    public void ForceOpenShutterImmediately()
    {
        // 애니메이션 없이 바로 끈 상태로 만듦
        if (shutterRect != null)
        {
            shutterRect.anchoredPosition = new Vector2(0, 2000); // 시야 밖으로
            shutterRect.gameObject.SetActive(false);
        }

        if (blackCurtainImage != null)
        {
            Color c = blackCurtainImage.color;
            c.a = 0f;
            blackCurtainImage.color = c;
            blackCurtainImage.gameObject.SetActive(false);
        }

        // 핸들러가 켜져 있으면 안 되므로 비활성화
        gameObject.SetActive(false);
    }
}