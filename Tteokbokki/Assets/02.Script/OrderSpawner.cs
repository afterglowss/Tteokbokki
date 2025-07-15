using System;
using System.Collections;
using UnityEngine;

public class OrderSpawner : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("주문 생성 시도 주기 (초)")]
    public float attemptInterval = 0.5f;

    [Tooltip("기본 생성 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float baseOrderProbability = 0.1f;

    [Tooltip("전날 성공률 (GameManager에서 설정)")]
    [Range(0f, 1f)]
    public float previousDaySuccessRate = 1.0f;

    [Tooltip("영수증 생성기 연결")]
    public RandomReceiptGenerator generator;

    [Tooltip("생성 시 딜레이 범위 (초)")]
    public Vector2 delayRangeSeconds = new Vector2(0.5f, 2.0f);

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
        float performanceFactor = Mathf.Clamp(previousDaySuccessRate * 1.2f, 0.3f, 1.0f);
        float timeFactor = GetTimeBuzzFactor(GameClock.gameTime.Hour);
        float dayFactor = GetWeekdayBuzzFactor(GameClock.gameTime.DayOfWeek);

        return baseOrderProbability * performanceFactor * timeFactor * dayFactor;
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
}
