using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWokManager : MonoBehaviour
{
    public static PlayerWokManager Instance { get; private set; }
    public TextMeshProUGUI playerIngredientsText;

    // ✨ [NEW] 텍스트가 포함된 ScrollView 오브젝트 (Inspector 연결)
    public GameObject statusScrollView;

    public ScrollRect statusScrollRect;

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
        // 시작할 때 안 보이게 숨김
        ClearUI();
    }

    public void OnCookButtonPressed()
    {
        if (!StoveManager.Instance.HasSelectedSlot())
        {
            Debug.LogWarning("선택된 화구가 없습니다!");
            TooltipManager.ShowFollowMouse(TooltipType.UI, "화구를 먼저 선택해주세요!", 1f);
            return;
        }

        StoveManager.Instance.TryCookSelectedSlot();
    }

    // ✨ [수정] 제목(statusTitle)을 받을 수 있도록 매개변수 추가 (기본값 설정)
    public void UpdateUI(Dictionary<string, int> ingredients, string statusTitle = "현재 담은 재료")
    {
        if (statusScrollView != null)
        {
            // ✨ [NEW] 내용 표시 전 스크롤 초기화 판단
            if (statusScrollView != null)
            {
                // 만약 지금까지 꺼져있었다면 -> 이제 막 켜지는 것이므로 스크롤 초기화!
                if (!statusScrollView.activeSelf)
                {
                    statusScrollView.SetActive(true);
                    if (statusScrollRect != null)
                    {
                        statusScrollRect.verticalNormalizedPosition = 1f;
                    }
                }
                // 이미 켜져있는 상태에서 텍스트만 바뀌는 거라면 스크롤 유지 (유저가 내리고 있었을 수도 있음)
            }
        }

        if (playerIngredientsText == null) return;

        if (ingredients == null || ingredients.Count == 0)
        {
            // 재료가 없을 때도 제목은 반영
            playerIngredientsText.text = $"{statusTitle}:\n없음";
            return;
        }

        string result = $"{statusTitle}:\n";
        foreach (var item in ingredients)
        {
            result += $"{item.Key} x{item.Value}\n";
        }
        playerIngredientsText.text = result;
    }

    public void ClearUI()
    {
        // 1. 텍스트 비우기
        if (playerIngredientsText != null) playerIngredientsText.text = "";

        // 2. 스크롤뷰 전체 비활성화
        if (statusScrollView != null) statusScrollView.SetActive(false);
    }

    public bool CheckRecipe(Dictionary<string, int> pendingIngredients)
    {
        return ContainsBaseIngredients(pendingIngredients);
    }

    private bool ContainsBaseIngredients(Dictionary<string, int> wok)
    {
        if (wok == null) return false;

        // 1. 필수 4대 재료 검사 (떡, 오뎅, 파, 양배추)
        var essentialIngredients = new Dictionary<string, int>
        {
            { "떡", 2 },
            { "오뎅", 2 },
            { "파", 1 },
            { "양배추", 1 }
        };

        foreach (var pair in essentialIngredients)
        {
            if (!wok.TryGetValue(pair.Key, out int amount) || amount < pair.Value)
            {
                // 디버깅용 로그 (필요시 주석 해제)
                // Debug.Log($"[조리 불가] 필수 재료 부족: {pair.Key} (필요: {pair.Value}, 현재: {amount})");
                return false;
            }
        }

        // 2. 소스 검사 (7종 중 하나라도 1개 이상 있어야 함)
        // IngredientEconomyDatabase에 있는 소스 목록을 기준으로 합니다.
        string[] sauces = new string[]
        {
            "군자 소스", "마라 소스", "로제 소스", "크림 소스",
            "간장 소스", "카레 소스", "짜장 소스"
        };

        bool hasSauce = false;
        foreach (var sauce in sauces)
        {
            if (wok.ContainsKey(sauce) && wok[sauce] > 0)
            {
                hasSauce = true;
                break; // 소스가 하나라도 발견되면 즉시 통과
            }
        }

        if (!hasSauce)
        {
            // Debug.Log("[조리 불가] 소스가 하나도 없습니다.");
            return false;
        }

        return true; // 모든 조건 통과
    }

    // ✨ [NEW] 재료 조합을 보고 완성될 메뉴 이름을 반환하는 함수
    public string IdentifyMenu(Dictionary<string, int> currentIngredients)
    {
        if (!ContainsBaseIngredients(currentIngredients)) return "Invalid";

        // 현재 웍에 든 소스들
        HashSet<string> mySauces = new HashSet<string>();
        foreach (var key in currentIngredients.Keys)
        {
            if (key.Contains("소스")) mySauces.Add(key);
        }

        if (mySauces.Count == 0) return "Invalid";

        // DB와 비교
        foreach (var menuEntry in MenuDatabase.Menus)
        {
            string dbMenuName = menuEntry.Key;
            var dbIngredients = menuEntry.Value.DefaultIngredients;

            HashSet<string> recipeSauces = new HashSet<string>();
            foreach (var key in dbIngredients.Keys)
            {
                if (key.Contains("소스")) recipeSauces.Add(key);
            }

            if (mySauces.SetEquals(recipeSauces))
            {
                Debug.Log($"[메뉴 판별 성공] {dbMenuName}");
                return dbMenuName;
            }
        }

        // 여기까지 왔으면 일치하는 메뉴가 없는 것!
        // 범인을 찾기 위해 로그 출력
        string mySaucesStr = string.Join(", ", mySauces);
        Debug.Log($"[메뉴 판별 실패] 현재 소스 조합 [{mySaucesStr}]에 해당하는 메뉴가 없습니다. -> Ruined 반환");

        return "Ruined"; // ✨ 절대 빈 문자열("")을 반환하지 않도록 주의!
    }
}