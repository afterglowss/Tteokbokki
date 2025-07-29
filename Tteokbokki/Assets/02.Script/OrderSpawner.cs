using System;
using System.Collections;
using UnityEngine;

public class OrderSpawner : MonoBehaviour
{
    public static OrderSpawner Instance { get; private set; }
    [Header("기본 설정")]
    [Tooltip("주문 생성 시도 주기 (초)")]
    public float attemptInterval = 0.5f;

    [Tooltip("기본 생성 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float baseOrderProbability = 0.1f;

    [Tooltip("전날 성공률 (GameManager에서 설정)")]
    [Range(0f, 1f)]
    public float previousDaySuccessRate = 0.5f;
    public void SetPreviousDaySuccessRate(float successRate) => previousDaySuccessRate = successRate;

    [Tooltip("영수증 생성기 연결")]
    public RandomReceiptGenerator generator;

    [Tooltip("생성 시 딜레이 범위 (초)")]
    public Vector2 delayRangeSeconds = new Vector2(0.5f, 2.0f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InvokeRepeating(nameof(TryOrder), 0f, attemptInterval);
    }

    private void TryOrder()
    {
        float probability = CalculateCurrentOrderProbability();
        float roll = UnityEngine.Random.value;

        if (roll <= probability)
        {
            float delay = UnityEngine.Random.Range(delayRangeSeconds.x, delayRangeSeconds.y);
            StartCoroutine(DelayedOrderSpawn(delay));
        }
    }

    private IEnumerator DelayedOrderSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        generator.GenerateAndDisplayReceipt();
        Debug.Log($"[OrderSpawner] 랜덤 딜레이 주문 생성됨 ({GameClock.gameTime:HH:mm})");
    }

    private float CalculateCurrentOrderProbability()
    {
        float performanceFactor = Mathf.Clamp(previousDaySuccessRate * 1.2f, 0.3f, 0.8f);
        float timeFactor = GetTimeBuzzFactor(GameClock.gameTime.Hour);
        float dayFactor = GetWeekdayBuzzFactor(GameClock.gameTime.DayOfWeek);

        float diversityFactor = GetIngredientDiversityFactor();

        return baseOrderProbability * performanceFactor * timeFactor * dayFactor * diversityFactor;
    }

    private float GetTimeBuzzFactor(int hour)
    {
        if (hour >= 12 && hour < 14) return 1.5f; // 점심
        if (hour >= 17 && hour < 20) return 1.8f; // 저녁
        return 0.8f; // 일반 시간
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

        if (count >= 20)
            return 1.0f;  // 영향 없음
        if (count >= 17)
            return 0.95f;
        if (count >= 14)
            return 0.9f;
        if (count >= 11)
            return 0.85f;
        if (count >= 8)
            return 0.8f;
        return 0.75f;  // 8개 미만은 가장 큰 패널티
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(TryOrder));
    }
    public void RestartSpawning()
    {
        CancelInvoke(nameof(TryOrder));  // 혹시 몰라 먼저 취소
        InvokeRepeating(nameof(TryOrder), 0f, attemptInterval);
    }
}
