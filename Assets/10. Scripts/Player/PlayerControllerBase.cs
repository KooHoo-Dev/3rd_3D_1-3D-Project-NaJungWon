using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    public abstract class PlayerControllerBase : MonoBehaviour
    {
        // 모델 / 애니메이션 참조 (여기에는 모델이랑 애니메이션 조작 부분만 담기)
        public Player              Player;       // 이 컨트롤러가 움직이는 플레이어
        public SkinnedMeshRenderer bodyRenderer; // 몸 렌더러
        public Animator            anim;         // 캐릭터 애니메이터
    }
}