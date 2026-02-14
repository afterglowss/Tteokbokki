using DG.Tweening;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;

public class GameManager : MonoBehaviour
{
    [Header("마감창 UI")]
    public GameObject endOfDayPanel;
    public EndOfDayUIHandler endOfDayUIHandler;

    public static GameManager Instance { get; private set; }

    public DialogueRunner dialogueRunner;

    private bool isEmergencyClosing = false;

    private bool isBadEndingDay = false;

    public int TotalSuccessCount { get; private set; } = 0;
    public int TotalMissedCount { get; private set; } = 0;
    public int ConsecutiveZeroSuccessDays { get; private set; } = 0;

    public int endingCount = 14;

    [Header("Bad Ending 1 UI (Main Scene)")]
    public GameObject panelBadEnding1;       // 검은 배경 패널
    public CanvasGroup cgBadEnding1;         // 투명도 조절용
    public Button btnBackToTitle1;           // 타이틀로 돌아가는 버튼

    // 엔딩 씬 이름 (빌드 세팅에 등록되어 있어야 함)
    [Header("Endings")]
    public string badEnding1Scene = "BadEnding1Scene"; // 조기 폐업
    public string badEnding2Scene = "BadEnding3Scene"; // 30% 이하
    public string normalEndingScene = "NormalEndingScene"; // 31~70% or 재료 부족
    public string happyEndingScene = "HappyEndingScene";   // 71% 이상 + 재료 충분

    public void RestoreSessionData(int success, int missed, int zeroDays)
    {
        TotalSuccessCount = success;
        TotalMissedCount = missed;
        ConsecutiveZeroSuccessDays = zeroDays;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        //if (GameSaveManager.Instance.IsLoading)
        //{
        //    // A. 이어하기인 경우: 아무것도 안 함 (LoadGame이 RestoreStock을 부를 테니까)
        //}
        //else if (TutorialManager.Instance.IsTutorialJustFinished) // 튜토리얼 직후인지 확인하는 플래그 필요
        //{
        //    // B. 튜토리얼 끝내고 온 경우
        //    IngredientStockManager.Instance.ApplyTutorialAftermath();
        //    TutorialManager.Instance.IsTutorialJustFinished = false; // 플래그 초기화
        //}
        //else
        //{
        //    // C. 튜토리얼 스킵하고 바로 새 게임 (그냥 기본 재료만 지급)
        //    IngredientStockManager.Instance.OrderBasicIngredients(); // 기존 함수 public으로 변경 필요
        //}

        // ✨ [핵심 수정] 게임 시작 시 로드 및 시간 체크 로직 추가
        if (GameLoadFlags.shouldLoadFromSave)
        {
            // 1. 데이터 로드
            GameSaveManager.Instance.LoadGame();

            Debug.Log($"[디버그] 로드된 시간: {GameClock.gameTime.Hour}시, 마감시간: {GameClock.closingHour}시");

            // 2. 상태 분기 체크
            if (GameSaveManager.Instance.IsSettlementPhase)
            {
                // [경우 A] 마감 창이 떠있는 상태에서 저장했음 -> 정산 창 복구
                Debug.Log("[시스템] 마감 정산 단계에서 저장된 데이터입니다.");
                ResumeEndOfDay();
            }
            else if (GameClock.gameTime.Hour >= GameClock.closingHour)
            {
                // [경우 B] 21시는 넘었지만 마감 창은 안 떴음 -> 잔여 영수증 처리 중이었음
                Debug.Log("[시스템] 마감 시간은 지났으나 잔여 주문 처리 중입니다.");

                // 주문 생성은 멈추되, 게임은 계속 진행 (영수증 처리해야 함)
                OrderSpawner.Instance.StopSpawning();
                GameClock.Resume(); // 시간은 흐르게 둠 (영수증 타이머 등을 위해) or Pause? 
                                    // 기획에 따라 여기서 GameClock.Pause()를 할 수도 있지만, 
                                    // 보통 잔여 영수증 타이머는 가야 하므로 Resume이 맞습니다.
                                    // 단, 'ClosingTimeReached' 이벤트는 이미 지나갔으므로 감시 코루틴을 수동 시작해야 함.

                StartCoroutine(WaitForReceiptClearAndEnd());
            }
            else
            {
                Debug.Log("[시스템] 정상 영업 시간이므로 영수증을 생성합니다.");
                OrderSpawner.Instance.RestartSpawning();
            }
        }
        else
        {
            // ✨ [핵심 수정] 새 게임 시작 시 주문 번호 초기화!
            ReceiptSystem.CurrentReceiptID = 1;
            ReceiptSystem.CurrentOrderItemID = 1;

            // 새 게임 로직
            if (TutorialManager.Instance != null /*&& TutorialManager.Instance.IsTutorialJustFinished*/)
            {
                IngredientStockManager.Instance.ApplyTutorialAftermath();
                // TutorialManager.Instance.IsTutorialJustFinished = false;
            }
            else
            {
                // 이거 나중에 튜토리얼 연결 후에 풀어둬야 함. 지금은 그대로 두면 나갔다 올때마다 추가되네;
                //IngredientStockManager.Instance.OrderBasicIngredients();
            }
            // 새 게임은 영업 시작
            OrderSpawner.Instance.RestartSpawning();
        }

        // ✨ Yarn 커맨드 등록 (대화 끝날 때 호출됨)
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler("trigger_bad_ending_1", TriggerBadEnding1Sequence);
        }

