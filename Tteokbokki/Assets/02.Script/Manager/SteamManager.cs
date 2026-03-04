using UnityEngine;
using Steamworks;
using System;

public class SteamManager : MonoBehaviour
{
    private static SteamManager _instance;
    public uint appId = 4481630; // 군자 떡볶이 AppID

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            // 스팀 클라이언트 초기화
            SteamClient.Init(appId);
            Debug.Log("스팀 초기화 성공: " + SteamClient.Name);
        }
        catch (Exception e)
        {
            Debug.LogError("스팀 초기화 실패: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        // 게임 종료 시 반드시 셧다운!
        SteamClient.Shutdown();
    }

    void Update()
    {
        // 스팀 서버와 통신을 유지하기 위해 매 프레임 호출
        SteamClient.RunCallbacks();
    }
}