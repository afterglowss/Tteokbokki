using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("프리팹 연결")]
    [SerializeField] private GameObject floatingTextPrefab; // 위에서 만든 FloatingText가 붙은 프리팹
    [SerializeField] private Transform canvasTransform;     // UI들이 모여있는 캔버스 (부모)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 돈 획득 연출을 띄웁니다.
    /// </summary>
    /// <param name="position">효과가 나타날 화면 좌표 (보통 영수증의 transform.position)</param>
    /// <param name="amount">금액</param>
    /// <param name="isBonus">보너스 여부 (색상 변경용)</param>
    public void ShowMoneyPopup(Vector3 position, int amount, bool isBonus = false)
    {
        if (floatingTextPrefab == null || canvasTransform == null) return;

        // 1. 프리팹 생성
        GameObject obj = Instantiate(floatingTextPrefab, canvasTransform);

        // 2. 위치 설정 (월드 좌표 -> 그대로 사용하거나 스크린 좌표 변환 필요. 
        // 보통 UI끼리라면 그냥 position 넣으면 됩니다.)
        // ✨ [수정] 2. 위치 설정 (Z축 문제 방지)
        // 전달받은 월드 좌표를 그대로 쓰되, Z값만 날려버립니다.
        obj.transform.position = new Vector3(position.x, position.y, 0f);

        // ✨ [핵심] 3. 스케일 및 회전 초기화 (이게 없으면 엄청 커지거나 돌아가있을 수 있음)
        obj.transform.localScale = Vector3.one;
        obj.transform.localRotation = Quaternion.identity;

        // 3. 텍스트 및 색상 설정
        FloatingText floatingText = obj.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            // 금액 포맷팅 (+15,000)
            string text = $"+{amount:N0}";

            // 색상: 보너스면 황금색, 일반이면 초록색
            Color color = isBonus ? new Color(1f, 0.8f, 0f) : new Color(0.2f, 1f, 0.2f);

            floatingText.Setup(text, color);
        }
    }
}