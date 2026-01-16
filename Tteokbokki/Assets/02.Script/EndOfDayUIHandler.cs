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

    [Header("Shutter Animation")]
    [SerializeField] private RectTransform shutterRect; // 셔터 이미지 (반드시 Anchor가 화면 꽉 차게 설정)
    [SerializeField] private float shutterMoveDuration = 0.6f; // 셔터 이동 시간
    [SerializeField] private float shutterStayDelay = 0.5f;    // 셔터가 내려와서 머무는 시간
    private float screenHeight; // 화면 높이 계산용

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

    private void Awake()
    {
        if (blackCurtainImage != null) blackCurtainImage.gameObject.SetActive(false);
        if (shutterRect != null) shutterRect.gameObject.SetActive(false);
    }

    // ... Start 함수 기존 유지 ...
    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            screenHeight = canvas.GetComponent<RectTransform>().rect.height;
        }
        else
        {
            screenHeight = 1920f; // 기본값 (FHD 기준)
        }
        buttonPayTaxAndNext.onClick.AddListener(OnPayTaxAndNextClicked);
        //buttonOrderAndNext.onClick.AddListener(OnOrderAndNextClicked);

        // ✨ [수정] 상점 UI의 내부 버튼 이벤트에 "다음 단계로 이동" 기능을 연결
        if (IngredientShop != null)
        {
            // 상점에서 "주문하기" 또는 "넘어가기"를 누르면 OnOrderAndNextClicked가 실행됨
            IngredientShop.OnShopProcessFinished.AddListener(OnOrderAndNextClicked);
        }
        // 혹시 모르니 기존 외부 버튼도 유지
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
        // 1. 데이터 초기화 (텍스트는 비워두거나 0으로 시작)
        InitializeSettlementData(animate: true);

        // ✨ [NEW] 2. 배경 커튼 Fade In (창 팝업 전에 먼저 어두워짐)
        if (blackCurtainImage != null)
        {
            // 1. 오브젝트를 먼저 켭니다 (Raycast를 막기 위해)
            blackCurtainImage.gameObject.SetActive(true);
            Debug.Log("검은 천막 활성화");

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
                if (animate) AnimateMoneyText(textSuccessAmount, 0, successTotal, "총 성공 금액", "#008000", "+");
                else textSuccessAmount.text = $"총 성공 금액: <color=#008000>+{successTotal:N0}원</color>";
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
                textNetIncome.text = $"순수익: <color=#008000>+{(int)value:N0}원</color>";
            }).SetEase(Ease.OutCubic).SetDelay(0.5f); // 창 뜨고 0.5초 뒤 시작
        }
        else
        {
            textNetIncome.text = $"순수익: <color=#008000>+{netIncome:N0}원</color>";
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

        Debug.Log("[마감] 영업 종료. 셔터 연출 시작");
        buttonFinalClose.interactable = false;

        // ✨ [중요] 게임 시간이 멈춰있을 수 있으므로 시퀀스도 Unscaled Time을 쓰도록 설정
        Sequence closeSeq = DOTween.Sequence().SetUpdate(true);

        // --- 1. 셔터 내리기 준비 ---
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

            // 높이 재계산 (화면 크기가 바뀔 수 있으므로 여기서 계산 추천)
            float height = shutterRect.rect.height;
            if (height == 0) height = 1920f; // 안전장치

            // 시작 위치: 화면 위쪽 (화면 높이만큼 위로)
            shutterRect.anchoredPosition = new Vector2(0, height);

            // 셔터 내려오기 (0,0 으로)
            // ✨ .SetUpdate(true)는 위에서 Sequence에 걸었으므로 자식 트윈에도 적용됨
            closeSeq.Append(shutterRect.DOAnchorPos(Vector2.zero, shutterMoveDuration).SetEase(Ease.OutBounce));
        }

        // --- 2. 셔터 내려오는 동안 뒤쪽 정리 ---
        if (mainWindowRect != null)
        {
            closeSeq.Join(mainWindowRect.DOScale(0f, 0.3f));
        }
        if (blackCurtainImage != null)
        {
            closeSeq.Join(blackCurtainImage.DOFade(0f, 0.3f));
        }

        // --- 3. 다음 날 로직 실행 ---
        closeSeq.AppendCallback(() =>
        {
            GameManager.Instance.StartOfDay();
        });

        // --- 4. 대기 후 셔터 올리기 ---
        closeSeq.AppendInterval(shutterStayDelay);

        if (shutterRect != null)
        {
            // 다시 화면 위로 (height 만큼)
            // 안전하게 다시 계산하거나 저장된 height 사용
            float height = shutterRect.rect.height;
            closeSeq.Append(shutterRect.DOAnchorPos(new Vector2(0, height), shutterMoveDuration).SetEase(Ease.InQuad));
        }

        // --- 5. 종료 및 비활성화 ---
        closeSeq.OnComplete(() =>
        {
            if (blackCurtainImage != null) blackCurtainImage.gameObject.SetActive(false);
            if (shutterRect != null) shutterRect.gameObject.SetActive(false);

            gameObject.SetActive(false);

            GameManager.Instance.StartDayGameplay();
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