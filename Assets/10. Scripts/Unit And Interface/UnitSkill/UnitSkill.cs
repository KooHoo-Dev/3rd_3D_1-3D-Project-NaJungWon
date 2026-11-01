using UnityEngine;

namespace PROJECT3D
{
    // 스킬의 부모 클래스
    //  - 데미지/범위/지속시간 계산은 부모(UnitAttack)가 한다 (스킬 테이블 수치 + 플레이어 스탯)
    //  - 여기서는 쿨타임과 시전 상태만 관리
    public abstract class UnitSkill : UnitAttack
    {
        public float BaseCooldown { get; private set; }  // 테이블 쿨타임(초)
        public float Cooldown { get; private set; }      // 이번에 적용된 쿨타임(초). 공속 반영
        private float cooldownTimer;                     // 남은 쿨타임

        public bool IsCasting { get; protected set; }    // 스킬 사용 중 여부
        public float CooldownRemain => cooldownTimer;    // UI 표시용
        private Vector3 delayedAimDirection;             // 공격 시점에 쓸 방향 (누른 순간의 방향)

        // 화면 중앙 조준 (검기에서 부모로 옮김. 스킬 공통) =====
        [Header("화면 중앙 조준")]
        [SerializeField] private float aimDistance = 100f;  // 화면 중앙 Ray 최대 거리 (아무것도 안 맞으면 이 거리의 점을 조준)
        private Camera mainCam;

        // 스킬 테이블 수치 넣기
        public void Init(SkillDef def)
        {
            SetAttackStat(def.SkillStat);
            BaseCooldown = def.Cooldown;
        }

        protected override void Update()
        {
            base.Update(); // 새로 추가 : 공격 시점 처리
            if (cooldownTimer > 0f) cooldownTimer -= WorldClock.Instance.DeltaTime;
        }

        // 키 입력이 들어왔을 때 호출
        // aimDirection = 카메라가 바라보는 3D 방향. 쿨타임 중이면 false // [변경] 주석
        public bool TryUse(Vector3 aimDirection)
        {
            if (cooldownTimer > 0f || IsCasting) return false;

            // 쿨타임 = 테이블 쿨타임 / 플레이어 공속 배율
            Cooldown = (OwnerStat.AttackSpeed <= 0f) ? BaseCooldown : BaseCooldown / OwnerStat.AttackSpeed;
            cooldownTimer = Cooldown;

            // 바로 쓰지 않고 애니메이션 / 딜레이 후에 쓴다 (설정이 없으면 바로)
            IsCasting = true; // 기다리는 동안은 시전 중 (이동/다른 스킬 막힘)
            delayedAimDirection = aimDirection;
            StartDelay();
            return true;
        }

        // 공격 시점이 되면 실제 스킬 사용
        protected override void OnDelayEnd()
        {
            IsCasting = false;
            Use(delayedAimDirection);
        }

        // 실제 스킬 내용. 자식이 반드시 재정의
        protected abstract void Use(Vector3 aimDirection);

        // 화면 중앙 조준 (검기에서 부모로 옮김)
        // from(발사 위치 / 몸 중심)에서 화면 정중앙이 가리키는 지점으로 가는 방향
        // FPS 프로젝트와 같은 방식 : 카메라 → 화면 중앙(0.5, 0.5) Ray → 맞은 지점(없으면 먼 지점)
        // 카메라가 없으면 받은 방향(카메라가 바라보는 방향)을 그대로 쓴다
        protected Vector3 GetScreenCenterDirection(Vector3 from, Vector3 aimDirection)
        {
            if (!TryGetScreenCenterRay(out Ray ray)) return aimDirection; // [변경] Ray 만드는 부분을 아래 함수로 분리 (GroundBomb과 같이 씀)

            // 맞은 곳이 있으면 그 지점, 없으면 Ray 끝 지점을 조준
            Vector3 targetPoint = ray.GetPoint(aimDistance);

            Vector3 direction = targetPoint - from;
            if (direction.sqrMagnitude < 0.01f) return aimDirection; // 너무 가까우면 (벽에 붙어서 쓸 때 등)
            return direction.normalized;
        }

        // ===== 새로 추가 : 화면 중앙 Ray (GetScreenCenterDirection에서 분리. 바닥 지점이 필요한 GroundBomb도 사용) =====
        protected float AimDistance => aimDistance; // 화면 중앙 Ray 최대 거리

        // 카메라 → 화면 정중앙(0.5, 0.5)으로 나가는 Ray. 카메라가 없으면 false
        protected bool TryGetScreenCenterRay(out Ray ray)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null)
            {
                ray = new Ray();
                return false;
            }

            // 왼쪽 아래 (0, 0) ~ 오른쪽 위 (1, 1) 비율 좌표
            ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return true;
        }
        // ===== 새로 추가 끝 =====
    }
}