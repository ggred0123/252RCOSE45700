using UnityEngine;

public class ShootEventReceiver : MonoBehaviour
{
    public BallDribble ball;  // Inspector에서 공의 BallDribble 스크립트 연결

    // 기존 함수
    public void ReleaseShot()
    {
        if(ball != null)
        {
            ball.ReleaseShot();
        }
    }

    // 덩크 릴리즈 이벤트
    public void OnDunkRelease()
    {
        if(ball != null)
        {
            ball.OnDunkRelease();
        }
    }


}