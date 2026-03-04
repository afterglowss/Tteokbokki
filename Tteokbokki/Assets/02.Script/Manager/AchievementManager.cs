using UnityEngine;
#if !STOVE_BUILD
using Steamworks;
#endif
using System.Linq;

public enum AchievementID
{
    // 스팀웍스 백엔드에 등록한 ID와 정확히 일치해야 합니다.
    //ACH_TEST,            // 테스트용 업적
    beginner,
    today_is_perfect,
    trust_you_eat,
    any_time,
    for_a_wider_variety_of_tteokbokki,
    everything_goes_well_with_tteokbokki,
    all_set,
    youre_fired,
    the_perpetual_intern,
    official_family_member,
    the_tteokbokki_tycoon,
    dark_matter_chef,
    gordon_ramsays_nightmare,
    young_and_rich_bird,
    living_on_the_edge,
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
#if !STOVE_BUILD
        string id = achievement.ToString();
        var ach = SteamUserStats.Achievements.FirstOrDefault(x => x.Identifier == id);

        if (!string.IsNullOrEmpty(ach.Identifier))
        {
            if (!ach.State)
            {
                ach.Trigger();
                Debug.Log($"[Steam] 도전과제 해제 성공: {id}");
            }
        }
        else
        {
            Debug.LogWarning($"[Steam] '{id}' ID를 가진 도전과제를 찾을 수 없습니다.");
        }
#else
        // 스토브 빌드일 때는 아무것도 하지 않고 스무스하게 넘어갑니다.
        Debug.Log($"[Stove] 도전과제 달성 조건 충족됨 (스토브 빌드이므로 무시): {achievement}");
#endif
    }

    // 테스트용: 도전과제 초기화 (개발 중에만 사용하세요!)
    public void ClearAchievement(AchievementID achievement)
    {
#if !STOVE_BUILD
        var ach = SteamUserStats.Achievements.FirstOrDefault(x => x.Identifier == achievement.ToString());
        if (ach.Identifier != null) ach.Clear();
#endif
    }
}