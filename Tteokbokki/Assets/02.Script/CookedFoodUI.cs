using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Image 제어를 위해 추가
using DG.Tweening;

public class CookedFoodUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Visual States")]
    public GameObject wokStateObject;     // 1. 웍 상태 오브젝트 (냄비 모양)
    public Image wokContentImage;         // 1-1. 웍 내부 음식 이미지 (스프라이트 교체용)
    public GameObject packageStateObject; // 2. 포장 상태 오브젝트 (용기 모양)

    [Header("재료 텍스트")]
    public TextMeshProUGUI ingredientsText;

    public Dictionary<string, int> Ingredients { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Canvas canvas;
    public PackagingSlot currentSlot { get; set; }
    public bool isPlacedInSlot { get; set; } = false;

    private Vector3 originalLocalPosition;
    private Transform originalParent;

    public StoveSlot originStoveSlot;

    private bool isTweening = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    // ✨ 초기화 함수 수정: 완성된 요리 이미지(Sprite)를 받음
    public void Initialize(Dictionary<string, int> ingredients, Sprite cookedSprite = null)
    {
        Ingredients = new Dictionary<string, int>(ingredients);
        UpdateText();

        originalLocalPosition = transform.localPosition;
        originalParent = transform.parent;

        // --- 시각 상태 초기화: 처음엔 웍 모양으로 시작 ---
        if (wokStateObject != null) wokStateObject.SetActive(true);
        if (packageStateObject != null) packageStateObject.SetActive(false);

        // 화구에서 넘겨준 완성된 음식 이미지를 웍 내부에 적용
        if (wokContentImage != null && cookedSprite != null)
        {
            wokContentImage.sprite = cookedSprite;
            // 혹시 투명도가 0이라면 보이게 설정
            Color c = wokContentImage.color;
            c.a = 1f;
            wokContentImage.color = c;
        }
    }

    // ✨ 포장 완료 상태로 전환하는 함수
    public void SwitchToPackedState(bool isSilent = false)
    {
        // 1. 현재 웍(냄비) 상태인지 먼저 확인합니다.
        // (wokStateObject가 켜져 있다는 건 아직 포장되지 않았다는 뜻)
        bool isTransforming = wokStateObject != null && wokStateObject.activeSelf;

        if (wokStateObject != null) wokStateObject.SetActive(false);
        if (packageStateObject != null) packageStateObject.SetActive(true);

        // 2. 웍 상태에서 -> 포장 상태로 '변신하는 순간'에만 소리 재생
        if (isTransforming && !isSilent)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(112);
        }
    }

    // ... (이하 UpdateText, Tooltip, Drag 관련 함수들은 기존과 동일하므로 유지) ...
    private void UpdateText()
    {
        string result = "";
        foreach (var kv in Ingredients) result += $"{kv.Key} x{kv.Value}\n";
        if (ingredientsText != null) ingredientsText.text = result;
    }

    private void ShowTooltip()
    {
        string tooltip = "재료:\n";
        foreach (var kv in Ingredients) tooltip += $"{kv.Key} x{kv.Value}\n";
        TooltipManager.ShowFollowMouse(TooltipType.Info, tooltip);
    }
    private void HideTooltip() => TooltipManager.Hide(TooltipType.Info);

    public void OnPointerEnter(PointerEventData eventData) => ShowTooltip();
    public void OnPointerExit(PointerEventData eventData) => HideTooltip();

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isTweening) { eventData.pointerDrag = null; return; }
        if (currentSlot != null && !currentSlot.IsTopOfStack(this)) { eventData.pointerDrag = null; return; }

        originalLocalPosition = transform.localPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f; // 드래그 중 살짝 투명하게
        transform.SetParent(canvas.transform); // 최상위 이동

        isPlacedInSlot = false;

        // ✨ [추가] 화구 위에 있던 음식이라면, 드래그 시작 시 화구 테두리 끄기
        if (originStoveSlot != null)
        {
            originStoveSlot.SetOutlineVisibility(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        isPlacedInSlot = false;

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var slot = result.gameObject.GetComponentInParent<PackagingSlot>();
            if (slot != null)
            {
                if (slot.foodStackParent.childCount >= slot.maxStackSize)
                {
                    //TooltipManager.ShowFollowMouse(TooltipType.UI, "포장 슬롯은 가득 찼습니다!");
                    ReturnToOriginal(() =>
                    {
                        // 이 코드는 0.25초 뒤, 음식이 화구에 딱 도착했을 때 실행됩니다.
                        if (originStoveSlot != null)
                        {
                            originStoveSlot.SetOutlineVisibility(true);
                        }
                    });
                    return;
                }

                slot.OnDrop(eventData); // 성공! -> PackagingSlot에서 SwitchToPackedState 호출함
                isPlacedInSlot = true;
                break;
            }
        }

        if (!isPlacedInSlot)
        {
            ReturnToOriginal(() =>
            {
                // 이 코드는 음식이 화구 위로 복귀한 뒤에 실행됩니다.
                if (originStoveSlot != null)
                {
                    originStoveSlot.SetOutlineVisibility(true);
                }
            }); // 허공에 놓으면 복귀
        }
        else
        {
            // 성공적으로 슬롯에 들어감 (정렬 애니메이션)
            transform.SetParent(currentSlot.foodStackParent);
            Vector2 target = new Vector2(0, currentSlot.GetStackIndex(this) * currentSlot.stackYOffset);
            DoTweenMove(target);

            //// ✨ [중요] 화구에서 떠났으므로 화구를 비워줌 (화구 초기화)
            //// 이 로직이 PackagingSlot.OnDrop에 없다면 여기서 호출해줘야 합니다.
            //if (originStoveSlot != null)
            //{
            //    originStoveSlot.NotifyFoodPickedUp();
            //}
        }
    }

    private void ReturnToOriginal(System.Action onComplete = null)
    {
        if (currentSlot != null) // 이미 포장된 상태였다면
        {
            transform.SetParent(currentSlot.foodStackParent);
            int index = currentSlot.GetStackIndex(this);
            Vector2 target = new Vector2(0, index * currentSlot.stackYOffset);
            DoTweenMove(target, onComplete);
        }
        else if (originStoveSlot != null) // 화구에서 막 꺼낸 상태였다면
        {
            transform.SetParent(originalParent);
            DoTweenMove(originalLocalPosition, onComplete);
        }
        else // 안전장치
        {
            transform.SetParent(originalParent);
            DoTweenMove(originalLocalPosition, onComplete);
        }
    }

    private void DoTweenMove(Vector2 targetPos, System.Action onComplete = null)
    {
        isTweening = true;
        rectTransform.DOAnchorPos(targetPos, 0.25f).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                isTweening = false;
                onComplete?.Invoke(); // ✨ 여기서 전달받은 행동 실행!
            });
    }
}