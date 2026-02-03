using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class InvertedMask : Image, ICanvasRaycastFilter
{
    [Header("하이라이트 구멍 설정")]
    [SerializeField] private RectTransform hole; // 여기에 Highlighter(StencilHole)를 넣으세요.

    public override Material materialForRendering
    {
        get
        {
            Material result = new Material(base.materialForRendering);
            result.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
            result.SetInt("_Stencil", 1);
            return result;
        }
    }

    // [수정 포인트] 'override' 키워드를 추가하여 부모의 기능을 재정의합니다.
    public override bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // 구멍(hole)이 할당되지 않았거나 꺼져 있으면 가림막이 클릭을 다 막음
        if (hole == null || !hole.gameObject.activeInHierarchy)
            return true;

        // 클릭 지점이 구멍(hole) 안쪽인지 체크
        // 안쪽이라면 false 반환 -> 가림막이 클릭을 무시함 -> 뒤에 있는 버튼이 눌림!
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(hole, sp, eventCamera);
        return !isInside;
    }
}