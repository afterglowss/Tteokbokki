using UnityEngine;
using UnityEngine.Localization.Settings; // ✨ 유니티 다국어 시스템 연결

public static class TextTranslator
{
    // 우리가 유니티에서 만든 단어장(String Table)의 진짜 이름
    // (만약 아까 만든 표 이름이 "UIText"가 아니라면 그 이름으로 바꿔주세요)
    private const string TableName = "UIText";

    // 🍲 재료 이름 번역기
    public static string GetIngredientName(string ingredientId)
    {
        // 규칙: 단어장의 Key를 "Ing_떡" 형태로 묶어서 관리합니다.
        string entryKey = $"Ing_{ingredientId}";

        // 단어장에서 해당하는 번역을 쏙 빼옵니다.
        string translated = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, entryKey);

        // 만약 단어장에 번역을 안 적어뒀다면? 에러 대신 일단 원래 이름("떡")을 그대로 내보냅니다. (안전장치)
        if (string.IsNullOrEmpty(translated))
        {
            Debug.LogWarning($"[번역 누락] '{entryKey}' 번역 데이터가 없습니다!");
            return ingredientId;
        }

        return translated;
    }

    // 🍽️ 메뉴 이름 번역기
    public static string GetMenuName(string menuId)
    {
        // 규칙: 단어장의 Key를 "Menu_군자 떡볶이" 형태로 관리합니다.
        string entryKey = $"Menu_{menuId}";

        string translated = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, entryKey);

        if (string.IsNullOrEmpty(translated))
        {
            Debug.LogWarning($"[번역 누락] '{entryKey}' 번역 데이터가 없습니다!");
            return menuId;
        }

        return translated;
    }

    // ✨ [NEW] 범용 텍스트 번역기 (변수들 {0}, {1} 구멍 채워주는 기능 포함!)
    public static string GetUIText(string key, params object[] args)
    {
        // args가 없으면 그냥 글자만 가져오고, 있으면 {0} 자리에 값들을 쏙쏙 넣어줍니다.
        string translated = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key, args);

        if (string.IsNullOrEmpty(translated))
        {
            return key; // 번역이 누락되면 차라리 Key를 보여줌
        }
        return translated;
    }
}