        // 초기화: 엔딩 패널은 꺼둠
        if (panelBadEnding1 != null) panelBadEnding1.SetActive(false);
    }

    // ✨ [NEW] 마감 상태 이어하기 전용 함수
    // EndOfDay()를 그대로 부르면 매출이 두 번 더해지는 참사가 발생하므로, UI와 상태만 복구합니다.
    private void ResumeEndOfDay()
    {
        // 1. 게임 상태 정지
        GameClock.Pause();
        OrderSpawner.Instance.StopSpawning(); // 주문 생성 차단

        // 2. 전화기 및 기타 요소 정리
        if (PhoneCallManager.Instance != null) PhoneCallManager.Instance.ForceStopAllCalls();
        if (StoveManager.Instance != null) StoveManager.Instance.DeselectCurrentSlot();
        if (ReceiptLineManager.Instance != null && ReceiptLineManager.Instance.combinedIngredientManager != null)
            ReceiptLineManager.Instance.combinedIngredientManager.ClearIngredientsText();

        // 3. 화면 정리 (포장된 음식 등)
        PackagingAreaManager.Instance.ClearAllFoods();

        // 4. 마감 UI 띄우기 (데이터는 LoadGame에서 복구된 ReceiptLineManager 데이터를 기반으로 UI가 알아서 계산함)
        ShowEndOfDayPanel();

        // 5. 상점 재고 경고 업데이트
        IngredientStockManager.Instance.UpdateLowStockList();

        // 6. 보너스 및 Yarn 변수 재설정 (휘발성 데이터 복구)
        // DailyBonusManager.Instance.SetTomorrowBonusIngredients();

        var bonusList = DailyBonusManager.Instance.GetTomorrowBonusIngredients().ToList();
        bool hasBonus = bonusList.Count > 0;
        if (dialogueRunner != null)
        {
            dialogueRunner.VariableStorage.SetValue("$hasBonus", hasBonus);
            dialogueRunner.VariableStorage.SetValue("$bonus1", bonusList.Count > 0 ? bonusList[0] : "");
            dialogueRunner.VariableStorage.SetValue("$bonus2", bonusList.Count > 1 ? bonusList[1] : "");
        }

        Debug.Log("[이어하기] 마감 정산 화면으로 복귀했습니다.");
    }

    // ✨ [NEW] 배드엔딩 1 연출 함수 (Yarn에서 호출)
    public void TriggerBadEnding1Sequence()
    {
        Debug.Log("💀 [BadEnding1] 연출 시작");

        // 1. 게임 시간 정지 (키보드 입력 차단)
        GameClock.Pause();

        // 2. 패널 활성화 및 초기화
        if (panelBadEnding1 != null && cgBadEnding1 != null)
        {
            panelBadEnding1.SetActive(true);
            cgBadEnding1.alpha = 0f;
            cgBadEnding1.blocksRaycasts = true; // 클릭 방지

            if (btnBackToTitle1 != null)
            {
                btnBackToTitle1.gameObject.SetActive(false); // 버튼은 아직 숨김
                btnBackToTitle1.onClick.RemoveAllListeners();
                btnBackToTitle1.onClick.AddListener(GoToStartScene);
            }

            // 3. 페이드 인 연출 (2초 동안)
            // Time.timeScale = 0이므로 .SetUpdate(true) 필수!
            cgBadEnding1.DOFade(1f, 2.0f).SetUpdate(true).OnComplete(() =>
            {
                // 4. 페이드인 끝난 후 3초 대기 -> 버튼 등장
                DOVirtual.DelayedCall(3.0f, () =>
                {
                    if (btnBackToTitle1 != null)
                    {
                        btnBackToTitle1.gameObject.SetActive(true);
                        // 버튼도 살짝 페이드인 하면 예쁨
                        CanvasGroup btnCg = btnBackToTitle1.GetComponent<CanvasGroup>();
                        if (btnCg == null) btnCg = btnBackToTitle1.gameObject.AddComponent<CanvasGroup>();
                        btnCg.alpha = 0f;
                        btnCg.DOFade(1f, 1f).SetUpdate(true);
                    }
                }).SetUpdate(true);
            });
        }
    }

    // ✨ 타이틀로 돌아가는 함수
    public void GoToStartScene()
    {
        Time.timeScale = 1f; // 중요: 시간 다시 정상화
        DOTween.KillAll();   // 실행 중인 트윈 정리
        SceneManager.LoadScene("StartScene"); // 시작 화면 씬 이름 확인 필요
    }

    public void StartOfDay()
    {
        //if (CheckPrematureBankruptcy()) return;

        isEmergencyClosing = false; // ✨ 플래그 초기화

        GameClock.gameTime = GameClock.gameTime.AddDays(1);

        if (GetDayCount() > endingCount)
        {
            CheckFinalEnding();
            return; // 더 이상 영업 준비를 하지 않음
        }

        if (ConsecutiveZeroSuccessDays >= 2)
        {
            isBadEndingDay = true;
            Debug.Log("💀 [BadEnding1] 조건 달성! 내일은 손님이 오지 않습니다...");
        }
        else
        {
            isBadEndingDay = false;
        }

        GameClock.Instance.SetToStartOfDay();
        GameClock.Instance.UpdateTimeAndDateDisplay();

        IngredientStockManager.Instance.ResetDailyOrderFlags();
        IngredientStockManager.Instance.AdvanceDayAndDecay();

        // ✨ [NEW] 어제 구매 내역을 반영하여 주방 버튼(재료통) 새로고침!
        // (일반 재료와 소스도 이때 분리되어 생성됨)
        IngredientStockManager.Instance.GenerateIngredientButtons();

        // 조리 상태 초기화
        StoveManager.Instance.ClearAllStoves();

        PackagingAreaManager.Instance.ClearAllFoods(); // 포장대 초기화

        ReceiptLineManager.Instance.ClearAllReceipts(); // 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearMissedReceipts(); // 실패 영수증 리스트 초기화
        ReceiptLineManager.Instance.ClearSuccessfulReceipts(); // 성공 영수증 리스트 초기화

        ReceiptSystem.CurrentReceiptID = 1; // 주문 번호 초기화
        ReceiptSystem.CurrentOrderItemID = 1; // 메뉴 번호 초기화

        DailyBonusManager.Instance.ApplyNewDayBonus();

        //GameClock.Resume();

        //OrderSpawner.Instance.RestartSpawning();

        PlayerWalletManager.Instance.ResetTodayEarnings();  // 하루 수익 초기화

        //HideEndOfDayPanel();

        Debug.Log("[준비] 다음 영업일 데이터 설정 완료 (시간은 아직 정지 상태)");
    }

    public void StartDayGameplay()
    {
        GameClock.Resume(); // 시간 흐르기 시작
        if (isBadEndingDay)
        {
            // 💀 [배드엔딩 1 루트]
            // 1. 주문 생성기 침묵시키기 (확률 0)
            OrderSpawner.Instance.SetSilenceMode();

            // 2. 뱁새 독백 시작 ("아무도 주문을 하지 않아...")
            // Yarn Node 이름은 "BadEnding1_Monologue" 등으로 가정
            if (dialogueRunner != null)
            {
                dialogueRunner.StartDialogue("BadEnding1_Monologue");
            }

            // 3. 대화가 끝난 후의 처리는 Yarn Command나
            // DialogueRunner의 OnDialogueComplete 이벤트에서 '타이틀로 이동' 등을 연결해야 함
        }
        else
        {
            // ✅ [정상 영업 루트]
            OrderSpawner.Instance.RestartSpawning(); // 주문 시작
            Debug.Log("[시작] 셔터가 열리고 영업이 시작되었습니다!");
        }
    }

    // 현재 며칠째인지 계산 (시작일로부터 경과일 + 1)
    private int GetDayCount()
    {
        TimeSpan span = GameClock.gameTime.Date - new DateTime(2025, 7, 4).Date; // GameClock.startYear 등 사용 추천
        return (int)span.TotalDays + 1;
    }

    public void EndOfDay()
    {
        GameClock.Pause();
        OrderSpawner.Instance.StopSpawning();

        // ✨ [NEW] 전화기 코드 뽑기 (전화 강제 종료)
        if (PhoneCallManager.Instance != null)
        {
            PhoneCallManager.Instance.ForceStopAllCalls();
        }

        // ✨ [핵심 추가] 정산 계산하기 전에, 현재 남아있는 영수증들을 전부 '실패'로 확정 짓습니다.
        if (ReceiptLineManager.Instance != null)
        {
            ReceiptLineManager.Instance.FailAllActiveReceipts();
        }

        // 1. 오늘 성적 가져오기
        var todaySuccess = ReceiptLineManager.Instance.GetSuccessfulReceipts();
        var todayMissed = ReceiptLineManager.Instance.GetMissedReceipts();

        int successCount = todaySuccess.Count;
        int missedCount = todayMissed.Count;

        // 2. 누적 데이터 업데이트
        TotalSuccessCount += successCount;
        TotalMissedCount += missedCount;

        // 3. 배드엔딩 1 (조기 폐업) 체크 조건 업데이트
        if (successCount == 0)
        {
            ConsecutiveZeroSuccessDays++;
            Debug.Log($"[주의] 오늘 성공한 주문 0건. 연속 {ConsecutiveZeroSuccessDays}일째.");
        }
        else
        {
            ConsecutiveZeroSuccessDays = 0; // 성공한 게 있으면 초기화
        }

        PackagingAreaManager.Instance.ClearAllFoods();

        var missed = ReceiptLineManager.Instance.GetMissedReceipts();
        var successful = ReceiptLineManager.Instance.GetSuccessfulReceipts();
        DateTime today = GameClock.gameTime.Date;

        ReceiptManager.SaveMissedReceipts(missed, today);
        ReceiptManager.SaveSuccessfulReceipts(successful, today);

        // 판매 총액 계산
        int successTotal = successful.Sum(r => r.GetTotalPrice());

        // ❌ [삭제됨] 자동 세금 차감 로직 제거 (이제 UI에서 버튼 눌러서 납부함)
        // PlayerWalletManager.Instance.DeductDailyTaxes(successTotal); 

        // 손실 총액 계산
        int missedTotal = missed.Sum(r => r.GetOrders().Sum(o => o.TotalPrice));
        // 성공률 계산
        float successRate = successful.Count / (float)(successful.Count + missed.Count + 0.01f);
        OrderSpawner.Instance.SetPreviousDaySuccessRate(successRate); // 다음날 확률 반영용

        // 로그 출력
        Debug.Log($"[마감] 성공 주문 {successful.Count}건 / 총 판매금액: {successTotal:N0}원");
        Debug.Log($"[마감] 미완료 주문 {missed.Count}건 / 손실 금액: {missedTotal:N0}원");
        // 세금 로그도 여기서 띄우기 애매하므로 제거하거나 예상액으로 변경
        // Debug.Log($"[마감] 세금 {Mathf.RoundToInt(successTotal * PlayerWalletManager.Instance.taxRate):N0}원 납부");

        // ✨ [UI 정리] 마감 창이 뜨기 전에 화구 정보와 영수증 정보창을 강제로 끕니다.
        // 1. 화구 정보창 끄기 & 화구 선택 해제 (PlayerWokManager 스크롤뷰 꺼짐)
        if (StoveManager.Instance != null)
        {
            StoveManager.Instance.DeselectCurrentSlot();
        }

        // 2. 영수증 재료 정보창 끄기 (CombinedIngredientManager 스크롤뷰 꺼짐)
        if (ReceiptLineManager.Instance != null && ReceiptLineManager.Instance.combinedIngredientManager != null)
        {
            ReceiptLineManager.Instance.combinedIngredientManager.ClearIngredientsText();
        }

        // 마감 UI 출력
        ShowEndOfDayPanel();

        IngredientStockManager.Instance.ResetDailyOrderFlags();

        IngredientStockManager.Instance.UpdateLowStockList();
        Debug.Log(IngredientStockManager.Instance.GetLowStockText());

        DailyBonusManager.Instance.SetTomorrowBonusIngredients();

        // Yarn 변수에 저장
        var bonusList = DailyBonusManager.Instance.GetTomorrowBonusIngredients().ToList();
        // 3. ✨ [핵심 수정] Yarn 변수 설정
        // $hasBonus: 보너스가 있는지 여부 (true / false)
        bool hasBonus = bonusList.Count > 0;
        dialogueRunner.VariableStorage.SetValue("$hasBonus", hasBonus);

        // $bonus1, $bonus2: 재료 이름 (없으면 빈 문자열)
        dialogueRunner.VariableStorage.SetValue("$bonus1", bonusList.Count > 0 ? bonusList[0] : "");
        dialogueRunner.VariableStorage.SetValue("$bonus2", bonusList.Count > 1 ? bonusList[1] : "");

        // (디버깅용 로그)
        if (hasBonus)
            Debug.Log($"[Yarn 설정] 보너스 있음: {bonusList[0]}, {(bonusList.Count > 1 ? bonusList[1] : "")}");
        else
            Debug.Log("[Yarn 설정] 내일은 보너스 없음 (쉬는 날)");

        GameClock.SaveLastPlayedDate(today);

        GameSaveManager.Instance.SaveGame();
    }

    public void OnClosingTimeReached()
    {
        OrderSpawner.Instance.StopSpawning(); // 주문 생성 중단

        // 남은 영수증이 없다면 바로 마감
        if (ReceiptLineManager.Instance.GetReceiptSlots().Count == 0)
        {
            EndOfDay();
        }
        else
        {
            // 감시 루틴 시작
            StartCoroutine(WaitForReceiptClearAndEnd());
        }
    }

    private void ShowEndOfDayPanel()
    {
        // Panel_EndOfDay 활성화
        // ✨ [수정] 핸들러가 OnEnable에서 스스로 초기화하므로, 여기서는 켜기만 하면 됩니다.
        endOfDayPanel.SetActive(true);
    }

    private void HideEndOfDayPanel()
    {
        endOfDayPanel.SetActive(false);
    }

    private IEnumerator WaitForReceiptClearAndEnd()
    {
        Debug.Log("[마감 대기] 오후 9시 이후, 영수증 처리 완료 대기 중...");

        // 매 0.5초마다 확인
        while (ReceiptLineManager.Instance.GetReceiptSlots().Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[마감] 모든 영수증 처리 완료. EndOfDay 호출!");

        EndOfDay();
    }

    // ✨ [NEW] 재료 소진 시 강제 조기 마감
    public void TriggerEmergencyClose(string reason)
    {
        // 1. 이미 마감 절차가 진행 중이라면 무시
        if (isEmergencyClosing || GameClock.isPaused || endOfDayPanel.activeSelf) return;

        isEmergencyClosing = true; // ✨ 지금부터 마감 절차 시작함을 표시

        Debug.Log($"[긴급 마감] {reason} - 더 이상 조리가 불가능하여 영업을 종료합니다.");

        // 2. 플레이어에게 이유를 알려줌 (툴팁 등)
        TooltipManager.ShowFollowMouse(TooltipType.UI, $"{reason}\n잠시 후 영업을 조기 종료합니다...", 5f);

        // 3. 주문 생성 즉시 중단
        OrderSpawner.Instance.StopSpawning();

        StartCoroutine(DelayedEmergencyCloseRoutine());
    }
    private IEnumerator DelayedEmergencyCloseRoutine()
    {
        yield return new WaitForSeconds(5.0f); // 5초 대기 (플레이어가 툴팁 읽을 시간)

        EndOfDay(); // 마감 정산 시작
    }

    private void CheckFinalEnding()
    {
        Debug.Log("=== 대망의 엔딩 분기 시작 ===");

        // 0. 총 주문 수
        int totalOrders = TotalSuccessCount + TotalMissedCount;
        float successRate = 0f;

        if (totalOrders > 0)
            successRate = (float)TotalSuccessCount / totalOrders * 100f;

        // 1. 재료 해금 개수 확인 (기본 재료 포함)
        int unlockedIngredientsCount = IngredientStockManager.Instance.GetPurchasedIngredientCount();

        Debug.Log($"최종 성적 - 성공률: {successRate:F1}% ({TotalSuccessCount}/{totalOrders}), 해금 재료: {unlockedIngredientsCount}개");

        // 2. 엔딩 분기
        if (successRate <= 30f)
        {
            // 배드 엔딩 2
            LoadEndingScene(badEnding2Scene);
        }
        else if (successRate <= 70f)
        {
            // 노멀 엔딩 (성공률이 애매함)
            LoadEndingScene(normalEndingScene);
        }
        else // 71% 이상
        {
            if (unlockedIngredientsCount >= 15)
            {
                // 해피 엔딩 (성공률도 높고, 재료도 많이 모음)
                LoadEndingScene(happyEndingScene);
            }
            else
            {
                // 노멀 엔딩 (성공률은 높지만, 재료가 부족함 = 쫄보 플레이)
                Debug.Log("성공률은 높으나 재료 해금 부족으로 노멀 엔딩 진입");
                LoadEndingScene(normalEndingScene);
            }
        }
    }

    // ✨ [NEW] 배드엔딩 1 (조기 폐업) 전용 체크
    // StartOfDay의 맨 앞부분에 추가하면 됩니다.
    private bool CheckPrematureBankruptcy()
    {
        if (ConsecutiveZeroSuccessDays >= 2)
        {
            // 뱁새 대사 출력 후 엔딩 이동 로직 필요
            // 여기서는 씬 이동만 구현
            Debug.Log("2일 연속 매출 0... 가게를 접습니다.");
            LoadEndingScene(badEnding1Scene);
            return true;
        }
        return false;
    }

    private void LoadEndingScene(string sceneName)
    {
        // UI 정리 및 씬 이동
        GameClock.Pause();
        SceneManager.LoadScene(sceneName);
    }
}