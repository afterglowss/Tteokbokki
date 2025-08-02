using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.Controls.AxisControl;

// Info:	음식 재료, 조리중 재료, CookedFoodUI, StoveSlot
// UI:      버튼, 안내 메시지, “재고 부족” 경고, 휴지통 안내 등

public enum TooltipType
{
    Info,
    UI
}

public class TooltipManager : MonoBehaviour
{
    public TooltipType type;

    [Header("UI Components")]
    public RectTransform backgroundRect;
    public TextMeshProUGUI tooltipText;

    [Header("Style Settings")]
    public Color backgroundColor;
    public TMP_FontAsset font;
    public int fontSize = 24;
    public Vector2 padding = new Vector2(8, 8);

    private Canvas canvas;
    private RectTransform canvasRect;

    private static Dictionary<TooltipType, TooltipManager> instances = new();

    private bool followMouse = false;
    private Vector3 followOffset;
    private static string currentMessage = "";

    private float autoHideTime = -1f; // 음수면 무한 지속
    private float showTimestamp = 0f;

    private void Awake()
    {
        if (instances.ContainsKey(type))
        {
            Debug.LogWarning($"툴팁 타입 '{type}'에 해당하는 TooltipManager가 이미 존재합니다. 기존 것을 대체합니다.");
            Destroy(instances[type].gameObject);
        }

        instances[type] = this;

        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        gameObject.SetActive(false);
        ApplyStyle();
    }

    private void Update()
    {
        if (!followMouse || !gameObject.activeSelf) return;

        // 자동 숨김 체크
        if (autoHideTime > 0f && Time.unscaledTime - showTimestamp >= autoHideTime)
        {
            Hide(type);  // type은 인스턴스가 들고 있어야 함
            return;
        }

        Camera cam = canvas.worldCamera;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        //// 기본 오프셋: 왼쪽 위
        //Vector2 offset = new Vector2(50f, 50f);

        //// World point 변환
        //Vector3 worldPos;
        //RectTransformUtility.ScreenPointToWorldPointInRectangle(
        //    canvasRect, mouseScreenPos + offset, cam, out worldPos
        //);

        //// 툴팁 위치 지정 (World 좌표)
        //backgroundRect.position = worldPos;

        //// 화면 밖으로 벗어나는 경우 위치 반전
        //Vector2 tooltipSize = backgroundRect.sizeDelta;
        //Vector2 canvasSize = canvasRect.sizeDelta;

        //// anchoredPosition으로 일단 캔버스 기준 위치를 얻기
        //Vector2 localPoint;
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos + offset, cam, out localPoint);

        //Vector2 targetPos = localPoint;

        //float canvasHalfX = canvasSize.x / 2f;
        //float canvasHalfY = canvasSize.y / 2f;

        //// 보정 로직 (반전)
        //if (targetPos.x + tooltipSize.x > canvasHalfX)  // 오른쪽 넘침
        //    offset.x = Mathf.Abs(offset.x) * -1; // 왼쪽으로
        //if (targetPos.x < -canvasHalfX)           // 왼쪽 넘침
        //    offset.x = Mathf.Abs(offset.x);       // 오른쪽으로

        //if (targetPos.y > canvasHalfY)            // 위 넘침
        //    offset.y = -Mathf.Abs(offset.y);      // 아래로
        //if (targetPos.y - tooltipSize.y < -canvasHalfY) // 아래 넘침
        //    offset.y = Mathf.Abs(offset.y);       // 위로

        //// 최종 위치 재계산
        //RectTransformUtility.ScreenPointToWorldPointInRectangle(
        //    canvasRect, mouseScreenPos + offset, cam, out worldPos
        //);
        //backgroundRect.position = worldPos;

        //// 디버그
        //Debug.Log($"[Tooltip] Mouse: {mouseScreenPos}, Offset: {offset}, WorldPos: {worldPos}");

        // 기본 오프셋 (툴팁을 약간 위/오른쪽에 위치시키기 위해)
        Vector2 offset = new Vector2(50f, 50f);

