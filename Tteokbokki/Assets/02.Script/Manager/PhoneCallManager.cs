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
    [SerializeField] private RectTransform phoneIconRt;

    [Header("Visual Settings")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color ringingColor = Color.red;

    [Header("Sound Settings")]
    public int ringSoundID = 115; // 🔥 벨소리 (Loop) ID 설정 필요

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private List<string> timeoutNodes = new();
    [SerializeField] private List<string> wrongNodes = new();

    [Header("Behavior")]
    [SerializeField] private float ringDuration = 8f;        // 이 시간 동안 안 받으면 강제 연결

    // 상태 변수
    private bool isRinging;
    private bool isInCall;

    // 🔥 전화 대기열 (중첩 해결용)
    private Queue<FailReason> callQueue = new Queue<FailReason>();
    private FailReason currentReason;

    private Coroutine ringRoutineCo;
    private Vector2 originalAnchoredPos;

    // 🔥 현재 재생 중인 벨소리 오디오 소스
    private AudioSource currentRingSource;

    // IsBusy: 울리는 중이거나 통화 중이면 바쁜 상태
    public bool IsBusy => isRinging || isInCall;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (phoneIconRt != null) originalAnchoredPos = phoneIconRt.anchoredPosition;

        SetIdleVisual();
        phoneButton.onClick.AddListener(OnPhoneClicked);

        // 대화가 끝나면 다음 로직 처리를 위해 이벤트 연결
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
    }

    // 🔔 외부에서 호출: 실패 발생 시 즉시 전화 요청
    public void TriggerCall(FailReason reason)
    {
        Debug.Log($"[PhoneCall] 전화 요청 들어옴: {reason}");

        // 이미 통화 중이거나 울리는 중이라면 대기열에 추가 (중첩 해결)
        if (IsBusy)
        {
            Debug.Log("[PhoneCall] 라인이 바쁨. 대기열에 추가.");
            callQueue.Enqueue(reason);
        }
        else
        {
            // 바로 울리기
            StartRinging(reason);
        }
    }

    private void StartRinging(FailReason reason)
    {
        isRinging = true;
        currentReason = reason;

        SetRingingVisual(true);

        // 🔥 [사운드] 벨소리 Loop 재생 (AudioManager 사용)
        if (AudioManager.Instance != null)
        {
            currentRingSource = AudioManager.Instance.PlayLoopSFX(ringSoundID, 0.13f);
        }

        // 흔들림 연출 및 타이머 시작
        if (ringRoutineCo != null) StopCoroutine(ringRoutineCo);
        ringRoutineCo = StartCoroutine(RingRoutine());
    }

    private IEnumerator RingRoutine()
    {
        float t = 0f;
        float amp = 8f, speed = 20f; // 흔들림 강도

        while (t < ringDuration && isRinging)
        {
            t += Time.deltaTime;

            // 아이콘 흔들기
            if (phoneIconRt != null)
            {
                float dx = Mathf.Sin(Time.time * speed) * amp;
                phoneIconRt.anchoredPosition = originalAnchoredPos + new Vector2(dx, 0f);
            }
            yield return null;
        }

        // 시간이 다 됨 -> 🔥 끊어지는 게 아니라 '자동 연결'
        if (isRinging)
        {
            Debug.Log("[PhoneCall] 받지 않아 강제 연결");
            StopRinging(); // 소리와 연출 끄고
            StartCall();   // 통화 시작
        }
    }

    private void StopRinging()
    {
        isRinging = false;
        if (ringRoutineCo != null) StopCoroutine(ringRoutineCo);
        ringRoutineCo = null;

        // 🔥 [사운드] 벨소리 끄기
        if (currentRingSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopSFX(currentRingSource);
            currentRingSource = null;
        }

        // 위치 복구
        if (phoneIconRt != null) phoneIconRt.anchoredPosition = originalAnchoredPos;
        // 시각적 상태는 StartCall이나 Idle로 넘어가면서 변경됨
    }

    private void OnPhoneClicked()
    {
        // 울리고 있을 때만 클릭 가능
        if (!isRinging || isInCall) return;

        Debug.Log("[PhoneCall] 플레이어가 전화를 받음");
        StopRinging();
        StartCall();
    }

    private void StartCall()
    {
        AudioManager.Instance.PlaySFX(104, 0.5f);

        isInCall = true;
        // 통화 중 비주얼 (필요 시 ringingColor 유지하거나 변경)
        SetRingingVisual(true); // 통화 중임을 표시 (빨간색 유지 등)

        string node = PickNode(currentReason);
        if (string.IsNullOrEmpty(node))
        {
            Debug.LogWarning("연결할 대화 노드가 없습니다.");
            OnDialogueComplete(); // 즉시 종료 처리
            return;
        }

        if (dialogueRunner != null)
            dialogueRunner.StartDialogue(node);
    }

    private void OnDialogueComplete()
    {
        // 대화 종료
        isInCall = false;
        SetIdleVisual();

        // 🔥 대기열 확인: 밀린 전화가 있다면 즉시 다음 전화 시작
        if (callQueue.Count > 0)
        {
            FailReason nextReason = callQueue.Dequeue();
            Debug.Log($"[PhoneCall] 대기 중이던 다음 전화 연결: {nextReason}");
            StartRinging(nextReason);
        }
    }

    private string PickNode(FailReason reason)
    {
        var list = (reason == FailReason.Timeout) ? timeoutNodes : wrongNodes;
        if (list == null || list.Count == 0) return null;
        int idx = Random.Range(0, list.Count);
        return list[idx];
    }

    private void SetIdleVisual()
    {
        if (phoneIcon != null) phoneIcon.color = idleColor;
        if (phoneIconRt != null) phoneIconRt.localRotation = Quaternion.identity;
    }

    private void SetRingingVisual(bool on)
    {
        if (phoneIcon != null) phoneIcon.color = on ? ringingColor : idleColor;
    }

    // 날짜 변경 시 대기열 초기화가 필요하다면 사용
    public void ResetDailyState()
    {
        callQueue.Clear();
        StopRinging();
        isInCall = false;
        SetIdleVisual();
    }

    // ✨ [NEW] 마감 시 모든 전화 상황 강제 종료
    public void ForceStopAllCalls()
    {
        // 1. 대기열 비우기
        callQueue.Clear();

        // 2. 울리는 중이라면 즉시 중단 (소리 끄기 포함)
        if (isRinging)
        {
            StopRinging();
        }

        // 3. 통화 중이라면 강제 종료 (Yarn 대화 중단)
        if (isInCall)
        {
            if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            {
                dialogueRunner.Stop(); // 대화창 끄기
            }
            isInCall = false;
            SetIdleVisual();
        }

        Debug.Log("[PhoneCall] 마감으로 인해 모든 전화가 강제 종료되었습니다.");
    }
}