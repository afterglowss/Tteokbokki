using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public enum FailReason { Timeout, WrongDelivery }

public class PhoneCallManager : MonoBehaviour
{
    public static PhoneCallManager Instance { get; private set; }

    [Header("UI & Audio")]
    [SerializeField] private Button phoneButton;
    [SerializeField] private Image phoneIcon;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color ringingColor = Color.red;
    [SerializeField] private RectTransform phoneIconRt;
    [SerializeField] private AudioSource ringAudio;     // loop on
    [SerializeField] private AudioClip ringClip;

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [Tooltip("예: phone/timeout_1, phone/timeout_2 ...")]
    [SerializeField] private List<string> timeoutNodes = new();
    [Tooltip("예: phone/wrong_1, phone/wrong_2 ...")]
    [SerializeField] private List<string> wrongNodes = new();

    [Header("Behavior")]
    [SerializeField, Range(0f, 1f)] private float probTimeout = 0.30f;
    [SerializeField, Range(0f, 1f)] private float probWrong = 0.45f;
    [SerializeField] private float ringDuration = 8f;        // 안 받으면 자동 종료
    [SerializeField] private float minDelay = 1.0f;          // 실패 후 전화 시도까지 딜레이 범위
    [SerializeField] private float maxDelay = 3.0f;
    [SerializeField] private float cooldownSeconds = 30f;    // 통화/벨 이후 재시도 쿨다운
    [SerializeField] private int dailyMaxCalls = 3;          // 하루 최대 통화(벨 성공 수신 기준)

    private bool isRinging;
    private bool isInCall;
    private float lastBusyTime;
    private int todayCalls; // 필요한 경우 GameClock의 날짜 롤오버 때 0으로 초기화

    private Coroutine ringRoutineCo;
    private Vector2 shakeOffset; // 간단한 흔들림

    private Vector2 originalAnchoredPos;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        originalAnchoredPos = phoneIconRt.anchoredPosition; // 원래 위치 저장
        SetIdleVisual();
        phoneButton.onClick.AddListener(OnPhoneClicked);

        // Yarn 완료 이벤트에 구독 (Yarn Spinner 2.x)
        dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        Debug.Log("Call Start!");
        StartRinging(FailReason.Timeout);
    }

    public bool IsBusy = false/*=> isRinging || isInCall || (Time.time - lastBusyTime) < cooldownSeconds*/;

    /// 실패 지점에서 호출: 일정 시간 뒤 확률 체크 → 울리기
    public void TryScheduleCall(FailReason reason)
    {
        if (IsBusy || todayCalls >= dailyMaxCalls) return;
        float delay = Random.Range(minDelay, maxDelay);
        StartCoroutine(ScheduleCallCo(reason, delay));
    }

    private IEnumerator ScheduleCallCo(FailReason reason, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsBusy || todayCalls >= dailyMaxCalls) yield break;

        float p = ComputeProbability(reason);
        if (Random.value <= p)
            StartRinging(reason);
    }

    private float ComputeProbability(FailReason reason)
    {
        return reason == FailReason.Timeout ? probTimeout : probWrong;
    }

    private void StartRinging(FailReason reason)
    {
        lastReason = reason;
        if (IsBusy) return;
        Debug.Log("hey");
        isRinging = true;
        lastBusyTime = Time.time;

        SetRingingVisual(true);
        ringRoutineCo = StartCoroutine(RingRoutine(reason));
    }

    private IEnumerator RingRoutine(FailReason reason)
    {
        // 소리 on
        if (ringClip != null) { ringAudio.clip = ringClip; ringAudio.loop = true; ringAudio.Play(); }

        float t = 0f;
        float amp = 8f, speed = 12f; // 흔들림 강도/속도
        while (t < ringDuration && isRinging)
        {
            t += Time.deltaTime;
            // 좌우 흔들림
            float dx = Mathf.Sin(Time.time * speed) * amp;
            phoneIconRt.anchoredPosition = originalAnchoredPos + new Vector2(dx, 0f);
            yield return null;
        }

        if (isRinging) // 시간 만료로 자동 종료
            StopRinging();
    }

    private void StopRinging()
    {
        isRinging = false;
        if (ringRoutineCo != null) StopCoroutine(ringRoutineCo);
        ringRoutineCo = null;

        if (ringAudio.isPlaying) ringAudio.Stop();
        phoneIconRt.anchoredPosition = originalAnchoredPos;
        SetIdleVisual();
    }

    private void OnPhoneClicked()
    {
        if (!isRinging || isInCall) return;
        // 벨 멈추고 통화 시작
        StopRinging();
        StartCall(); // Yarn 시작은 StartCall 내부에서 마지막 실패 사유 기억분기 필요
    }

    // 최근 실패 사유를 저장해서 해당 카테고리에서 랜덤 노드 선택
    private FailReason lastReason;
    //private void StartRinging(FailReason reason, bool force = false)
    //{
    //    // 오버로드를 위한 내부 헬퍼 방지용 이름 충돌 피하려고 메서드명 분리
    //    // 위 StartRinging(FailReason)에서만 호출
    //}
    // 위 이름 충돌 방지: lastReason 세팅을 StartRinging에서 해주자.
    

    private void StartCall()
    {
        isInCall = true;

        string node = PickNode(lastReason);
        if (string.IsNullOrEmpty(node))
        {
            // 안전장치: 노드가 없으면 즉시 종료
            isInCall = false;
            return;
        }

        dialogueRunner.StartDialogue(node);
        // 통화를 받은 것으로 간주 → 일일 카운트 증가
        todayCalls++;
    }

    private string PickNode(FailReason reason)
    {
        var list = (reason == FailReason.Timeout) ? timeoutNodes : wrongNodes;
        if (list == null || list.Count == 0) return null;
        int idx = Random.Range(0, list.Count);
        return list[idx];
    }

    private void OnDialogueComplete()
    {
        // 대화 종료 → 통화 종료 → 아이콘 잠잠
        isInCall = false;
        lastBusyTime = Time.time; // 쿨다운 시작
        SetIdleVisual();
    }

    private void SetIdleVisual()
    {
        phoneIcon.color = idleColor;
        // 흔들림 해제
        if (phoneIconRt != null) phoneIconRt.localRotation = Quaternion.identity;
    }

    private void SetRingingVisual(bool on)
    {
        phoneIcon.color = on ? ringingColor : idleColor;
        // 필요 시 애니메이션 트리거나 DOTween으로도 대체 가능
    }

    // 게임 날짜가 바뀔 때 호출 (GameClock에서 이벤트 받아 초기화)
    public void ResetDailyCount()
    {
        todayCalls = 0;
    }
}
