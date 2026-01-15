using DG.Tweening; // DOTween 필수
using System.Collections;
using System.Collections.Generic; // List 사용
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfDayUIHandler : MonoBehaviour
{
    [Header("Black Curtain")]
    [SerializeField] private Image blackCurtainImage; // 뒤쪽 배경을 가려줄 반투명 이미지
    [SerializeField] private float fadeDuration = 0.5f;
    [Range(0f, 1f)][SerializeField] private float curtainMaxAlpha = 0.7f; // 커튼의 최대 불투명도

    [Header("Animation References")]
    [SerializeField] private RectTransform mainWindowRect; // 창 전체의 RectTransform (배경 포함)
    [SerializeField] private Image[] macDecorationButtons; // 빨, 노, 초 장식 이미지들 (없으면 비워도 됨)
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

    // ... (Step 2, 3 변수들은 기존 유지) ...
    [Header("Step 2: 상점 UI")]
    [SerializeField] private TextMeshProUGUI lowStockTextUI;
    [SerializeField] private TextMeshProUGUI textLowStockCost;
    [SerializeField] private Button buttonOrderAndNext;

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

    // ... Start 함수 기존 유지 ...
    private void Start()
    {
        buttonPayTaxAndNext.onClick.AddListener(OnPayTaxAndNextClicked);
        buttonOrderAndNext.onClick.AddListener(OnOrderAndNextClicked);
        buttonFinalClose.onClick.AddListener(OnFinalCloseClicked);

        checkAllToggle.onValueChanged.AddListener(OnToggleAllChanged);
        checkReciptToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
        checkTaxToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
        checkIngredientToggle.onValueChanged.AddListener(_ => OnIndividualToggleChanged());
        if (blackCurtainImage != null)
        {
            blackCurtainImage.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 1. 데이터 초기화 (텍스트는 비워두거나 0으로 시작)
        InitializeSettlementData(animate: true);

        // ✨ [NEW] 2. 배경 커튼 Fade In (창 팝업 전에 먼저 어두워짐)
        if (blackCurtainImage != null)
        {
            // 1. 오브젝트를 먼저 켭니다 (Raycast를 막기 위해)
            blackCurtainImage.gameObject.SetActive(true);

            // 2. 투명하게 초기화 (알파 0)
            Color c = blackCurtainImage.color;
            blackCurtainImage.color = new Color(c.r, c.g, c.b, 0f);

            // 3. 부드럽게 어두워짐
            blackCurtainImage.DOFade(curtainMaxAlpha, fadeDuration).SetEase(Ease.OutQuad);
        }

        // 3. 창 팝업 연출 시작 (약간의 딜레이를 주면 더 자연스러울 수 있음)
        DOVirtual.DelayedCall(0.1f, PlayWindowOpenAnimation);

        GoToStep(1);
    }

    // ✨ [NEW] 창 열리는 연출
    private void PlayWindowOpenAnimation()
    {
        // 초기화: 창은 작게, 장식 버튼은 안 보이게
        if (mainWindowRect != null)
        {
            mainWindowRect.localScale = Vector3.zero;
        }

        foreach (var btn in macDecorationButtons)
        {
            if (btn != null) btn.transform.localScale = Vector3.zero;
        }

        // Sequence로 연출 묶기
        Sequence seq = DOTween.Sequence();

        // 1. 창이 뿅! 하고 커짐 (Back 이징으로 쫀득하게)
        if (mainWindowRect != null)
        {
            seq.Append(mainWindowRect.DOScale(1f, windowPopDuration).SetEase(Ease.OutBack));
        }

        // 2. 빨/노/초 버튼이 따-다-닥 하고 나타남
        for (int i = 0; i < macDecorationButtons.Length; i++)
        {
            if (macDecorationButtons[i] != null)
            {
                // 약간의 시차를 두고 튀어나옴
                seq.Insert(windowPopDuration * 0.7f + (i * 0.1f),
                    macDecorationButtons[i].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            }
        }
    }

    private void InitializeSettlementData(bool animate = false)
    {
        isTaxPaid = false;

        int successTotal = 0;
        int missedTotal = 0;

        if (ReceiptLineManager.Instance != null)
        {
            textSuccess.text = ReceiptLineManager.Instance.GetTodaySuccessfulReceiptsText();
            textMissed.text = ReceiptLineManager.Instance.GetTodayMissedReceiptsText();

            successTotal = ReceiptLineManager.Instance.GetTotalSuccessfulAmount();
            missedTotal = ReceiptLineManager.Instance.GetTotalMissedAmount();

            // ✨ 숫자 카운팅 연출 (성공 금액)
            if (textSuccessAmount != null)
            {
                if (animate) AnimateMoneyText(textSuccessAmount, 0, successTotal, "총 성공 금액", "green", "+");
                else textSuccessAmount.text = $"총 성공 금액: <color=green>+{successTotal:N0}원</color>";
            }

            // ✨ 숫자 카운팅 연출 (실패 금액)
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

        // ✨ 하단 텍스트는 애니메이션 없이 표시하거나, 원한다면 여기도 카운팅 적용 가능
        textTotalAndTax.text = $"총 매출: {grossIncome:N0}원\n세금: <color=red>-{currentTaxAmount:N0}원</color>";

        // 순수익도 카운팅하면 멋짐!
        if (animate && textNetIncome != null)
        {
            // 순수익 카운팅 (0 -> netIncome)
            DOVirtual.Float(0, netIncome, 1.5f, (value) =>
            {
                textNetIncome.text = $"순수익: <color=green>+{(int)value:N0}원</color>";
            }).SetEase(Ease.OutCubic).SetDelay(0.5f); // 창 뜨고 0.5초 뒤 시작
        }
        else
        {
            textNetIncome.text = $"순수익: <color=green>+{netIncome:N0}원</color>";
        }


        buttonPayTaxAndNext.interactable = true;
        if (textPayButton != null) textPayButton.text = "세금 납부 및 다음 단계";
    }

    // ✨ 숫자 카운팅 헬퍼 함수
    private void AnimateMoneyText(TextMeshProUGUI textUI, int start, int end, string prefix, string color, string sign)
    {
        // 0.5초 딜레이 후 1초 동안 카운팅
        DOVirtual.Float(start, end, 1f, (value) =>
        {
            textUI.text = $"{prefix}: <color={color}>{sign}{(int)value:N0}원</color>";
        }).SetEase(Ease.OutQuart).SetDelay(0.3f);
    }

    // ... (나머지 GoToStep, 버튼 클릭 이벤트 등 기존 함수 그대로 유지) ...
    private void GoToStep(int step)
    {
        panelSettlement.SetActive(step == 1);
        panelShop.SetActive(step == 2);
        panelClosing.SetActive(step == 3);

        if (step == 3)
        {
            PrepareClosingChecklist();
            UpdateCloseButtonState();
        }
    }

    private void PrepareClosingChecklist()
    {
        checkTaxToggle.isOn = isTaxPaid;
        checkReciptToggle.isOn = false;
        checkIngredientToggle.isOn = false;
        checkAllToggle.isOn = false;
    }
    private void UpdateCloseButtonState()
    {
        buttonFinalClose.interactable = checkAllToggle.isOn;
    }
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
    private void OnFinalCloseClicked()
    {
        if (!checkAllToggle.isOn)
        {
            TooltipManager.ShowFollowMouse(TooltipType.UI, "모든 마감 항목을 확인해주세요!", 1f);
            return;
        }

        Debug.Log("[마감] 영업 종료. 연출 후 다음 날로 넘어갑니다.");

        // 중복 클릭 방지
        buttonFinalClose.interactable = false;

        // 퇴장 연출 시퀀스
        Sequence closeSeq = DOTween.Sequence();

        // 1. 창이 작아지며 사라짐 (InBack 이징으로 빨려들어가듯이)
        if (mainWindowRect != null)
        {
            closeSeq.Append(mainWindowRect.DOScale(0f, windowPopDuration * 0.8f).SetEase(Ease.InBack));
        }

        // ✨ [수정] 페이드 아웃 로직
        if (blackCurtainImage != null)
        {
            closeSeq.Join(blackCurtainImage.DOFade(0f, fadeDuration).SetEase(Ease.OutQuad));
        }

        // ✨ [중요] 연출이 다 끝나면(OnComplete) 그제서야 커튼을 끄고 다음날로 넘어감
        closeSeq.OnComplete(() =>
        {
            if (blackCurtainImage != null)
                blackCurtainImage.gameObject.SetActive(false); // 여기서 끕니다!

            gameObject.SetActive(false);
            GameManager.Instance.StartOfDay();
        });
    }
    private void OnPayTaxAndNextClicked()
    {
        if (isTaxPaid)
        {
            IngredientShop.OpenShop();
            GoToStep(2);
            return;
        }
        if (PlayerWalletManager.Instance.Spend(currentTaxAmount))
        {
            Debug.Log($"[세금 납부] {currentTaxAmount:N0}원 납부 완료");
            isTaxPaid = true;
            IngredientShop.OpenShop();
            GoToStep(2);
        }
        else
        {
            Debug.LogWarning("[세금 납부 실패] 잔액이 부족합니다.");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "잔액이 부족하여 세금을 낼 수 없습니다!", 2f);
        }
    }
    private void OnOrderAndNextClicked()
    {
        Debug.Log("[상점] 주문 단계 완료");
        PrepareClosingChecklist();
        GoToStep(3);
    }
    public void FillIngredientData()
    {
        IngredientStockManager.Instance.UpdateLowStockList();
        lowStockTextUI.text = IngredientStockManager.Instance.GetLowStockText();
        textLowStockCost.text = IngredientStockManager.Instance.GetLowStockCostSummaryText();
    }
}