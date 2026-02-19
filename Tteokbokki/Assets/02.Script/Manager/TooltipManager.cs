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
    public RectTransform backgroundRect;
    public TextMeshProUGUI tooltipText;

    [Header("Style Settings")]
    public TMP_FontAsset font;
    public int fontSize = 24;

    // ✨ [NEW] 기본 오프셋을 Inspector에서 조절 가능하게 변경
    public Vector2 baseOffset = new Vector2(50f, -50f);

    private Canvas canvas;
    private RectTransform canvasRect;

    private static Dictionary<TooltipType, TooltipManager> instances = new();

    private bool followMouse = false;
    private static string currentMessage = "";

    private float autoHideTime = -1f;
    private float showTimestamp = 0f;

    private void Awake()
    {
        if (instances.ContainsKey(type))
        {
            if (instances[type] != this && instances[type] != null)
            {
                // 중복 처리
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

        if (autoHideTime > 0f && Time.unscaledTime - showTimestamp >= autoHideTime)
        {
            Hide(type);
            return;
        }

        Camera cam = canvas.worldCamera;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // ✨ [핵심 수정] 동적 오프셋 계산 (겹침 방지)
        Vector2 currentOffset = baseOffset;

        // 만약 내가 'UI' 타입인데 'Info' 타입이 이미 켜져 있다면?
        if (type == TooltipType.UI && instances.TryGetValue(TooltipType.Info, out var infoManager))
        {
            if (infoManager != null && infoManager.gameObject.activeSelf)
            {
                // Info 툴팁의 높이 + 약간의 여백(10f)만큼 더 아래로 내림
                // (기본 오프셋 Y가 음수이므로 더 빼주면 아래로 내려감)
                float infoHeight = infoManager.backgroundRect.rect.height * canvas.scaleFactor; // 캔버스 스케일 고려
                                                                                                // 또는 로컬 좌표계산이므로 단순히 rect height만 고려해도 될 수 있음. 테스트 필요 시 조정.
                                                                                                // 보통 RectTransformUtility를 쓰므로 스케일 팩터 없이 height만 써도 됩니다.

                currentOffset.y -= (infoManager.backgroundRect.sizeDelta.y + 20f);
            }
        }

        Vector2 tooltipSize = backgroundRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos + currentOffset, cam, out localPoint);

        Vector2 pivot = new Vector2(0f, 1f); // 기본: 좌상단 기준

        // 화면 밖으로 나가는 것 방지 로직 (Pivot 변경)
        if (localPoint.x + tooltipSize.x > canvasSize.x / 2f) pivot.x = 1f;
        if (localPoint.x - tooltipSize.x < -canvasSize.x / 2f) pivot.x = 0f;

        // Y축 넘침 처리는 스태킹 때문에 복잡해질 수 있으므로, 단순하게 처리하거나
        // 스태킹된 상태에서는 아래쪽 공간이 부족하면 위로 올리는 로직이 추가로 필요할 수 있음.
        if (localPoint.y - tooltipSize.y < -canvasSize.y / 2f) pivot.y = 0f;
        if (localPoint.y > canvasSize.y / 2f) pivot.y = 1f;

        backgroundRect.pivot = pivot;

        Vector3 worldPos;
        // 계산된 currentOffset을 적용
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect, mouseScreenPos + currentOffset, cam, out worldPos
        );
        backgroundRect.position = worldPos;
    }

    // ... (나머지 ApplyStyle, Show, ShowFollowMouse, Hide 함수들은 기존 그대로 유지) ...
    private void ApplyStyle()
    {
        if (tooltipText != null)
        {
            if (font != null) tooltipText.font = font;
            tooltipText.fontSize = fontSize;
        }
    }

    public static void Show(TooltipType type, string message, Vector3 worldPosition)
    {
        if (!instances.TryGetValue(type, out var manager)) return;

        manager.gameObject.SetActive(true);
        manager.tooltipText.text = message;
        LayoutRebuilder.ForceRebuildLayoutImmediate(manager.backgroundRect);

        Camera cam = manager.canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.canvasRect, screenPos, cam, out var localPoint);

        Vector2 clamped = localPoint;
        Vector2 canvasSize = manager.canvasRect.sizeDelta;

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

    public static void HideAll()
    {
        foreach (var manager in instances.Values)
        {
            if (manager != null)
            {
                manager.followMouse = false;
                manager.gameObject.SetActive(false);
            }
        }
    }
}