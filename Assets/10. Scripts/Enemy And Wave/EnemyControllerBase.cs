using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace PROJECT3D
{
    // 모든 몹 컨트롤러의 공통 부분(출현 연출, 추격, 분리, 높이, 공격 타이밍, 애니메이션)을 담당
    // - 공격 내용은 프리팹에 붙은 EnemyAttack 컴포넌트가 실행 (없으면 추격 전용 몹)
    public abstract class EnemyControllerBase : MonoBehaviour
    {
        [Header("Settings")]
        protected Enemy Enemy { get; private set; }
        protected Animator Animator { get; private set; }
        private MapGrid MapGrid => MapGrid.Instance;
        private FlowField Field => FlowFieldManager.Instance.Field;
        
        [Header("분리(Separate) 설정")]
        [SerializeField] protected float separationDistance = 1.5f; // 몹끼리 멀어지는 거리
        // 분리(Separate) 계산 때 주변 몹을 담아 두는 통 (매 프레임 새로 만들지 않고 재사용)
        private List<Enemy> nearMonsters = new List<Enemy>();
        
        [Header("공격 설정")]
        [SerializeField] private EnemyAttack attack; // 프리팹에 붙은 공격 컴포넌트 (없으면 추격 전용 몹)
        [SerializeField] protected float attackRange = 1.5f; // 공격을 시작하는 거리
        [SerializeField] private float attackDelay = 0.5f; //공격 딜레이
        private float attackTimer;  // 공격 쿨타임 재기용

        // 접촉 데미지 (모든 몹 공통)
        [Header("접촉 데미지")]
        [SerializeField] private float contactInterval = 1f;   // 같은 몹이 다시 접촉 데미지를 주기까지 (초)
        [SerializeField] private float contactDamageRate = 1f; // 공격력의 몇 배 (1 = 공격력 그대로, 0 = 접촉 데미지 없음)
        private float contactTimer;                            // 다음 접촉 데미지까지 남은 시간

        [Header("이동 설정")]
        [SerializeField] private float moveSmooth = 8f; // 방향 전환 보간 속도 (클수록 빨리 꺾는다)
        private Vector3 smoothChase;                    // 보간된 추격 방향 (칸마다 바뀌는 흐름장 방향을 부드럽게)
        protected virtual float MoveSpeed => Enemy.Unit.Speed;
        protected bool IsMoving { get; set; } // 추격 중인지 여부
        
        [Header("회전 설정")]
        [SerializeField] private float turnSpeed = 10f; // 회전 보간 속도 (클수록 빨리 돈다)
        protected Vector3 LookDirection { get; set; }   // 바라볼 수평 방향 (Vector3.zero면 회전 안 함)
        
        [Header("접지 설정")]
        [SerializeField] protected float yOffset = 1f;         // 몹좌표 offset
        [SerializeField] protected float snapRayHeight = 2f;   // 몸 위 이 높이에서 아래로 레이를 쏜다
        [SerializeField] protected float snapRayDistance = 5f; // 바닥을 찾는 최대 거리
        [SerializeField] protected LayerMask groundMask;       // 바닥으로 취급할 레이어만 검사
        [SerializeField] protected float heightSpeedRatio = 2f;  // 오르내리는 속도 = 이동속도의 몇 배
        [SerializeField] private BodyTilt bodyTilt;              // 모델 기울기 연출 (공중몹은 비워 둔다)
        private Vector3 groundNormal = Vector3.up;               // 높이 레이가 맞힌 바닥 방향
        
        [Header("연출 설정")]
        [SerializeField] private float riseDuration = 1f; // 바닥 -> 자기 높이 상승 시간
        public bool SpawnRiseDone = false; // 출현 연출 종료 여부
        
        protected static readonly int Attack = Animator.StringToHash("Attack");
        protected static readonly int IsMove = Animator.StringToHash("IsMove");
        protected static readonly int IsDash = Animator.StringToHash("IsDash");
        protected static readonly int Die  = Animator.StringToHash("Die");

        // 출현 연출(DoTween)의 목표 높이. 지상 = 바닥 + yOffset, 공중 = 바닥 + hoverHeight.
        public float GetSpawnTargetY() => GetTargetHeight();

        #region 높이 설정

        protected bool TryGetGroundHeight(out float groundY)
        {
            Vector3 origin = transform.position + Vector3.up * snapRayHeight;
            float rayLength = snapRayHeight + snapRayDistance;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength, groundMask))
            {
                groundY = hit.point.y;
                groundNormal = hit.normal; // 같은 레이에서 바닥 방향도 저장 (레이 추가 없음)
                return true;
            }

            groundY = transform.position.y; // 아래에 바닥이 없으면(맵 밖) 지금 고도를 그대로 돌려준다
            groundNormal = Vector3.up;
            return false;
        }
        

        // 목표 고도만 계산한다. 기본 = 바닥 + yOffset. 몹 종류별로 다르면 override.
        protected virtual float GetTargetHeight()
        {
            if (TryGetGroundHeight(out float groundY) == false) return transform.position.y;
            return groundY + yOffset;
        }

        #endregion
        
        #region Unity Methods

        protected virtual void Awake()
        {
            Enemy = GetComponent<Enemy>();
            Animator = GetComponentInChildren<Animator>();
        }
        
        protected virtual void OnEnable()
        {
            EnemyRegistry.AddRising(); // 명부 등록은 출현 연출이 끝난 뒤(FinishSpawnRise)로 미룸.
            // 풀에서 다시 나온 몹이 이전 값을 들고 오지 않게 초기화
            LookDirection = Vector3.zero;
            groundNormal = Vector3.up;
            smoothChase = Vector3.zero;
            attackTimer = 0f;
            contactTimer = 0f;
            stunTimer = 0f;
            stunLeftCount = 0;
            IsMoving = false;

            // 출현 연출 : 스폰 위치에서 자기 높이(GetSpawnTargetY)까지 상승
            SpawnRiseDone = false;
            transform.DOKill();

            float targetY = GetSpawnTargetY(); // 지상 = 바닥 + yOffset, 공중 = 바닥 + hoverHeight
            transform.DOMoveY(targetY, riseDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(FinishSpawnRise); // 끝나야 명부 등록 + 추격 시작
        }

        // 출현 연출 끝 → 이때부터 조준 / 피격 / 분리 대상 (올라오는 동안은 무적)
        private void FinishSpawnRise()
        {
            EnemyRegistry.RemoveRising();
            EnemyRegistry.Register(Enemy);
            SpawnRiseDone = true;
        }

        protected virtual void OnDisable()
        {
            transform.DOKill(); // 올라오는 중에 꺼지면 FinishSpawnRise는 불리지 않는다

            // 올라오는 중이었으면 그 수만 빼고, 아니면 명부에서 빼기
            if (SpawnRiseDone) EnemyRegistry.Unregister(Enemy);
            else EnemyRegistry.RemoveRising();
        }
        
        protected virtual void Update()
        {
            if (Enemy.Ready == false) return;   // 표 데이터가 들어오기 전에는 대기
            if (SpawnRiseDone == false) return; // 출현 연출(DoTween) 중에는 대기
            
            UpdateHeight(); //공중몹의 추락 연출을 위해 사망후에도 y이동 가능
            if (bodyTilt != null) bodyTilt.SetGroundNormal(groundNormal); // 높이 레이 결과로 몸 기울기
            
            if (Enemy.BaseStat.IsDead) return; //죽었으면 이동불가

            UpdateContactDamage(WorldClock.Instance.DeltaTime); // 몸이 닿아 있으면 접촉 데미지

            // 정지 중이면 이동 / 공격을 하지 않음
            if (UpdateStun(WorldClock.Instance.DeltaTime))
            {
                IsMoving = false;
                UpdateAnimation();
                return;
            }

            // 공격 컴포넌트가 있는 몹이 사거리 안에 들어오면 멈춰서 공격
            if (attack != null && Enemy.Unit.AttackSpeed > 0f && IsTargetInRange(attackRange)) // [변경] 공용 거리 판정
            {
                IsMoving = false;

                attackTimer -= WorldClock.Instance.DeltaTime;
                if (attackTimer <= 0f)
                {
                    attackTimer = 1f / Enemy.Unit.AttackSpeed; // 초당 공격 횟수
                    LookAtTarget();
                    ProcessAttack();
                }
            }
            else
            {
                Move(WorldClock.Instance.DeltaTime);
            }

            UpdateRotation(WorldClock.Instance.DeltaTime);
            EnemyRegistry.UpdateCell(Enemy); // 움직인 뒤 칸이 바뀌었으면 명부의 칸도 업데이트
            UpdateAnimation();
        }

        #endregion

        #region 이동 설정
        
        protected virtual void Move(float deltaTime)
        {
            Vector3 chase = GetChaseDirection();

            // 칸마다 딱딱 바뀌는 흐름장 방향을 보간해서 부드럽게 꺾는다
            float t = 1f - Mathf.Exp(-moveSmooth * deltaTime);
            smoothChase = Vector3.Lerp(smoothChase, chase.normalized, t);
            if (smoothChase.sqrMagnitude > 0.01f) LookDirection = smoothChase; // 추격 방향을 바라본다. 분리로 밀리는 방향은 무시

            Vector3 direction = smoothChase + GetSeparationDirection();
            if (direction.sqrMagnitude < 0.1f)
            {
                IsMoving = false;
                return;
            }

            IsMoving = ApplyMovement(direction, deltaTime);
        }
        
        // 계산한 방향으로 실제 위치를 옮기는 부분. 이번 프레임에 실제로 움직였으면 true.
        protected virtual bool ApplyMovement(Vector3 direction, float deltaTime)
        {
            // 1. 이미 이동 불가 칸에 끼어 있으면 먼저 빠져나온다
            if (TryRecoverFromBlocked(deltaTime)) return true;

            // 2. 막힌 칸으로 들어가는 이동은 미리 차단한다 (Separate에 밀려 낑기는 걸 예방)
            Vector3 step = direction.normalized * (MoveSpeed * deltaTime);
            if (CanMoveTo(transform.position + step))
            {
                transform.position += step;
                return true;
            }

            // 3. 여기 걸리는 대부분은 분리 방향이 막힌 방향일 때다.
            //    분리를 버리고 추격 방향만으로 다시 검사한다.
            Vector3 chase = GetChaseDirection();
            if (chase.sqrMagnitude > 0.01f)
            {
                Vector3 chaseStep = chase.normalized * (MoveSpeed * deltaTime);
                if (CanMoveTo(transform.position + chaseStep))
                {
                    transform.position += chaseStep;
                    return true;
                }
            }
            
            // 4. 그래도 막혀 있으면 이번 프레임은 제자리 (걷는 애니메이션도 꺼진다)
            return false;
        }

        #endregion

        #region 회전 설정

        // 대상 쪽을 바라볼 방향으로 저장 (공격 중)
        protected void LookAtTarget()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.1f) LookDirection = toTarget;
        }

        // 바라볼 방향으로 부드럽게 돌린다 (turnSpeed가 클수록 빨리 돈다)
        protected virtual void UpdateRotation(float deltaTime)
        {
            Vector3 look = LookDirection;
            look.y = 0f; // 몸이 기울지 않게 수평만
            if (look.sqrMagnitude < 0.01f) return; // 방향이 없으면 회전 안 함

            // 방향 벡터 Slerp → 회전값(Quaternion) Slerp
            Quaternion targetRotation = Quaternion.LookRotation(look);
            float t = 1f - Mathf.Exp(-turnSpeed * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        #endregion
        // 기본 접지
        protected virtual void UpdateHeight()
        {
            // 목표 고도까지 일정 속도로 오르고/내린다 (지상/공중 공통)
            Vector3 position = transform.position;
            float heightSpeed = MoveSpeed * heightSpeedRatio;
            position.y = Mathf.MoveTowards(position.y, GetTargetHeight(), heightSpeed * WorldClock.Instance.DeltaTime);
            transform.position = position;
        }

        // 흐름장에서 지금 서 있는 칸의 추격 방향을 가져오기
        protected virtual Vector3 GetChaseDirection()
        {
            Vector3 chase = Field.GetDirection(transform.position);
            chase.y = 0f; // 좌우 이동만 하고, 높이는 UpdateHeight에서 몹 종류별로 맞춘다
            return chase;
        }

        // 주변 몹에게서 멀어지는 방향. 가까이 있을수록 세게 밀어낸다 (Separate 패턴)
        protected Vector3 GetSeparationDirection()
        {
            Vector3 sum = Vector3.zero;

            // 전체 몹이 아니라 내 주변 칸에 있는 몹만 가져온다 (EnemyRegistry 공간 격자)
            EnemyRegistry.FindInRange(transform.position, separationDistance, ref nearMonsters);

            foreach (Enemy other in nearMonsters)
            {
                if (other == Enemy) continue; //본인은 포함x

                Vector3 toOther = transform.position - other.transform.position;
                toOther.y = 0f;

                float distance = toOther.magnitude;
                if (distance > 0f && distance < separationDistance)
                    sum += toOther * (1f - distance / separationDistance);
            }

            return sum;
        }

        // 다음 위치로 이동해도 되는지 MapGrid 기준으로 검사
        // 같은 칸 안에서의 움직임은 칸 이동이 아니므로 항상 허용.
        private bool CanMoveTo(Vector3 next)
        {
            Tile from = MapGrid.Grid.GetTileOrNull(transform.position);
            Tile to = MapGrid.Grid.GetTileOrNull(next);
            if (from == null || to == null) return false;
            if (to.Coord == from.Coord) return true; // 같은 칸끼리는 항상 허용

            return MapGrid.CanMove(from, to); // 턱·낙차·코너까지 맵에서 판정
        }

        // 이동 불가 칸에 끼었을 때, 가장 가까운 이동 가능 칸을 향해 걸어 나온다
        private bool TryRecoverFromBlocked(float deltaTime)
        {
            Tile tile = MapGrid.Grid.GetTileOrNull(transform.position);
            if (tile != null && tile.IsWalkable) return false; // 안 끼었으면 통과

            // 끼였다 -> 가장 가까운 이동 가능 칸을 찾아서 그쪽으로 걸어 나온다
            // (여긴 from이 막힌 칸이라 CanMove를 쓸 수 없다)
            Vector2Int coord = tile != null
                ? tile.Coord
                : MapGrid.Grid.WorldToCell(transform.position);
            Tile nearest = MapGrid.Grid.FindNearestWalkable(coord, 5);
            if (nearest == null) return false;

            Vector3 target = MapGrid.Grid.CellToWorld(nearest.Coord);
            Vector2 toTarget = new Vector2(target.x - transform.position.x, target.z - transform.position.z);
            if (toTarget.sqrMagnitude < 0.0001f) return false;

            Vector3 position = transform.position;
            position += new Vector3(toTarget.x, 0f, toTarget.y).normalized * (MoveSpeed * deltaTime);
            transform.position = position;
            return true;
        }

        // 공격/돌진 대상. 지금은 로컬 플레이어 하나.
        // 4인 멀티가 되면 여기만 '가장 가까운 플레이어'를 찾도록 수정할 것
        protected CombatEntity GetTargetOrNull()
        {
            return LocalPlayer.Instance;
        }

        // 대상이 range 안에 있는지 판정 (높이는 무시하고 바닥 평면 거리)
        protected bool IsTargetInRange(float range)
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return false;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude <= range * range;
        }

        // 내 피격 범위 기준 접촉 판정
        protected bool IsTouchingTarget(CombatEntity target, float extraRadius)
        {
            float bodyRadius = Enemy.HurtRadius + extraRadius;
            return target.IsHitBy(Enemy.HitCenter, bodyRadius, HitShape.Column, Enemy.HurtHalfHeight);
        }

        // 몸이 대상에 닿아 있으면 contactInterval마다 접촉 데미지
        private void UpdateContactDamage(float deltaTime)
        {
            if (deltaTime <= 0f) return; // 일시정지 중(증강 선택, 사망 연출)에는 안 때린다

            contactTimer -= deltaTime;
            if (contactTimer > 0f) return;

            CombatEntity target = GetTargetOrNull();
            if (target == null || CombatSystem.Instance == null) return;

            if (IsTouchingTarget(target, 0f) == false) return;

            int damage = Mathf.RoundToInt(Enemy.Unit.Damage * contactDamageRate);
            if (damage <= 0) return;

            CombatSystem.Instance.SendDamage(Enemy, target, damage, target.HitCenter);
            contactTimer = contactInterval; // 닿았을 때만 쿨타임 (안 닿아 있다가 닿으면 바로 때린다)
        }

        // 공격 실행 : 애니메이션 + 공격 컴포넌트에게 "쏴라"만 전한다.
        protected virtual void ProcessAttack()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return;
            if (Animator == null) Debug.LogWarning($"[EnemyControllerBase] Animator가 null임");
            Animator.SetTrigger(Attack); // 애니메이터에 "Attack" 트리거가 있을 때만 동작
            attack.StartFire(target); // (애니메이션 / 딜레이 후 발사)
        }

        // 공통 애니메이션 : 이동/정지 상태를 애니메이터에 넘긴다.
        protected virtual void UpdateAnimation()
        {
            if (Animator == null) return;

            Animator.SetBool(IsMove, IsMoving);
        }

        public virtual void StartDead()
        {
            Animator.SetTrigger(Die);
        }

        // 정지(스턴)
        // time초 멈춤을 interval초마다 count번 반복 (BS 마법사 진화 : 0.5초 정지를 1초마다 3번)
        // 정지 중에 또 맞으면 처음부터 다시 센다
        private float stunTime;      // 한 번 멈추는 시간
        private float stunInterval;  // 멈춤 시작 사이 간격
        private int stunLeftCount;   // 남은 멈춤 횟수
        private float stunTimer;     // 지금 멈춤의 남은 시간
        private float stunNextTimer; // 다음 멈춤까지 남은 시간

        public void Stun(float time, int count, float interval)
        {
            stunTime = time;
            stunInterval = interval;
            stunLeftCount = count;
            stunNextTimer = 0f; // 바로 첫 멈춤
        }

        // 정지 시간 진행. 지금 멈춰 있어야 하면 true
        private bool UpdateStun(float deltaTime)
        {
            if (stunLeftCount <= 0 && stunTimer <= 0f) return false;

            stunTimer -= deltaTime;
            stunNextTimer -= deltaTime;
            if (stunLeftCount > 0 && stunNextTimer <= 0f)
            {
                stunTimer = stunTime;
                stunNextTimer = stunInterval;
                stunLeftCount--;
            }

            return stunTimer > 0f;
        }

        #region DrawGizmos

        // 몹은 수가 많아서 선택했을 때만 그린다
        protected virtual void OnDrawGizmosSelected()
        {
            // 1. 공격 사거리 : 바닥 평면 거리라 높이 없는 원으로
            if (attackRange > 0f)
            {
                Gizmos.color = Color.red;
                HitGizmo.DrawCircle(transform.position, attackRange);
            }
        }

        #endregion
    }
}