using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class DailyBonusManager : MonoBehaviour
{
    public static DailyBonusManager Instance { get; private set; }

    private HashSet<string> todayBonusIngredients = new();    // 오늘 적용 중인 보너스 재료
    private HashSet<string> tomorrowBonusCandidates = new();  // 마감 시 보여줄 내일 보너스 재료

    private const int bonusPerIngredient = 5000; // 1개 포함당 보너스 금액

    public TextMeshProUGUI bonusText;

    // ✨ [NEW] 오늘 하루 누적된 보너스 금액
    public int TodayAccumulatedBonus { get; private set; } = 0;

    [SerializeField]
    private static readonly HashSet<string> excludedBonusIngredients = new()
{
    "떡",
    "오뎅",
    "파",
    "양배추",
    "군자 소스",
};

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        GetTomorrowBonusText();
    }

    public void SetTomorrowBonusIngredients()
    {
        tomorrowBonusCandidates = PickRandomIngredients(2);  // 랜덤 2종 선택
        Debug.Log($"[보너스] 내일 보너스 재료 예정: {string.Join(", ", tomorrowBonusCandidates)}");
    }

    public void ApplyNewDayBonus()
    {
        todayBonusIngredients = new HashSet<string>(tomorrowBonusCandidates);

        // ✨ [NEW] 하루 시작할 때 누적 금액 초기화
        TodayAccumulatedBonus = 0;

        Debug.Log($"[보너스] 오늘 보너스 재료 적용됨: {string.Join(", ", todayBonusIngredients)}");
    }

    // ✨ [NEW] 보너스 금액 추가 (PackagingSlot에서 호출)
    public void AddBonusIncome(int amount)
    {
        TodayAccumulatedBonus += amount;
    }

    public int CalculateBonusFromIngredients(Dictionary<string, int> ingredients)
    {
        if (ingredients == null || todayBonusIngredients.Count == 0)
            return 0;

        int totalBonus = 0;
        foreach (var kv in ingredients)
        {
            if (todayBonusIngredients.Contains(kv.Key))
            {
                totalBonus += bonusPerIngredient * kv.Value;
            }
        }

        return totalBonus;
    }

    public string GetTomorrowBonusText()
    {
        if (tomorrowBonusCandidates.Count == 0)
            return "예정된 보너스 재료 없음";

        bonusText.text = "내일의 보너스 재료: " + string.Join(", ", tomorrowBonusCandidates);

        return "내일의 보너스 재료: " + string.Join(", ", tomorrowBonusCandidates);
    }

    private HashSet<string> PickRandomIngredients(int count)
    {
        var eligible = IngredientEconomyDatabase.Data.Keys
            .Where(name => !excludedBonusIngredients.Contains(name)) // 제외 대상 제거
            .ToList();

        var result = new HashSet<string>();
        while (result.Count < count && eligible.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, eligible.Count);
            result.Add(eligible[idx]);
            eligible.RemoveAt(idx);
        }

        return result;
    }
    public HashSet<string> GetTomorrowBonusIngredients()
    {
        return new HashSet<string>(tomorrowBonusCandidates);
    }

    // ✨ [추가] 외부(상점 UI)에서 오늘 보너스 재료가 뭔지 물어볼 때 대답해주는 함수
    public string GetTodayBonusString()
    {
        if (tomorrowBonusCandidates.Count == 0)
            return "없음";

        // HashSet에 있는 재료 이름들을 ", "로 연결해서 문자열로 반환
        return string.Join(", ", tomorrowBonusCandidates);
    }

    public bool IsBonusIngredient(string name)
    {
        return todayBonusIngredients.Contains(name);
    }

    // ✨ [NEW] 저장용: 리스트로 변환해서 반환
    public List<string> GetTomorrowBonusForSave()
    {
        return new List<string>(tomorrowBonusCandidates);
    }

    // ✨ [NEW] 로드용: 저장된 리스트 복원
    public void RestoreBonusData(List<string> savedBonuses)
    {
        tomorrowBonusCandidates.Clear();
        if (savedBonuses != null)
        {
            foreach (var bonus in savedBonuses)
            {
                tomorrowBonusCandidates.Add(bonus);
            }
        }

        // 로드 직후 UI 갱신 (선택 사항)
        GetTomorrowBonusText();
    }
}
