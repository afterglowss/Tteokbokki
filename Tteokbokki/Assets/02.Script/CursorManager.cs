using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // ✨ [NEW] 새로운 인풋 시스템을 위해 추가!

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    [Header("커서 이미지 (PNG)")]
    public Texture2D defaultCursor;
    public Texture2D hoverCursor;
    public Texture2D dragCursor;

    [Header("클릭 판정 위치 (Hotspot)")]
    public Vector2 defaultHotspot = Vector2.zero;
    public Vector2 hoverHotspot = Vector2.zero;
    public Vector2 dragHotspot = Vector2.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // ✨ [핵심 수정] 마우스가 연결되어 있는지 확인하고, 왼쪽 버튼이 눌렸는지 체크합니다.
        bool isLeftClickDown = Mouse.current != null && Mouse.current.leftButton.isPressed;

        // 1순위: 마우스 왼쪽 버튼을 누르고 있을 때 (드래그/클릭)
        if (isLeftClickDown)
        {
            SetCursor(dragCursor, dragHotspot);
            return;
        }

        // 2순위: 마우스가 UI(버튼, 패널 등) 위에 올라가 있을 때 (Hover)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetCursor(hoverCursor, hoverHotspot);
            return;
        }

        // 3순위: 아무것도 안 할 때 (기본)
        SetCursor(defaultCursor, defaultHotspot);
    }

    private void SetCursor(Texture2D cursorTexture, Vector2 hotspot)
    {
        if (cursorTexture == null) return;

        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}