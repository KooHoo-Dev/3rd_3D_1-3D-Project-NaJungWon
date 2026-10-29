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
        [SerializeField] protected float attackRange = 1.5f; // 공격을 시작하는 거리
        [SerializeField] private float attackDelay = 0.5f; //공격 딜레이
        private float attackTimer;  // 공격 쿨타임 재기용
        private EnemyAttack attack; // 프리팹에 붙은 공격 컴포넌트 (없으면 추격 전용 몹)
        
        [Header("이동 설정")]
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

        //[Header("사망 설정")]
        
        [Header("연출 설정")]
        [SerializeField] private float riseDuration = 1f; // 바닥 -> 자기 높이 상승 시간
        public bool SpawnRiseDone = false; // 출현 연출 종료 여부
        
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int IsMove = Animator.StringToHash("IsMove");
        private static readonly int Die  = Animator.StringToHash("Die");

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
                return true;
            }

            groundY = transform.position.y; // 아래에 바닥이 없으면(맵 밖) 지금 고도를 그대로 돌려준다
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
            attack = GetComponent<EnemyAttack>();
        }
        
        protected virtual void OnEnable()
        {
            EnemyRegistry.Register(Enemy);
            LookDirection = Vector3.zero;
            // 풀에서 다시 나온 몹이 이전 값을 들고 오지 않게 초기화
            attackTimer = 0f;
            IsMoving = false;

            // 출현 연출 : 스폰 위치에서 자기 높이(GetSpawnTargetY)까지 상승
            SpawnRiseDone = false;
            transform.DOKill();

            float targetY = GetSpawnTargetY(); // 지상 = 바닥 + yOffset, 공중 = 바닥 + hoverHeight
            transform.DOMoveY(targetY, riseDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => SpawnRiseDone = true); // 끝나야 추격 시작
        }

        protected virtual void OnDisable()
        {
            transform.DOKill();
            EnemyRegistry.Unregister(Enemy);
        }
        
        protected virtual void Update()
        {
            if (Enemy.Ready == false) return;   // 표 데이터가 들어오기 전에는 대기
            if (SpawnRiseDone == false) return; // 출현 연출(DoTween) 중에는 대기
            
            UpdateHeight(); //공중몹의 추락 연출을 위해 사망후에도 y이동 가능

            if (Enemy.BaseStat.IsDead) return; //죽었으면 이동불가
            
            // 공격 컴포넌트가 있는 몹이 사거리 안에 들어오면 멈춰서 공격
            if (attack != null && Enemy.Unit.AttackSpeed > 0f && IsTargetInAttackRange())
            {
                IsMoving = false;

                attackTimer -= WorldClock.Instance.DeltaTime;
                if (attackTimer <= 0f)
                {
                    attackTimer = 1f / Enemy.Unit.AttackSpeed; // 초당 공격 횟수
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
        
        // 기본 이동 : 흐름장 추격 + 몹끼리 분리.
        // 기본 이동 : 흐름장 추격 + 몹끼리 분리.
        protected virtual void Move(float deltaTime)
        {
            Vector3 chase = GetChaseDirection();
            if (chase.sqrMagnitude > 0.1f) LookDirection = chase; // 추격 방향(흐름장)을 바라본다. 분리로 밀리는 방향은 무시

            Vector3 direction = chase + GetSeparationDirection();
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

        // 바라볼 방향으로 forward를 부드럽게 돌린다 (turnSpeed가 클수록 빨리 돈다)
        protected virtual void UpdateRotation(float deltaTime)
        {
            if (LookDirection.sqrMagnitude < 0.1f) return; // 방향이 없으면 회전 안 함

            Vector3 forward = Vector3.Slerp(transform.forward, LookDirection.normalized, turnSpeed * deltaTime);
            forward.y = 0f; // 몸이 기울지 않게 수평만
            if (forward.sqrMagnitude > 0.1f) transform.forward = forward;
        }

        #endregion
        // 기본 접지
        protected virtual void UpdateHeight()
        {
            Vector3 position = transform.position;
            position.y = GetTargetHeight(); // 바닥이 없으면 GetTargetHeight가 지금 높이를 돌려준다
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
        // 4인 멀티가 되면 여기만 '가장 가까운 플레이어'를 찾도록 바꿔야한다.
        protected CombatEntity GetTargetOrNull()
        {
            return LocalPlayer.Instance;
        }

        // 대상이 공격 사거리 안에 있는지 판정 (높이는 무시하고 바닥 평면 거리)
        private bool IsTargetInAttackRange()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return false;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude <= attackRange * attackRange;
        }

        // 공격 실행 : 애니메이션 + 공격 컴포넌트에게 "쏴라"만 전한다.
        protected virtual void ProcessAttack()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return;

            if (Animator != null) Animator.SetTrigger(Attack); // 애니메이터에 "Attack" 트리거가 있을 때만 동작
            attack.Fire(target);
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
    }
}