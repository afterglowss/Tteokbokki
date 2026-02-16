using System;
using UnityEngine;
using TMPro;
using System.Data;
using System.IO;
using System.Globalization;

[Serializable]
public class DateWrapper
{
    public string dateString;
}

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [Header("UI 연결")]
    public TextMeshProUGUI dateTimeText; // ✨ 하나로 합친 텍스트 변수

    // 기존 변수들은 혹시 몰라 남겨두거나 주석 처리 (삭제해도 무방)
    // public TextMeshProUGUI clockText; 
    // public TextMeshProUGUI dateText;

    public float realSecondsPerGameMinute = 2f; // 현실 3초 = 게임 1분

    [Header("영업 시간")]
    public static int openingHour = 17;
    public static int closingHour = 21;

    public static bool isPaused = false;
    private bool hasReachedClosingTime = false;

    [Header("초기 날짜 및 시간 설정")]
    public int startYear = 2025;
    public int startMonth = 8; // 이미지에 맞춰 8월로 예시 변경
    public int startDay = 25;  // 이미지에 맞춰 25일로 예시 변경
    public int startHour = 12;
    public int startMinute = 0;

    public static DateTime gameTime;
    public DateTime GetCurrentGameTime() => gameTime;

    private const string LastDateFileName = "LastPlayedDate.json";

    private void Awake()
    {
        isPaused = false;

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

        // ✨ 매 프레임 시간 갱신 시에도 통합된 포맷 사용
        UpdateDateTimeText();

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

        if (Instance != null)
        {
            // 현재 시간이 마감 시간보다 크거나 같으면 true, 아니면 false
            Instance.hasReachedClosingTime = (newTime.Hour >= closingHour);

            // 텍스트도 즉시 갱신
            Instance.UpdateTimeAndDateDisplay();
        }
    }

    public void SetToStartOfDay()
    {
        DateTime dateOnly = gameTime.Date;
        gameTime = dateOnly.AddHours(openingHour);
        hasReachedClosingTime = false;
        UpdateTimeAndDateDisplay();
    }

    public void SetToEightFiftyNine()
    {
        DateTime dateOnly = gameTime.Date;
        gameTime = dateOnly.AddHours(20).AddMinutes(59);
        Debug.Log($"[DEBUG] 게임 시간이 오후 8시 59분으로 설정됨: {gameTime:yyyy-MM-dd HH:mm}");
        UpdateTimeAndDateDisplay();
    }

    // 외부에서 호출하거나 날짜가 크게 바뀌었을 때
    public void UpdateTimeAndDateDisplay()
    {
        UpdateDateTimeText();
    }

    // ✨ 텍스트 갱신 로직을 하나로 통합
    private void UpdateDateTimeText()
    {
        if (dateTimeText != null)
        {
            // "yyyy - MM - dd dddd HH:mm" 형식을 사용합니다.
            // new CultureInfo("ko-KR")를 넣으면 컴퓨터 언어 설정과 상관없이 무조건 한국어로 나옵니다.
            dateTimeText.text = gameTime.ToString("yyyy - MM - dd dddd HH:mm", new CultureInfo("ko-KR"));
        }
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