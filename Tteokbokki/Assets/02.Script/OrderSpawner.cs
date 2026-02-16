using System;
using System.Collections;
using UnityEngine;

public class OrderSpawner : MonoBehaviour
{
    public static OrderSpawner Instance { get; private set; }

    [Header("연결 정보")]
    public RandomReceiptGenerator generator;
    public ReceiptLineManager lineManager; // ✨ [NEW] 영수증 개수 확인용

    [Header("랜덤 생성 설정")]
    [Tooltip("주문 생성 시도 주기 (초) - 값을 늘려서 천천히 시도하게 하세요")]
    public float attemptInterval = 2.0f; // 0.5f -> 2.0f 추천

    [Tooltip("기본 생성 확률 (0.0 ~ 1.0) - 값을 낮춰서 자연 생성 빈도를 줄이세요")]
    [Range(0f, 1f)]
    public float baseOrderProbability = 0.15f;

    [Header("심심함 방지 (Failsafe)")]
    [Tooltip("영수증이 0개일 때, 몇 초 뒤에 강제로 주문을 넣을까요?")]
    public float maxEmptyDuration = 5.0f;
    private float currentEmptyTimer = 0f;

    [Header("전날 성과 (GameManager)")]
    [Range(0f, 1f)] public float previousDaySuccessRate = 0.5f;
    public void SetPreviousDaySuccessRate(float successRate) => previousDaySuccessRate = successRate;

    [Tooltip("생성 시 딜레이 범위 (초)")]
    public Vector2 delayRangeSeconds = new Vector2(0.5f, 2.0f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 마감 시간 이후라면 (정산 중이든, 잔여 처리 중이든) 자동 생성은 무조건 막아야 함
        if (GameClock.gameTime.Hour >= GameClock.closingHour)
        {
            Debug.Log("[OrderSpawner] 이미 마감 시간이므로 자동 주문 생성을 시작하지 않습니다.");
            this.enabled = false;
            return;
        }

        // ✨ [핵심 수정] 튜토리얼 중이라면 주문을 생성하지 않고 그냥 나갑니다!
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorial)
        {
            Debug.Log("[OrderSpawner] 튜토리얼 모드이므로 자동 주문 생성을 하지 않습니다.");
            return;
        }

        // 랜덤 생성 시도 루틴 시작
        StartCoroutine(RandomSpawnRoutine());
    }

    private void Update()
    {
        // ✨ [NEW] 영수증이 하나도 없는지 감시하는 로직
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorial) return; // 튜토리얼 중에는 감시하지 않음
        CheckEmptyLineStatus();
    }

    // InvokeRepeating 대신 코루틴 사용 (제어가 더 쉬움)
    private IEnumerator RandomSpawnRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorial)
            {
                yield break;
            }

            yield return new WaitForSeconds(attemptInterval);
            TryRandomOrder();
        }
    }

    // 1. 일반적인 랜덤 주문 시도
    private void TryRandomOrder()
    {
        // 만약 대기열이 꽉 찼다면 생성 시도조차 하지 않음 (선택 사항)
        // if (lineManager.GetReceiptSlots().Count >= lineManager.maxSlots) return;

        float probability = CalculateCurrentOrderProbability();
        float roll = UnityEngine.Random.value;

        if (roll <= probability)
        {
            float delay = UnityEngine.Random.Range(delayRangeSeconds.x, delayRangeSeconds.y);
            StartCoroutine(DelayedOrderSpawn(delay));
        }
    }

    // 2. 영수증 0개 감시 로직
    private void CheckEmptyLineStatus()
    {
        if (lineManager == null) return;

        // 현재 활성화된(슬롯에 있는) 영수증 개수 확인
        // (대기열 pendingReceipts까지 포함할지는 기획에 따라 결정. 보통 화면에 없으면 심심하므로 슬롯 기준)
        int activeCount = lineManager.GetReceiptSlots().Count;

        if (activeCount == 0)
        {
            currentEmptyTimer += Time.deltaTime;

            if (currentEmptyTimer >= maxEmptyDuration)
            {
                Debug.Log($"[OrderSpawner] 너무 조용해서 강제 주문 생성! ({maxEmptyDuration}초 경과)");
                // 즉시 생성 (딜레이 없이 바로 꽂아주는 게 지루함 해소에 좋음)
                generator.GenerateAndDisplayReceipt();

                // 타이머 초기화
                currentEmptyTimer = 0f;
            }
        }
        else
        {
            // 영수증이 하나라도 있으면 타이머 리셋
            currentEmptyTimer = 0f;
        }
    }

    private IEnumerator DelayedOrderSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        generator.GenerateAndDisplayReceipt();
        Debug.Log($"[OrderSpawner] 랜덤 주문 생성됨");
    }

    // ... (확률 계산 로직들은 기존 유지) ...
    private float CalculateCurrentOrderProbability()
    {
        float performanceFactor = Mathf.Clamp(previousDaySuccessRate * 1.2f, 0.3f, 0.8f);
        float timeFactor = GetTimeBuzzFactor(GameClock.gameTime);
        float dayFactor = GetWeekdayBuzzFactor(GameClock.gameTime.DayOfWeek);
        float diversityFactor = GetIngredientDiversityFactor();

        return baseOrderProbability * performanceFactor * timeFactor * dayFactor * diversityFactor;
    }

    // ... (GetTimeBuzzFactor, GetWeekdayBuzzFactor, GetIngredientDiversityFactor 등 기존 코드 유지) ...
    private float GetTimeBuzzFactor(DateTime currentTime)
    {
        int hour = currentTime.Hour;
        int minute = currentTime.Minute;

        // 1. 점심 피크 (기존 유지): 12:00 ~ 13:59
        if (hour >= 12 && hour < 14) return 1.5f;

        // 2. 저녁 피크 (수정): 17:30 ~ 19:59
        // 17시인 경우 30분 이상인지 체크, 18~19시는 무조건 피크
        if ((hour == 17 && minute >= 30) || (hour >= 18 && hour < 20))
        {
            return 1.8f;
        }

        // 3. 일반 시간
        return 0.8f;
    }

    private float GetWeekdayBuzzFactor(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Saturday or DayOfWeek.Sunday => 1.5f,
            DayOfWeek.Friday => 1.2f,
            _ => 1.0f
        };
    }
    private float GetIngredientDiversityFactor()
    {
        int count = IngredientStockManager.Instance.GetPurchasedIngredientCount();
        if (count >= 20) return 1.0f;
        if (count >= 17) return 0.95f;
        if (count >= 14) return 0.9f;
        if (count >= 11) return 0.85f;
        if (count >= 8) return 0.8f;
        return 0.75f;
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        this.enabled = false; // ✨ [핵심] Update 루프까지 통째로 비활성화
    }
    public void RestartSpawning()
    {
        this.enabled = true; // 스크립트 다시 켜기
        StopAllCoroutines();
        StartCoroutine(RandomSpawnRoutine());
    }

    public void SetSilenceMode()
    {
        StopSpawning(); // 코루틴 정지
        this.enabled = false; // 업데이트 정지
        Debug.Log("🚫 [OrderSpawner] 배드엔딩 모드: 주문 생성이 0%로 고정됩니다.");
    }
}