using System;
using UnityEngine;
using TMPro;
using System.Data;
using System.IO;

[Serializable]
public class DateWrapper
{
    public string dateString;
}

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }
    public TextMeshProUGUI clockText; // UI TextMeshPro 요소 연결
    public TextMeshProUGUI dateText;
    public float realSecondsPerGameMinute = 2f; // 현실 3초 = 게임 1분

    [Header("영업 시간")]
    public static int openingHour = 12;
    public static int closingHour = 21;

    public static bool isPaused = false;
    private bool hasReachedClosingTime = false;

    [Header("초기 날짜 및 시간 설정")]
    public int startYear = 2025;
    public int startMonth = 1;
    public int startDay = 1;
    public int startHour = 12;
    public int startMinute = 0;

    public static DateTime gameTime;
    public DateTime GetCurrentGameTime() => gameTime;
    // 마지막 플레이 날짜 저장 경로
    private const string LastDateFileName = "LastPlayedDate.json";
    private void Awake()
    {
        gameTime = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        UpdateTimeAndDateDisplay();
    }

    void Update()
    {
        if (!isPaused)
        {
            AdvanceTime(Time.deltaTime);
        }
    }

    public void AdvanceTime(float realSecondsElapsed)
    {
        if (isPaused) return;

        float gameMinutesToAdd = realSecondsElapsed / realSecondsPerGameMinute;
        gameTime = gameTime.AddMinutes(gameMinutesToAdd);
        clockText.text = gameTime.ToString("HH:mm");

        // 마감 시간 도달 감지
        if (!hasReachedClosingTime && gameTime.Hour >= closingHour)
        {
            hasReachedClosingTime = true;
            GameManager.Instance.OnClosingTimeReached();
        }
    }

    public static void Pause()
    {
        isPaused = true;
    }

    public static void Resume()
    {
        isPaused = false;
    }
    public static void SetGameTime(DateTime newTime)
    {
        gameTime = newTime;
    }
    public void SetToStartOfDay()
    {
        // 날짜는 유지하고 시간만 12시로 설정
        DateTime dateOnly = gameTime.Date;
        gameTime = dateOnly.AddHours(openingHour);

        hasReachedClosingTime = false;

        UpdateTimeAndDateDisplay();
    }

    public void SetToEightFiftyNine()
    {
        DateTime dateOnly = gameTime.Date;
        gameTime = dateOnly.AddHours(20).AddMinutes(59);  // 오후 8시 59분

        Debug.Log($"[DEBUG] 게임 시간이 오후 8시 59분으로 설정됨: {gameTime:yyyy-MM-dd HH:mm}");

        UpdateTimeAndDateDisplay();
    }
    public void UpdateTimeAndDateDisplay()
    {
        DateTime gameTime = GameClock.gameTime;
        string[] koreanDays = { "일", "월", "화", "수", "목", "금", "토" };
        string formatted = $"{gameTime:yyyy년 M월 d일} ({koreanDays[(int)gameTime.DayOfWeek]})";

        if (dateText != null)
            dateText.text = formatted;
        if (clockText != null)
            clockText.text = gameTime.ToString("HH:mm");
    }
    public static void SaveLastPlayedDate(DateTime date)
    {
        string json = JsonUtility.ToJson(new DateWrapper { dateString = date.ToString("yyyy-MM-dd") });
        File.WriteAllText(Path.Combine(Application.persistentDataPath, LastDateFileName), json);
    }

    public static DateTime? LoadLastPlayedDate()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, LastDateFileName);
        if (!File.Exists(fullPath))
            return null;

        string json = File.ReadAllText(fullPath);
        DateWrapper wrapper = JsonUtility.FromJson<DateWrapper>(json);

        if (DateTime.TryParse(wrapper.dateString, out DateTime result))
        {
            return result;
        }

        return null;
    }

}