        // 툴팁 크기와 캔버스 크기
        Vector2 tooltipSize = backgroundRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        // 마우스 위치 + 오프셋을 캔버스 로컬 좌표로 변환
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos + offset, cam, out localPoint);

        Vector2 pivot = new Vector2(0f, 1f); // 기본은 왼쪽 위

        // 오른쪽 화면을 넘으면 pivot.x = 1 (툴팁이 오른쪽 끝에서 왼쪽으로 튐)
        if (localPoint.x + tooltipSize.x > canvasSize.x / 2f)
            pivot.x = 1f;

        // 왼쪽 화면을 넘으면 pivot.x = 0
        if (localPoint.x - tooltipSize.x < -canvasSize.x / 2f)
            pivot.x = 0f;

        // 아래로 화면을 넘으면 pivot.y = 0 (툴팁이 아래 끝에서 위로 올라옴)
        if (localPoint.y - tooltipSize.y < -canvasSize.y / 2f)
            pivot.y = 0f;

        // 위로 화면을 넘으면 pivot.y = 1
        if (localPoint.y > canvasSize.y / 2f)
            pivot.y = 1f;

        // 적용
        backgroundRect.pivot = pivot;

        // pivot을 바꿨으니 다시 World 좌표로 변환해 위치 지정
        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect, mouseScreenPos + offset, cam, out worldPos
        );
        backgroundRect.position = worldPos;
    }


    private void ApplyStyle()
    {
        if (tooltipText != null)
        {
            tooltipText.font = font;
            tooltipText.fontSize = fontSize;
        }

        if (backgroundRect != null)
        {
            backgroundRect.GetComponent<Image>().color = backgroundColor;
        }
    }

    public static void Show(TooltipType type, string message, Vector3 worldPosition)
    {
        if (!instances.TryGetValue(type, out var manager)) return;

        manager.gameObject.SetActive(true);
        manager.tooltipText.text = message;

        LayoutRebuilder.ForceRebuildLayoutImmediate(manager.tooltipText.rectTransform);
        Vector2 size = manager.tooltipText.rectTransform.sizeDelta;
        manager.backgroundRect.sizeDelta = size + manager.padding;


        // 정확한 위치 계산
        Camera cam = manager.canvas.worldCamera;  // 반드시 해당 Canvas의 카메라!
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.canvasRect, screenPos, cam, out var localPoint);

        // 위치 보정: 화면 경계를 넘지 않게 Clamp
        Vector2 clamped = localPoint;
        Vector2 canvasSize = manager.canvasRect.sizeDelta;

        clamped.x = Mathf.Clamp(clamped.x, 0, canvasSize.x - manager.backgroundRect.sizeDelta.x);
        clamped.y = Mathf.Clamp(clamped.y, 0, canvasSize.y - manager.backgroundRect.sizeDelta.y);

        manager.backgroundRect.anchoredPosition = clamped;
    }

    //public static void ShowFollowMouse(TooltipType type, string message)
    //{
    //    if (!instances.TryGetValue(type, out var manager)) return;

    //    manager.gameObject.SetActive(true);
    //    manager.tooltipText.text = message;
    //    currentMessage = message;

    //    LayoutRebuilder.ForceRebuildLayoutImmediate(manager.tooltipText.rectTransform);
    //    Vector2 size = manager.tooltipText.rectTransform.sizeDelta;
    //    manager.backgroundRect.sizeDelta = size + manager.padding;

    //    manager.followMouse = true;
    //}

    public static void ShowFollowMouse(TooltipType type, string message, float hideAfterSeconds = -1)
    {
        if (!instances.TryGetValue(type, out var manager)) return;

        manager.gameObject.SetActive(true);
        manager.tooltipText.text = message;
        currentMessage = message;

        LayoutRebuilder.ForceRebuildLayoutImmediate(manager.tooltipText.rectTransform);
        Vector2 size = manager.tooltipText.rectTransform.sizeDelta;
        manager.backgroundRect.sizeDelta = size + manager.padding;

        manager.followMouse = true;
        manager.autoHideTime = hideAfterSeconds;
        manager.showTimestamp = Time.unscaledTime;
        manager.type = type; // ← Hide()에서 다시 이 type을 쓸 수 있게 저장
    }


    public static void Hide(TooltipType type)
    {
        if (instances.TryGetValue(type, out var manager))
        {
            manager.followMouse = false;
            manager.gameObject.SetActive(false);
        }
    }
}
