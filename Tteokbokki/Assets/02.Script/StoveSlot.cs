using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class StoveSlot : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI timerText;
    public GameObject wokIcon;
    public GameObject selectedHighlight;    // 선택된 상태를 표시할 오브젝트

    private float cookTimeSeconds;
    private float cookTimeRemaining;
    private bool isCooking = false;
    private bool isCooked = false;  // 조리 완료 상태

    private Dictionary<string, int> currentIngredients;
    private Action<Dictionary<string, int>> onCookComplete;

    public bool IsAvailable => !isCooking && !isCooked;

    private StoveManager stoveManager;

    public void Initialize(StoveManager manager)
    {
        stoveManager = manager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        stoveManager?.SelectSlot(this);
        //Debug.Log($"{this.name}이 선택되었습니다.");
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(selected);
        }
    }

    public void StartCooking(Dictionary<string, int> ingredients, float cookTime, Action<Dictionary<string, int>> onComplete)
    {
        currentIngredients = new Dictionary<string, int>(ingredients);
        cookTimeSeconds = cookTime;
        cookTimeRemaining = cookTime;
        this.onCookComplete = onComplete;

        isCooking = true;
        isCooked = false;

        wokIcon.SetActive(true);
        UpdateTimerDisplay();
    }
    void Update()
    {
        if (!isCooking) return;

        cookTimeRemaining -= Time.deltaTime * (60f / 3f); // 게임 속도
        if (cookTimeRemaining <= 0)
        {
            FinishCooking();
        }
        else
        {
            UpdateTimerDisplay();
        }
    }

    public PackagingAreaManager packagingAreaManager;

    public Transform cookedFoodSpawnPoint; // Inspector에서 빈 오브젝트로 위치 설정
    public GameObject cookedFoodPrefab;    // 음식 UI 프리팹 (CookedFoodUI)

    public GameObject globalTooltipPanel;        // UI 전역 툴팁 패널
    public TextMeshProUGUI globalTooltipText;    // 텍스트
    private void FinishCooking()
    {
        isCooking = false;
        isCooked = true;
        timerText.text = "완료!";

        GameObject obj = Instantiate(cookedFoodPrefab, cookedFoodSpawnPoint);
        obj.transform.localPosition = Vector3.zero;

        var foodUI = obj.GetComponent<CookedFoodUI>();
        foodUI.Initialize(currentIngredients);

        // 툴팁 연결
        foodUI.SetTooltipReferences(globalTooltipPanel, globalTooltipText);
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(cookTimeRemaining / 60);
        int seconds = Mathf.FloorToInt(cookTimeRemaining % 60);
        timerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public Dictionary<string, int> GetCookedIngredients() => isCooked ? currentIngredients : null;
}
