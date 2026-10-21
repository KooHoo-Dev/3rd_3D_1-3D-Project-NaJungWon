using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    public abstract class PlayerControllerBase : MonoBehaviour
    {
        //여기 모델이랑 애니메이션 조작 부분만 담기
        public Player Player;
        public SkinnedMeshRenderer bodyRenderer;
        public Animator anim;
        
        public void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}