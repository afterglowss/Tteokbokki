using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    void Start()
    {
        SetAspectRatio();
    }

    // 해상도가 바뀔 때마다 체크하려면 Update에 넣어도 되지만, 
    // 보통은 시작할 때 한 번이면 충분합니다.
    void SetAspectRatio()
    {
        // 우리가 원하는 고정 비율 (16:9)
        float targetaspect = 16.0f / 9.0f;

        // 현재 모니터의 비율
        float windowaspect = (float)Screen.width / (float)Screen.height;

        // 비율 차이 계산
        float scaleheight = windowaspect / targetaspect;

        Camera camera = GetComponent<Camera>();

        // 1. 모니터가 더 납작한 경우 (위아래가 남음 -> 레터박스)
        // 예: 16:10 모니터, 4:3 모니터 등
        if (scaleheight < 1.0f)
        {
            Rect rect = camera.rect;

            rect.width = 1.0f;
            rect.height = scaleheight;
            rect.x = 0;
            rect.y = (1.0f - scaleheight) / 2.0f;

            camera.rect = rect;
        }
        // 2. 모니터가 더 길쭉한 경우 (양옆이 남음 -> 필러박스)
        // 예: 21:9 울트라 와이드 모니터, 최신 스마트폰 등
        else
        {
            float scalewidth = 1.0f / scaleheight;

            Rect rect = camera.rect;

            rect.width = scalewidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scalewidth) / 2.0f;
            rect.y = 0;

            camera.rect = rect;
        }
    }

    // 혹시 게임 도중 해상도를 바꿀 수 있다면 이 함수를 호출해줘야 함
    public void Refresh()
    {
        SetAspectRatio();
    }
}