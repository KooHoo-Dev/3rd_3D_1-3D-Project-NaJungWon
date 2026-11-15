using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 스킬의 부모 클래스
    //  - 데미지/범위/지속시간 계산은 부모(UnitAttack)가 한다 (스킬 테이블 수치 + 플레이어 스탯)
    //  - 여기서는 쿨타임과 시전 상태만 관리
    public abstract class UnitSkill : UnitAttack
    {
        [Header("화면 중앙 조준 설정")]
        [SerializeField] private float     aimDistance      = 100f; // 화면 중앙 Ray 최대 거리 (아무것도 안 맞으면 이 거리의 점을 조준)
        [SerializeField] private LayerMask aimMask;                 // 화면 중앙 Ray가 맞을 레이어 (지형 / 장애물)
        [SerializeField] private float     aimEnemyDistance = 40f;  // 화면 중앙 Ray 위에서 몹을 찾는 최대 거리 (길수록 격자 검색 범위가 넓어진다)

        [Header("조준 스킬 설정")]
        [SerializeField] private bool isAimSkill = false; // 켜면 : 누르는 동안 조준(카메라 전환 + Unit_Aim 모션), 떼면 사용 / 끄면 : 누르자마자 사용

        // 외부 공개
        public    float BaseCooldown   { get; private set; }   // 테이블 쿨타임(초)
        public    float Cooldown       { get; private set; }   // 이번에 적용된 쿨타임(초). 공속 반영
        public    float CooldownRemain => cooldownTimer;       // 남은 쿨타임 (UI 표시용)
        public    bool  IsCasting      { get; protected set; } // 스킬 사용 중 여부
        public    bool  IsAimSkill     => isAimSkill;          // 조준 스킬인지
        protected float AimDistance    => aimDistance;         // 화면 중앙 Ray 최대 거리 (GroundBomb도 사용)
        // 조준을 시작해도 되는지 (쿨타임 / 시전 중 확인)
        public    bool  CanUse         => cooldownTimer <= 0f && IsCasting == false;

        // 내부 설정 : 쿨타임 / 시전
        private float   cooldownTimer;       // 남은 쿨타임
        private Vector3 delayedAimDirection; // 공격 시점에 쓸 방향 (누른 순간의 방향)
        private Ray     delayedAimRay;       // 공격 시점에 쓸 화면 중앙 Ray (사용한 순간의 카메라 기준)
        private bool    hasDelayedAimRay;    // 위 Ray가 있는지 (카메라가 없으면 false)

        // 내부 설정 : 화면 중앙 조준
        private Camera      mainCam;                            // 화면 중앙 Ray를 쏠 카메라 (처음 쓸 때 찾는다)
        private List<Enemy> aimEnemyBuffer = new List<Enemy>(); // Ray 위 몹 검색 결과 (재사용)

        #region UnityMethod

        protected override void Update()
        {
            base.Update(); // 공격 시점 처리
            if (cooldownTimer > 0f) cooldownTimer -= WorldClock.Instance.DeltaTime;
        }

        #endregion

        #region 외부 설정

        #region 스킬 사용

        // 스킬 테이블 수치 넣기
        public void Init(SkillDef def)
        {
            SetAttackStat(def.SkillStat);
            BaseCooldown = def.Cooldown;
        }

        // 키 입력이 들어왔을 때 호출
        // aimDirection = 카메라가 바라보는 3D 방향. 쿨타임 중이면 false // [변경] 주석
        public bool TryUse(Vector3 aimDirection)
        {
            if (cooldownTimer > 0f || IsCasting) return false;

            // [변경] 쿨타임 = 테이블 쿨타임 / 공속이 오른 비율 (지금 공속 / 캐릭터 기본 공속)
            //        최종 공속으로 바로 나누면 기본 공속이 1이 아닌 직업(마법사 0.8)은 시작부터 쿨타임이 길어진다
            Cooldown = BaseCooldown / GetAttackSpeedRate();
            cooldownTimer = Cooldown;

            // 바로 쓰지 않고 애니메이션 / 딜레이 후에 쓴다 (설정이 없으면 바로)
            IsCasting = true; // 기다리는 동안은 시전 중 (이동/다른 스킬 막힘)
            delayedAimDirection = aimDirection;
            // 화면 중앙 Ray도 지금 저장 (조준이 풀리면서 카메라가 돌아가는 중에 발사 시점이 와도 조준했던 곳으로)
            hasDelayedAimRay = MakeScreenCenterRay(out delayedAimRay);
            StartDelay();
            return true;
        }

        // 새로 추가 : 공속이 오른 비율 (지금 공속 / 캐릭터 기본 공속). 처음에는 직업과 상관없이 1
        private float GetAttackSpeedRate()
        {
            if (OwnerStat.AttackSpeed <= 0f) return 1f;

            Player player = Owner as Player;
            if (player == null || player.StatHandler == null) return OwnerStat.AttackSpeed; // 플레이어가 아니면 지금 공속 그대로

            float baseAttackSpeed = player.StatHandler.ChrStat.AttackSpeed; // 캐릭터 기본 공속 (테이블 값)
            if (baseAttackSpeed <= 0f) return OwnerStat.AttackSpeed;

            return OwnerStat.AttackSpeed / baseAttackSpeed;
        }

        // 공격 시점이 되면 실제 스킬 사용
        protected override void OnDelayEnd()
        {
            IsCasting = false;
            Use(delayedAimDirection);
        }

        // 실제 스킬 내용. 자식이 반드시 재정의
        protected abstract void Use(Vector3 aimDirection);

        #endregion

        #region 조준

        // 조준 중 처리 (PlayerController가 부른다). 예상 범위 표시가 필요한 스킬만 재정의
        public virtual void UpdateAim() { } // 조준 중 매 프레임
        public virtual void EndAim() { }    // 조준 끝 (사용했거나 취소)

        #endregion

        #region 화면 중앙 조준
        
        // from(발사 위치 / 몸 중심)에서 화면 정중앙이 가리키는 지점으로 가는 방향
        // 카메라 → 화면 중앙(0.5, 0.5) Ray → 맞은 지점(없으면 먼 지점)
        // 카메라가 없으면 받은 방향(카메라가 바라보는 방향)을 그대로 쓴다
        protected Vector3 GetScreenCenterDirection(Vector3 from, Vector3 aimDirection)
        {
            if (!TryGetScreenCenterRay(out Ray ray)) return aimDirection;

            // 맞은 곳이 있으면 그 지점, 없으면 Ray 끝 지점을 조준
            Vector3 targetPoint = GetScreenCenterPoint(ray, from); // 화면 중앙 Ray가 실제로 닿는 지점 (어깨 시점에서도 조준점대로 나가게)

            Vector3 direction = targetPoint - from;
            if (direction.sqrMagnitude < 0.01f) return aimDirection; // 너무 가까우면 (벽에 붙어서 쓸 때 등)
            return direction.normalized;
        }

        // 카메라 → 화면 정중앙(0.5, 0.5)으로 나가는 Ray. 카메라가 없으면 false
        // 지금 카메라가 아니라 스킬을 사용한 순간(TryUse)에 저장해 둔 Ray를 준다 (Use에서만 부르므로)
        protected bool TryGetScreenCenterRay(out Ray ray)
        {
            ray = delayedAimRay;
            return hasDelayedAimRay;
        }

        // 지금 카메라 기준 화면 중앙 Ray
        protected bool MakeScreenCenterRay(out Ray ray)
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

        #endregion

        #endregion

        #region 내부 설정

        // 화면 중앙 Ray가 실제로 닿는 지점 (지형 / 장애물). 안 닿으면 aimDistance 앞 점
        //  - 카메라와 발사 위치가 달라서(어깨 시점) 먼 점만 노리면 가까운 대상이 조준점 옆으로 빗나간다 → 닿는 점을 노린다
        //  - Ray 시작을 발사 위치 깊이까지 앞으로 옮긴다 (캐릭터 뒤의 벽 / 캐릭터 자신을 맞히지 않게)
        //  - 공격 판정과 같은 EnemyRegistry.FindHitTargets로 Ray 위의 몹을 찾는다
        private Vector3 GetScreenCenterPoint(Ray ray, Vector3 from)
        {
            float startDistance = Mathf.Max(0f, Vector3.Dot(from - ray.origin, ray.direction)); // 발사 위치를 Ray 위로 옮긴 거리
            Vector3 start = ray.GetPoint(startDistance);

            // 1. 지형 / 장애물 (물리 Ray)
            float castDistance = Mathf.Max(0f, aimDistance - startDistance); // start부터 Ray 끝까지
            Vector3 point = start + ray.direction * castDistance;
            if (castDistance > 0f && Physics.Raycast(start, ray.direction, out RaycastHit hit, castDistance, aimMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                castDistance = hit.distance; // 벽 뒤 몹은 안 찾게 여기까지만
            }

            // 2. 그 앞에 몹이 있으면 가장 가까운 몹 (새로 추가)
            Enemy enemy = FindEnemyOnRay(start, ray.direction, Mathf.Min(castDistance, aimEnemyDistance));
            if (enemy != null) point = enemy.HitCenter;
            return point;
        }

        // start에서 direction으로 length까지, Ray에 몸(피격 원통)이 걸친 몹 중 가장 가까운 몹. 없으면 null
        //  - Ray를 따라 길고 얇게 눕힌 직육면체로 기존 공격 판정(FindHitTargets)을 그대로 쓴다
        //  - 판정에 몹의 피격 반경 / 높이가 더해지므로, 상자는 거의 선(두께 0)이어도 몸에 닿으면 걸린다
        private Enemy FindEnemyOnRay(Vector3 start, Vector3 direction, float length)
        {
            if (length <= 0f) return null;

            float halfLength = length * 0.5f;
            Vector3 center = start + direction * halfLength;
            EnemyRegistry.FindHitTargets(center, 0f, HitShape.Cube, 0f, Quaternion.LookRotation(direction), halfLength, ref aimEnemyBuffer);

            Enemy nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < aimEnemyBuffer.Count; i++)
            {
                float distance = Vector3.Dot(aimEnemyBuffer[i].HitCenter - start, direction); // Ray 위로 옮긴 거리 (앞뒤만)
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = aimEnemyBuffer[i];
                }
            }
            return nearest;
        }

        #endregion
    }
}