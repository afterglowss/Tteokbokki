using System;
using UnityEngine;
using TMPro;

public class GameClock : MonoBehaviour
{
    public TextMeshProUGUI clockText; // UI TextMeshPro 요소 연결

    [Header("초기 날짜 및 시간 설정")]
    public int startYear = 2025;
    public int startMonth = 1;
    public int startDay = 1;
    public int startHour = 12;
    public int startMinute = 0;

    private DateTime gameTime;
    private float elapsedTime = 0f;
    private const float realSecondsPerGameMinute = 3f; // 현실 3초 = 게임 1분
    public DateTime GetCurrentGameTime() => gameTime;
    void Start()
    {
        gameTime = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= realSecondsPerGameMinute)
        {
            gameTime = gameTime.AddMinutes(1); // 게임 내 1분 증가
            elapsedTime = 0f;
        }

        clockText.text = gameTime.ToString("HH:mm"); // UI 업데이트 (초 제외)
    }
}
