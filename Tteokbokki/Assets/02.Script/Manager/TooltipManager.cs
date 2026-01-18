using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public enum TooltipType
{
    Info,
    UI
}
public class TooltipManager : MonoBehaviour
{
    public TooltipType type;

    [Header("UI Components")]
    public RectTransform backgroundRect; // Content Size Fitter와 Layout Group이 달린 부모 객체
    public TextMeshProUGUI tooltipText;  // 그 자식으로 있는 텍스트

    [Header("Style Settings")]
    // public Color backgroundColor; // [삭제] 이미지 컴포넌트 색상을 그대로 사용
    public TMP_FontAsset font;
    public int fontSize = 24;
    // public Vector2 padding = new Vector2(8, 8); // [삭제] Vertical Layout Group의 Padding 사용

    private Canvas canvas;
    private RectTransform canvasRect;

    private static Dictionary<TooltipType, TooltipManager> instances = new();

    private bool followMouse = false;
    // private Vector3 followOffset; // 사용하지 않으므로 정리
    private static string currentMessage = "";

    private float autoHideTime = -1f;
    private float showTimestamp = 0f;

    private void Awake()
    {
        if (instances.ContainsKey(type))
        {
            // 중복 방지 로직 (기존 유지)
            if (instances[type] != this && instances[type] != null)
            {
                // 필요에 따라 처리
            }
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
            Hide(type);
            return;
        }

        Camera cam = canvas.worldCamera;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 offset = new Vector2(50f, -50f); // 마우스 오른쪽 아래 기본

        Vector2 tooltipSize = backgroundRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos + offset, cam, out localPoint);

        Vector2 pivot = new Vector2(0f, 1f); // 기본: 좌상단 기준

        // 화면 밖으로 나가는 것 방지 로직 (Pivot 변경 방식)
        if (localPoint.x + tooltipSize.x > canvasSize.x / 2f) pivot.x = 1f; // 오른쪽 넘침 -> 우측 기준
        if (localPoint.x - tooltipSize.x < -canvasSize.x / 2f) pivot.x = 0f;
        if (localPoint.y - tooltipSize.y < -canvasSize.y / 2f) pivot.y = 0f; // 아래 넘침 -> 하단 기준
        if (localPoint.y > canvasSize.y / 2f) pivot.y = 1f;

        backgroundRect.pivot = pivot;

        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect, mouseScreenPos + offset, cam, out worldPos
        );
        backgroundRect.position = worldPos;
    }

    private void ApplyStyle()
    {
        // 폰트 스타일만 적용하고 배경색 변경 로직은 제거
        if (tooltipText != null)
        {
            if (font != null) tooltipText.font = font;
            tooltipText.fontSize = fontSize;
        }

        // Image 색상은 Inspector에서 설정한 것을 따름
    }

    public static void Show(TooltipType type, string message, Vector3 worldPosition)
    {
        if (!instances.TryGetValue(type, out var manager)) return;

        manager.gameObject.SetActive(true);
        manager.tooltipText.text = message;

        // [중요] 텍스트가 바뀌었으니 레이아웃을 즉시 갱신 (깜빡임 방지)
        // Text가 아니라 부모인 backgroundRect를 리빌드해야 Content Size Fitter가 작동함
        LayoutRebuilder.ForceRebuildLayoutImmediate(manager.backgroundRect);

        // -- 위치 계산 로직 (기존 유지) --
        Camera cam = manager.canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.canvasRect, screenPos, cam, out var localPoint);

        Vector2 clamped = localPoint;
        Vector2 canvasSize = manager.canvasRect.sizeDelta;

        // 단순 Clamp 처리 (Pivot 보정 로직이 더 좋다면 Update 로직 참고하여 수정 가능)
        clamped.x = Mathf.Clamp(clamped.x, -canvasSize.x / 2, canvasSize.x / 2 - manager.backgroundRect.sizeDelta.x);
        clamped.y = Mathf.Clamp(clamped.y, -canvasSize.y / 2, canvasSize.y / 2 - manager.backgroundRect.sizeDelta.y);

        manager.backgroundRect.anchoredPosition = clamped;
    }

    public static void ShowFollowMouse(TooltipType type, string message, float hideAfterSeconds = -1)
    {
        if (!instances.TryGetValue(type, out var manager)) return;

        manager.gameObject.SetActive(true);
        manager.tooltipText.text = message;
        currentMessage = message;

        // [수정] 수동 크기 계산 삭제 -> Content Size Fitter에게 맡기고 강제 업데이트만 수행
        LayoutRebuilder.ForceRebuildLayoutImmediate(manager.backgroundRect);

        manager.followMouse = true;
        manager.autoHideTime = hideAfterSeconds;
        manager.showTimestamp = Time.unscaledTime;
        manager.type = type;
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