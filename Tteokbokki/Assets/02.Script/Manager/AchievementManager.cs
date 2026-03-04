using UnityEngine;
using Steamworks;
using System.Linq;

public enum AchievementID
{
    // 스팀웍스 백엔드에 등록한 ID와 정확히 일치해야 합니다.
    ACH_TEST,            // 테스트용 업적
    beginner,
    today_is_perfact,
    trust_you_eat,
    any_time,
    for_a_wider_variety_of_tteokbokki,
    everything_goes_well_with_tteokbokki,
    all_set,
    bad_ending1,
    the_perpetual_intern,
    official_family_member,
    the_tteokbokki_tycoon,
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // 도전과제 해제 핵심 함수
    public void Unlock(AchievementID achievement)
    {
        string id = achievement.ToString();

        // 1. Identifier 속성을 사용하여 해당 업적을 찾습니다.
        var ach = SteamUserStats.Achievements.FirstOrDefault(x => x.Identifier == id);

        // 2. 구조체이므로 Name이나 Identifier가 비어있는지로 유효성을 확인합니다.
        if (!string.IsNullOrEmpty(ach.Identifier))
        {
            // 3. 이미 달성(State)된 상태가 아닐 때만 실행합니다.
            if (!ach.State)
            {
                ach.Trigger(); // 업적 해제
                Debug.Log($"[Steam] 도전과제 해제 성공: {id}");
            }
        }
        else
        {
            Debug.LogWarning($"[Steam] '{id}' ID를 가진 도전과제를 스팀웍스에서 찾을 수 없습니다. 대소문자를 확인하세요!");
        }
    }

    // 테스트용: 도전과제 초기화 (개발 중에만 사용하세요!)
    public void ClearAchievement(AchievementID achievement)
    {
        var ach = SteamUserStats.Achievements.FirstOrDefault(x => x.Identifier == achievement.ToString());
        if (ach.Identifier != null) ach.Clear();
    }
}