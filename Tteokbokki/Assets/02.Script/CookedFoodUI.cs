using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

public class CookedFoodUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("재료 텍스트")]
    public TextMeshProUGUI ingredientsText;

    public Dictionary<string, int> Ingredients { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

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
    private void Update()
    {

    }

    public void Initialize(Dictionary<string, int> ingredients)
    {
        Ingredients = new Dictionary<string, int>(ingredients);
        UpdateText();

        originalLocalPosition = transform.localPosition;
        originalParent = transform.parent;
    }

    private void UpdateText()
    {
        string result = "";
        foreach (var kv in Ingredients)
        {
            result += $"{kv.Key} x{kv.Value}\n";
        }

        if (ingredientsText != null)
            ingredientsText.text = result;
    }

    private void ShowTooltip()
    {
        string tooltip = "재료:\n";
        foreach (var kv in Ingredients)
        {
            tooltip += $"{kv.Key} x{kv.Value}\n";
        }
        TooltipManager.Instance.Show(tooltip);
    }

    private void HideTooltip()
    {
        TooltipManager.Instance.Hide();
    }

    // 마우스 올라갔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isTweening)
        {
            eventData.pointerDrag = null;  // 드래그 자체를 무효화
            return;
        }

        if (currentSlot != null && !currentSlot.IsTopOfStack(this))
        {
            eventData.pointerDrag = null;
            return;
        }

        originalLocalPosition = transform.localPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
        transform.SetParent(canvas.transform); // 자유롭게 이동

        isPlacedInSlot = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //UpdateTooltipPosition();
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
                    TooltipManager.Instance.Show("포장 슬롯은 최대 4개까지 가능합니다!");

                    // 복귀 처리
                    if (currentSlot != null)
                    {
                        // 이전 슬롯으로 되돌림
                        transform.SetParent(currentSlot.foodStackParent);
                        int index = currentSlot.GetStackIndex(this);
                        if (index < 0) index = currentSlot.foodStackParent.childCount;
                        Vector2 target = new Vector2(0, index * currentSlot.stackYOffset);
                        isTweening = true;
                        rectTransform.DOAnchorPos(target, 0.25f).SetEase(Ease.OutCubic)
                            .OnComplete(() => isTweening = false);
                    }
                    else if (originStoveSlot != null)
                    {
                        // 처음 화구로 복귀
                        transform.SetParent(originalParent);
                        isTweening = true;
                        rectTransform.DOAnchorPos(originalLocalPosition, 0.25f).SetEase(Ease.OutCubic)
                            .OnComplete(() => isTweening = false);
                    }

                    return;
                }

                slot.OnDrop(eventData);
                isPlacedInSlot = true;
                break;
            }
        }

        if (!isPlacedInSlot)
        {
            // 복귀 처리 (슬롯 외 클릭 시)
            transform.SetParent(originalParent);
            isTweening = true;
            rectTransform.DOAnchorPos(originalLocalPosition, 0.25f).SetEase(Ease.OutCubic)
                .OnComplete(() => isTweening = false);
        }
        else
        {
            // 정상 드롭 시 정렬
            transform.SetParent(currentSlot.foodStackParent);
            Vector2 target = new Vector2(0, currentSlot.GetStackIndex(this) * currentSlot.stackYOffset);
            isTweening = true;
            rectTransform.DOAnchorPos(target, 0.25f).SetEase(Ease.OutCubic)
                .OnComplete(() => isTweening = false);
        }
    }



}
