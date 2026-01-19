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
        InitializeSettlementData(animate: true);

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
    }

    private void InitializeSettlementData(bool animate = false)
    {
        // ... (기존 로직 동일) ...
        isTaxPaid = false;
        int successTotal = 0;
        int missedTotal = 0;

        if (ReceiptLineManager.Instance != null)
        {
            textSuccess.text = ReceiptLineManager.Instance.GetTodaySuccessfulReceiptsText();
            textMissed.text = ReceiptLineManager.Instance.GetTodayMissedReceiptsText();
            successTotal = ReceiptLineManager.Instance.GetTotalSuccessfulAmount();
            missedTotal = ReceiptLineManager.Instance.GetTotalMissedAmount();

            if (textSuccessAmount != null)
            {
                if (animate) AnimateMoneyText(textSuccessAmount, 0, successTotal, "총 성공 금액", "#008000", "+");
                else textSuccessAmount.text = $"총 성공 금액: <color=#008000>+{successTotal:N0}원</color>";
            }
            if (textMissedAmount != null)
            {
                if (animate) AnimateMoneyText(textMissedAmount, 0, missedTotal, "총 손실 금액", "red", "-");
                else textMissedAmount.text = $"총 손실 금액: <color=red>-{missedTotal:N0}원</color>";
            }
        }

        int grossIncome = PlayerWalletManager.Instance.TodayEarnedAmount;
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

        if (mainWindowRect != null) closeSeq.Join(mainWindowRect.DOScale(0f, 0.3f));
        if (blackCurtainImage != null) closeSeq.Join(blackCurtainImage.DOFade(0f, 0.3f));

        closeSeq.AppendCallback(() => { GameManager.Instance.StartOfDay(); });
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
            gameObject.SetActive(false);
            GameManager.Instance.StartDayGameplay();
        });
    }

    // ✨ [수정] 세금 납부 및 다음 단계
    private void OnPayTaxAndNextClicked()
    {
        PunchButton(buttonPayTaxAndNext); // 버튼 효과

        if (isTaxPaid)
        {
            IngredientShop.OpenShop();
            SwitchPanel(panelShop); // ✨ 슬라이드 전환
            return;
        }

        if (PlayerWalletManager.Instance.Spend(currentTaxAmount))
        {
            Debug.Log($"[세금 납부] {currentTaxAmount:N0}원 납부 완료");
            isTaxPaid = true;
            IngredientShop.OpenShop();
            SwitchPanel(panelShop); // ✨ 슬라이드 전환
        }
        else
        {
            Debug.LogWarning("[세금 납부 실패] 잔액이 부족합니다.");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "잔액이 부족하여 세금을 낼 수 없습니다!", 2f);
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
}