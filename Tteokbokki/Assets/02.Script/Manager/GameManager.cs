using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Drawing;
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

    // ✨ [NEW] 긴급 공지 배너 UI
    [Header("Announcement Banner")]
    public RectTransform announcementBanner; // 화면 상단에 배치될 패널
    public TextMeshProUGUI announcementText; // 패널 안의 텍스트

    [Header("D-Day Animation UI")]
    public RectTransform ddayContainer; // 화면 중앙으로 이동할 전체 부모 (빈 오브젝트 또는 배경 패널)
    public TextMeshProUGUI ddayText;    // 숫자가 틱! 하고 바뀔 텍스트

    public static GameManager Instance { get; private set; }

    public DialogueRunner dialogueRunner;

    private bool isEmergencyClosing = false;

    private bool isBadEndingDay = false;

    public int TotalSuccessCount { get; private set; } = 0;
    public int TotalMissedCount { get; private set; } = 0;
    public int ConsecutiveZeroSuccessDays { get; private set; } = 0;
    public int ConsecutivePerfectDays { get; private set; } = 0; // 연속 퍼펙트 변수 생성

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

    public void RestoreSessionData(int success, int missed, int zeroDays, int perfectDays)
    {
        TotalSuccessCount = success;
        TotalMissedCount = missed;
        ConsecutiveZeroSuccessDays = zeroDays;
        ConsecutivePerfectDays = perfectDays;
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

    public void ApplyYarnLanguage()
    {
        if (dialogueRunner == null) return;

        // ✨ 점장님이 찾아내신 진짜 인덱스 적용! (0: 영어, 1: 한국어)
        // (기본값을 한국어인 1로 설정해 둡니다)
        int savedLangIndex = PlayerPrefs.GetInt("GameLanguage", 1);

        // ✨ 0번일 때 "en", 1번일 때 "ko"가 되도록 수정!
        string yarnLangCode = (savedLangIndex == 0) ? "en" : "ko";

        var builtinProvider = dialogueRunner.GetComponent<Yarn.Unity.BuiltinLocalisedLineProvider>();

        if (builtinProvider != null)
        {
            builtinProvider.LocaleCode = yarnLangCode;
            builtinProvider.AssetLocaleCode = yarnLangCode;
            Debug.Log($"[시스템] 대화 언어가 '{yarnLangCode}'로 완벽하게 설정되었습니다!");
        }
        else
        {
            Debug.LogWarning("🚨 [시스템] BuiltinLocalisedLineProvider가 없습니다!");
        }
    }

    void Start()
    {
        // ✨ Yarn 커맨드 등록 (대화 끝날 때 호출됨)
        // ✨ Yarn 커맨드 등록
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler("trigger_bad_ending_1", TriggerBadEnding1Sequence);
            // ✨ [핵심 수정] Yarn이 작동하기 전, 가장 먼저 언어를 세팅합니다!
            //ApplyYarnLanguage();
        }


        // ✨ [핵심 수정] 게임 시작 시 로드 및 시간 체크 로직 추가
        if (GameLoadFlags.shouldLoadFromSave)
        {
            // 1. 데이터 로드
            GameSaveManager.Instance.LoadGame();

            if (endOfDayUIHandler != null) endOfDayUIHandler.ForceOpenShutterImmediately();

            Debug.Log($"[디버그] 로드된 시간: {GameClock.gameTime.Hour}시, 마감시간: {GameClock.closingHour}시");

            // ✨ [핵심 수정] 로드했는데 이미 엔딩 날짜(14일)를 넘겼다면? -> 바로 엔딩으로 납치!
            if (GetDayCount() > endingCount)
            {
                Debug.Log("[시스템] 엔딩 이후의 세이브 파일입니다. 엔딩 분기를 재실행합니다.");
                CheckFinalEnding(); // 다시 점수 계산해서 해당 엔딩 씬으로 이동
                return;
            }

            // ✨ [NEW] 2. 배드엔딩 1(조기 폐업) 조건인지 체크
            // (로드했는데 이미 '망한 상태'라면, 정상 영업 대신 배드엔딩 시퀀스를 다시 가동해야 함)
            if (ConsecutiveZeroSuccessDays >= 2)
            {
                Debug.Log("[시스템] 조기 폐업 조건이 충족된 세이브입니다. 배드엔딩 1을 진행합니다.");

                // ✨ [NEW] 여기서도 BGM 끄고 시계 소리 재생
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM();
                    AudioManager.Instance.PlayLoopSFX(501);
                }

                // 주문 생성 차단
                OrderSpawner.Instance.SetSilenceMode();

                // 바로 대화 시작 (혹은 바로 UI 띄우기)
                if (dialogueRunner != null)
                {
                    // 대화가 끝나면 TriggerBadEnding1Sequence가 호출되면서 세이브가 삭제됨
                    dialogueRunner.StartDialogue("BadEnding1_Monologue");
                }
                return; // 아래 정상 영업 로직 실행 안 함
            }

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
            //ReceiptSystem.CurrentOrderItemID = 1;

            TotalSuccessCount = 0;
            TotalMissedCount = 0;
            ConsecutiveZeroSuccessDays = 0;
            ConsecutivePerfectDays = 0;
            isBadEndingDay = false;

            if (GameLoadFlags.isTutorialJustFinished)
            {
                // [Case 1] 튜토리얼 직접 깨고 옴
                Debug.Log("[GameManager] 튜토리얼 완료 진입: 하드코딩 데이터 적용 & 셔터 ON");

                // 1. 튜토리얼 결과 적용
                IngredientStockManager.Instance.ApplyTutorialAftermath();

                PlayerWalletManager.Instance.SetBalance(225200);
                Debug.Log("[GameManager] 튜토리얼 정산 완료: 잔고 225,200원으로 설정");

                // 2. 셔터 애니메이션 재생 (게임 시작 느낌)
                if (endOfDayUIHandler != null)
                {
                    endOfDayUIHandler.PlayOpenShutterAnimation();
                }
            }
            else
            {
                // [Case 2] 튜토리얼 스킵 (완전 초기 상태)
                Debug.Log("[GameManager] 튜토리얼 스킵 진입: 기본 데이터 & 셔터 OFF");

                // 1. 기본 재료만 지급 (마라소스 X)
                IngredientStockManager.Instance.OrderBasicIngredients();

                // 2. 셔터 애니메이션 없음 (바로 가게 내부)
                if (endOfDayUIHandler != null)
                {
                    endOfDayUIHandler.ForceOpenShutterImmediately();
                }
            }

            if (GameDataLogger.Instance != null)
            {
                GameDataLogger.Instance.StartNewDayLog(PlayerWalletManager.Instance.CurrentBalance);
            }
            // 새 게임은 영업 시작
            OrderSpawner.Instance.RestartSpawning();
        }

        
        // 초기화: 엔딩 패널은 꺼둠
        if (panelBadEnding1 != null) panelBadEnding1.SetActive(false);

        //ShowAnnouncement($"<color=red>[긴급 영업 종료]</color> 재고로 조리 가능한 메뉴가 없습니다! 잔여 주문 처리 시 즉시 마감됩니다.");
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

            // ✨ [핵심 수정] 원본 이름(한국어) 대신, 번역기를 한 번 거친 이름을 Yarn 변수에 넣습니다!
            string translatedBonus1 = bonusList.Count > 0 ? TextTranslator.GetIngredientName(bonusList[0]) : "";
            string translatedBonus2 = bonusList.Count > 1 ? TextTranslator.GetIngredientName(bonusList[1]) : "";
            dialogueRunner.VariableStorage.SetValue("$bonus1", translatedBonus1);
            dialogueRunner.VariableStorage.SetValue("$bonus2", translatedBonus2);
        }

        Debug.Log("[이어하기] 마감 정산 화면으로 복귀했습니다.");
    }

    // ✨ [NEW] 배드엔딩 1 연출 함수 (Yarn에서 호출)
    public void TriggerBadEnding1Sequence()
    {
        Debug.Log("💀 [BadEnding1] 연출 시작");

        // 🗑️ [핵심] 가게가 망했으므로 세이브 파일을 영구 삭제합니다.
        // 이제 메인 화면으로 돌아가도 '이어하기'가 불가능해집니다.
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.DeleteSaveFile();
            Debug.Log("💀 배드엔딩 1 달성: 세이브 데이터가 삭제되었습니다.");
        }

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
                        if (AchievementManager.Instance != null)
                        {
                            AchievementManager.Instance.Unlock(AchievementID.youre_fired);
                        }

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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopSFX(501);
            //AudioManager.Instance.PlayBGM(201, AudioManager.Instance.GetBGMVolume());
        }
        Time.timeScale = 1f; // 중요: 시간 다시 정상화
        DOTween.KillAll();   // 실행 중인 트윈 정리
        SceneManager.LoadScene("StartScene"); // 시작 화면 씬 이름 확인 필요
    }

    public void StartOfDay()
    {
        //if (CheckPrematureBankruptcy()) return;

        isEmergencyClosing = false; // ✨ 플래그 초기화

        if (announcementBanner != null) announcementBanner.gameObject.SetActive(false);

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

        // ✨ [NEW] 1. 로거 초기화 (오늘의 시작 자산 기록)
        if (GameDataLogger.Instance != null)
        {
            GameDataLogger.Instance.StartNewDayLog(PlayerWalletManager.Instance.CurrentBalance);
        }

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
        ReceiptLineManager.Instance.ClearCanceledReceipts(); // ✨ [NEW] 잊지 말고 꼭 비워주기!

        ReceiptSystem.CurrentReceiptID = 1; // 주문 번호 초기화
        //ReceiptSystem.CurrentOrderItemID = 1; // 메뉴 번호 초기화

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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();       // 배경음악 정지
                AudioManager.Instance.PlayLoopSFX(501); // 째깍째깍 소리 반복 재생
            }

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

            PlayDDayAnimation();

            Debug.Log("[시작] 셔터가 열리고 영업이 시작되었습니다!");
        }
    }

    // 현재 며칠째인지 계산 (시작일로부터 경과일 + 1)
    private int GetDayCount()
    {
        TimeSpan span = GameClock.gameTime.Date - new DateTime(GameClock.Instance.startYear, GameClock.Instance.startMonth, GameClock.Instance.startDay).Date; // GameClock.startYear 등 사용 추천
        return (int)span.TotalDays + 1;
    }

    public void EndOfDay()
    {
        GameClock.Pause();
        OrderSpawner.Instance.StopSpawning();

        HideAnnouncement();

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
        var todayCanceled = ReceiptLineManager.Instance.GetCanceledReceipts(); // ✨ [NEW] 취소 내역 가져오기

        int successCount = todaySuccess.Count;
        int missedCount = todayMissed.Count;
        int canceledCount = todayCanceled.Count; // ✨ [NEW]

        bool isTutorial = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorial;

        if (!isTutorial)
        {
            if (missedCount == 0 && successCount > 0)
            {
                ConsecutivePerfectDays++;

                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.Unlock(AchievementID.today_is_perfect);

                    // ✨ 2. 3일 연속 달성 시 업적 해제!
                    if (ConsecutivePerfectDays >= 3)
                    {
                        AchievementManager.Instance.Unlock(AchievementID.trust_you_eat);
                    }

                    if (ConsecutivePerfectDays >= endingCount)
                    {
                        AchievementManager.Instance.Unlock(AchievementID.any_time);
                    }
                }
            }
            else
            {
                // 실패가 1건이라도 있거나 손님이 안 왔다면 연속 기록 얄짤없이 0으로 뚝스딱스!
                ConsecutivePerfectDays = 0;
            }

            // 2. 누적 데이터 업데이트
            TotalSuccessCount += successCount;
            TotalMissedCount += (missedCount + canceledCount);

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
        }

        PackagingAreaManager.Instance.ClearAllFoods();

        var totalFailedList = new List<Receipt>();
        totalFailedList.AddRange(todayMissed);
        totalFailedList.AddRange(todayCanceled);

        //var missed = ReceiptLineManager.Instance.GetMissedReceipts();
        var successful = ReceiptLineManager.Instance.GetSuccessfulReceipts();
        DateTime today = GameClock.gameTime.Date;

        ReceiptManager.SaveMissedReceipts(totalFailedList, today);
        ReceiptManager.SaveSuccessfulReceipts(successful, today);

        // 판매 총액 계산
        int successTotal = successful.Sum(r => r.GetTotalPrice());

        // ❌ [삭제됨] 자동 세금 차감 로직 제거 (이제 UI에서 버튼 눌러서 납부함)
        // PlayerWalletManager.Instance.DeductDailyTaxes(successTotal); 

        // 손실 총액 계산
        int missedTotal = totalFailedList.Sum(r => r.GetOrders().Sum(o => o.TotalPrice));
        // 성공률 계산
        float successRate = successful.Count / (float)(successful.Count + totalFailedList.Count + 0.01f);
        OrderSpawner.Instance.SetPreviousDaySuccessRate(successRate); // 다음날 확률 반영용

        // 로그 출력
        Debug.Log($"[마감] 성공 주문 {successful.Count}건 / 총 판매금액: {successTotal:N0}원");
        Debug.Log($"[마감] 미완료 주문 {totalFailedList.Count}건 / 손실 금액: {missedTotal:N0}원");
        // 세금 로그도 여기서 띄우기 애매하므로 제거하거나 예상액으로 변경
        // Debug.Log($"[마감] 세금 {Mathf.RoundToInt(successTotal * PlayerWalletManager.Instance.taxRate):N0}원 납부");

        // ✨ [UI 정리] 마감 창이 뜨기 전에 화구 정보와 영수증 정보창을 강제로 끕니다.
        // 1. 화구 정보창 끄기 & 화구 선택 해제 (PlayerWokManager 스크롤뷰 꺼짐)
        if (StoveManager.Instance != null)
        {
            StoveManager.Instance.DeselectCurrentSlot();
            StoveManager.Instance.ClearAllStoves();
        }

        // 2. 영수증 재료 정보창 끄기 (CombinedIngredientManager 스크롤뷰 꺼짐)
        if (ReceiptLineManager.Instance != null && ReceiptLineManager.Instance.combinedIngredientManager != null)
        {
            ReceiptLineManager.Instance.combinedIngredientManager.ClearIngredientsText();
        }

        if (isTutorial)
        {
            // ✨ [NEW] 밸런싱 데이터 수집 및 CSV 저장
            if (GameDataLogger.Instance != null)
            {
                // 1. 판매된 재료 카운팅 (성공 영수증 분석)
                foreach (var r in successful)
                {
                    foreach (var order in r.GetOrders())
                    {
                        // 기본 재료 + 추가 재료 모두 'Sold'로 기록
                        // (주의: 레시피 DB 접근이 필요하지만, 일단 Extras라도 확실히 기록)
                        // 만약 MenuItem에 DefaultIngredients가 있다면 그것도 순회해야 완벽함
                        foreach (var ing in order.Menu.DefaultIngredients)
                            GameDataLogger.Instance.LogIngredientSold(ing.Key, ing.Value);

                        foreach (var extra in order.GetExtras())
                            GameDataLogger.Instance.LogIngredientSold(extra.Key, extra.Value);
                    }
                }

                // 2. 보너스 수익 기록
                if (DailyBonusManager.Instance != null)
                {
                    GameDataLogger.Instance.AddBonusIncome(DailyBonusManager.Instance.TodayAccumulatedBonus);
                }
            }
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

        // ✨ [핵심 수정] 원본 이름(한국어) 대신, 번역기를 한 번 거친 이름을 Yarn 변수에 넣습니다!
        string translatedBonus1 = bonusList.Count > 0 ? TextTranslator.GetIngredientName(bonusList[0]) : "";
        string translatedBonus2 = bonusList.Count > 1 ? TextTranslator.GetIngredientName(bonusList[1]) : "";
        dialogueRunner.VariableStorage.SetValue("$bonus1", translatedBonus1);
        dialogueRunner.VariableStorage.SetValue("$bonus2", translatedBonus2);

        // (디버깅용 로그)
        if (hasBonus)
            Debug.Log($"[Yarn 설정] 보너스 있음: {bonusList[0]}, {(bonusList.Count > 1 ? bonusList[1] : "")}");
        else
            Debug.Log("[Yarn 설정] 내일은 보너스 없음 (쉬는 날)");

        GameClock.SaveLastPlayedDate(today);

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorial)
        {
            return;
        }
        GameSaveManager.Instance.SaveGame();
    }

    public void OnClosingTimeReached()
    {
        if (isBadEndingDay) return;

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

    // ✨ [수정] 재료 소진 시 강제 조기 마감
    public void TriggerEmergencyClose(string reason)
    {
        // 1. 이미 마감 절차가 진행 중이라면 무시
        if (isEmergencyClosing || GameClock.isPaused || endOfDayPanel.activeSelf) return;

        isEmergencyClosing = true;

        int emergencyCount = PlayerPrefs.GetInt("TotalEmergencyCloses", 0);
        emergencyCount++;
        PlayerPrefs.SetInt("TotalEmergencyCloses", emergencyCount);
        PlayerPrefs.Save();

        Debug.Log($"[긴급 마감] {reason} - 재료 소진. 잔여 주문 처리 대기 중...");

        // 2. 주문 생성 즉시 중단
        OrderSpawner.Instance.StopSpawning();

        /// ✨ 1. 마우스 따라다니는 긴급 마감 툴팁 번역 ({0} 자리에 reason이 들어갑니다)
        string tooltipMsg = TextTranslator.GetUIText("Tooltip_EmergencyClose", reason);
        TooltipManager.ShowFollowMouse(TooltipType.UI, tooltipMsg, 5f);

        // ✨ 2. 화면 상단 긴급 마감 배너 텍스트 번역
        string bannerMsg = TextTranslator.GetUIText("Announcement_EmergencyClose", reason);
        ShowAnnouncement(bannerMsg);
        // 4. 영수증 감시 코루틴 시작
        StartCoroutine(WaitForReceiptsAndEmergencyClose());
    }


    private IEnumerator WaitForReceiptsAndEmergencyClose()
    {
        // 메시지 읽을 시간 3초 대기
        yield return new WaitForSeconds(3.0f);

        // 영수증이 하나라도 남아있다면 대기
        while (ReceiptLineManager.Instance.GetReceiptSlots().Count > 0)
        {
            // 🚨 [주도권 이양] 만약 대기 중에 9시(21시)가 되었다면?
            if (GameClock.gameTime.Hour >= GameClock.closingHour)
            {
                Debug.Log("[긴급 마감] 21시가 되어 정규 마감 로직으로 전환합니다.");

                // 긴급 마감 플래그 해제 (정규 마감 로직이 방해받지 않도록)
                isEmergencyClosing = false;

                // 정규 마감 로직(OnClosingTimeReached)은 GameClock에 의해 자동 호출되므로
                // 여기서는 긴급 마감 코루틴을 그냥 종료해버리면 됩니다.
                yield break;
            }

            yield return new WaitForSeconds(1.0f);
        }

        // 여기까지 왔다면: 9시가 되기 전에 영수증을 모두 처리한 것임
        Debug.Log("[긴급 마감] 모든 영수증이 처리되었습니다. 정산 시작!");

        EndOfDay();
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

        // ✨ [NEW] 현재 자산 가져오기
        int currentBalance = PlayerWalletManager.Instance.CurrentBalance;
        int normalEndingTargetBalance = 2500000; // 목표 금액: 250만 원
        int happyEndingTargetBalance = 3000000;  // 해피 엔딩 목표 금액

        if (currentBalance >= 5000000 && AchievementManager.Instance != null)
        {
            AchievementManager.Instance.Unlock(AchievementID.young_and_rich_bird);
        }

        Debug.Log($"최종 성적 - 성공률: {successRate:F1}% ({TotalSuccessCount}/{totalOrders}), 해금 재료: {unlockedIngredientsCount}개");

        // 2. 엔딩 분기
        if (successRate <= 30f)
        {
            // 배드 엔딩 2
            LoadEndingScene(badEnding2Scene);
        }
        // ✨ [NEW] 2. 자산이 250만 원 미만이면 -> 배드 엔딩 2 (성공률이 좋아도 돈이 없음)
        else if (currentBalance < normalEndingTargetBalance)
        {
            Debug.Log($"[엔딩] 자산 부족({currentBalance:N0} < {normalEndingTargetBalance:N0}) -> Bad Ending 2");
            LoadEndingScene(badEnding2Scene);
        }
        // 3. (여기 온 시점에서 성공률 > 30%이고 250만원 <= 자산 < 300만 원임)
        else if (successRate <= 70f || currentBalance < happyEndingTargetBalance)
        {
            // 성공률이 평범함 (31~70%) -> 노멀 엔딩
            Debug.Log("[엔딩] 250만원 자산 충족, 성공률 평범 -> Normal Ending");
            LoadEndingScene(normalEndingScene);
        }
        else // 4. 성공률 높음 (71% 이상) + 자산 충족
        {
            if (unlockedIngredientsCount >= 15)
            {
                // 재료도 많이 모음 -> 해피 엔딩! 🎉
                Debug.Log("[엔딩] 자산/성공률/재료 모두 충족 -> Happy Ending!");
                LoadEndingScene(happyEndingScene);
            }
            else
            {
                // 실력과 돈은 있으나 쫄보 플레이(재료 해금 부족) -> 노멀 엔딩
                Debug.Log("[엔딩] 성공률/자산은 높으나 재료 해금 부족 -> Normal Ending");
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

    public void ShowAnnouncement(string message)
    {
        if (announcementBanner == null || announcementText == null) return;

        announcementText.text = message;
        announcementBanner.gameObject.SetActive(true);

        // DOTween: 화면 밖(위)에서 아래로 튕기며 내려오는 연출
        announcementBanner.DOKill(); // 진행 중인 애니메이션 취소
        announcementBanner.anchoredPosition = new Vector2(0, 300f); // 완전히 숨겨진 위치 (패널 높이에 따라 조절)
        announcementBanner.DOAnchorPosY(-35f, 0.5f).SetEase(Ease.OutBounce); // 살짝 띄워진 제자리로 이동
    }

    // ✨ [NEW] 공지 배너 숨기기
    public void HideAnnouncement()
    {
        if (announcementBanner == null || !announcementBanner.gameObject.activeSelf) return;

        announcementBanner.DOKill();
        announcementBanner.DOAnchorPosY(300f, 0.4f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            announcementBanner.gameObject.SetActive(false);
        });
    }

    // ✨ [NEW] 애니메이션 없이 즉시 D-Day 텍스트만 업데이트 (세이브 로드용)
    public void UpdateDDayImmediate()
    {
        if (ddayText == null) return;

        DateTime startDate = new DateTime(GameClock.Instance.startYear, GameClock.Instance.startMonth, GameClock.Instance.startDay);
        int daysPassed = (GameClock.gameTime.Date - startDate.Date).Days;
        int currentDDay = endingCount - daysPassed;

        // 즉시 텍스트만 갱신
        ddayText.text = $"D - {currentDDay}";
    }

    // ✨ [NEW] 하루 시작 시 D-Day 애니메이션을 재생하는 함수
    // ✨ [수정] 위치 이동 없이 제자리에서 커지고 슬롯이 돌아가는 애니메이션
    public void PlayDDayAnimation()
    {
        if (ddayContainer == null || ddayText == null) return;

        DateTime startDate = new DateTime(GameClock.Instance.startYear, GameClock.Instance.startMonth, GameClock.Instance.startDay);
        int daysPassed = (GameClock.gameTime.Date - startDate.Date).Days;

        int currentDDay = endingCount - daysPassed;
        int previousDDay = currentDDay + 1;

        if (daysPassed == 0) previousDDay = currentDDay;

        // 초기 셋팅
        ddayText.text = $"D - {previousDDay}";
        ddayText.rectTransform.anchoredPosition = Vector2.zero;
        ddayText.color = new Color(ddayText.color.r, ddayText.color.g, ddayText.color.b, 1f);

        Sequence seq = DOTween.Sequence();

        // [STEP 1] 위치 이동 삭제! 제자리에서 1.5배로 스르륵 커짐
        seq.Append(ddayContainer.DOScale(1.5f, 0.8f).SetEase(Ease.OutCubic));

        seq.AppendInterval(0.5f);

        if (daysPassed > 0)
        {
            // [STEP 2] 슬롯머신 연출: 어제 숫자가 위로 휙! 올라가면서 투명해짐
            seq.Append(ddayText.rectTransform.DOAnchorPosY(50f, 0.3f).SetEase(Ease.InBack));
            seq.Join(ddayText.DOFade(0f, 0.3f));

            // [STEP 3] 글자 내용 교체 및 아래로 몰래 이동
            seq.AppendCallback(() =>
            {
                ddayText.text = $"D - {currentDDay}";
                ddayText.rectTransform.anchoredPosition = new Vector2(0, -50f);
            });

            // [STEP 4] 틱! 하고 튕겨 올라오며 뚜렷해짐
            seq.Append(ddayText.rectTransform.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutBack));
            seq.Join(ddayText.DOFade(1f, 0.3f));
        }
        else
        {
            // 첫날은 귀엽게 꿀렁임
            seq.Append(ddayText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1f));
        }

        seq.AppendInterval(1f);

        // [STEP 5] 제자리에서 다시 원래 크기(1배)로 얌전히 줄어듦
        seq.Append(ddayContainer.DOScale(1f, 0.8f).SetEase(Ease.InOutSine));
    }
}