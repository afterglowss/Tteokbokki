using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Yarn.Unity;

public class EndingSceneUI : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup endingPanelGroup; // 전체 화면을 덮는 패널 (이미지+텍스트)
    public Button btnBackToTitle;        // 메인으로 버튼
    public DialogueRunner dialogueRunner;

    [Header("Shutter Animation")]
    public RectTransform shutterRect;    // ✨ [NEW] 셔터 이미지 (반드시 캔버스 가장 위에 배치)
    public float shutterDuration = 1.0f; // 셔터 올라가는 시간
    public float startDelay = 0.5f;      // 씬 로드 후 잠시 대기했다가 올리기

    [Header("Dialogue Settings")]
    public string startNodeName = "EndingStart"; // ✨ [NEW] 셔터가 올라간 후 시작할 Yarn 노드 이름

    [Header("Credits (Optional)")]
    public CanvasGroup creditsGroup;

    [Header("Timing Settings")]
    public float bgFadeDuration = 2.0f;
    public float creditsFadeDuration = 2.0f;
    public float waitBeforeButton = 3.0f;

    private void Start()
    {
        // 1. 초기 상태 설정 (전부 투명하게, 버튼 숨김)
        if (endingPanelGroup != null) endingPanelGroup.alpha = 0f;
        if (creditsGroup != null) creditsGroup.alpha = 0f;

        if (btnBackToTitle != null)
        {
            btnBackToTitle.gameObject.SetActive(false);
            btnBackToTitle.onClick.AddListener(OnTitleButtonClicked);
        }

        // Yarn 커맨드 등록 (대화가 끝난 후 연출 시작)
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler("trigger_ending_sequence", PlayEndingSequence);
        }

        // 2. ✨ 셔터 연출 시작
        PlayShutterOpenSequence();
    }

    private void PlayShutterOpenSequence()
    {
        // 셔터가 없다면 바로 대화 시작
        if (shutterRect == null)
        {
            StartDialogue();
            return;
        }

        // 1. 셔터 강제 닫힘 상태로 초기화 (화면을 가림)
        shutterRect.gameObject.SetActive(true);
        shutterRect.anchoredPosition = Vector2.zero; // 화면 중앙(또는 꽉 채운 상태)

        // 화면 높이 계산 (위로 올리기 위해)
        float height = shutterRect.rect.height > 0 ? shutterRect.rect.height : 1920f;

        // 2. 시퀀스 애니메이션
        Sequence seq = DOTween.Sequence();

        // (A) 잠시 대기 (숨 고르기)
        seq.AppendInterval(startDelay);

        // (B) 셔터 위로 올리기
        seq.Append(shutterRect.DOAnchorPosY(height, shutterDuration).SetEase(Ease.OutQuad));

        // (C) 셔터가 다 올라가면 -> 대화 시작!
        seq.OnComplete(() =>
        {
            shutterRect.gameObject.SetActive(false); // 최적화를 위해 끄기
            StartDialogue();
        });
    }

    private void StartDialogue()
    {
        if (dialogueRunner != null && !string.IsNullOrEmpty(startNodeName))
        {
            // ✨ 셔터가 걷히고 나서 대화 시작
            dialogueRunner.StartDialogue(startNodeName);
        }
        else
        {
            // 대화가 없으면 바로 엔딩 연출 (안전장치)
            PlayEndingSequence();
        }
    }

    // 대화 종료 후(Yarn 커맨드 trigger_ending_sequence) 호출됨
    private void PlayEndingSequence()
    {
        Sequence seq = DOTween.Sequence();

        // 1. 배경(메인 이미지) 페이드 인
        if (endingPanelGroup != null)
        {
            seq.Append(endingPanelGroup.DOFade(1f, bgFadeDuration));
        }

        // 2. 크레딧이 연결되어 있다면? -> 크레딧 연출 추가
        if (creditsGroup != null)
        {
            seq.AppendInterval(0.5f);
            seq.Append(creditsGroup.DOFade(1f, creditsFadeDuration));
            seq.AppendInterval(2.0f);
        }

        // 3. 마지막 대기 시간
        seq.AppendInterval(waitBeforeButton);

        // 4. 버튼 등장
        seq.AppendCallback(() => ShowReturnButton());
    }

    private void ShowReturnButton()
    {
        if (AchievementManager.Instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == "BadEnding3Scene") // (배드엔딩 2)
            {
                AchievementManager.Instance.Unlock(AchievementID.the_perpetual_intern);
            }
            else if (currentScene == "NormalEndingScene") // (노멀 엔딩)
            {
                AchievementManager.Instance.Unlock(AchievementID.official_family_member);
            }
            else if (currentScene == "HappyEndingScene") // (해피 엔딩)
            {
                AchievementManager.Instance.Unlock(AchievementID.the_tteokbokki_tycoon);
            }
        }

        if (btnBackToTitle != null)
        {
            btnBackToTitle.gameObject.SetActive(true);
            CanvasGroup btnCg = btnBackToTitle.GetComponent<CanvasGroup>();
            if (btnCg == null) btnCg = btnBackToTitle.gameObject.AddComponent<CanvasGroup>();

            btnCg.alpha = 0f;
            btnCg.DOFade(1f, 1.0f);
        }
    }

    private void OnTitleButtonClicked()
    {
        Time.timeScale = 1f;
        DOTween.KillAll();
        SceneManager.LoadScene("StartScene");
    }
}