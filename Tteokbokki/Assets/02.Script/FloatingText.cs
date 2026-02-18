using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private float moveDistance = 100f; // 위로 얼마나 올라갈지
    [SerializeField] private float duration = 1.0f;     // 애니메이션 시간

    public void Setup(string text, Color color)
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();

        tmpText.text = text;
        tmpText.color = color;
        tmpText.alpha = 1f; // 처음엔 불투명

        // ✨ DOTween 연출
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // 1. 위로 올라가기
        seq.Append(transform.DOMoveY(transform.position.y + moveDistance, duration).SetEase(Ease.OutQuad));

        // 2. 절반 지난 시점부터 서서히 투명해지기
        seq.Join(tmpText.DOFade(0f, duration * 0.5f).SetDelay(duration * 0.5f));

        // 3. 끝나면 삭제 (오브젝트 풀링을 쓴다면 Deactivate)
        seq.OnComplete(() => Destroy(gameObject));
    }
}