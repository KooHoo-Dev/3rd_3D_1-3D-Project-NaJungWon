using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 돌진 스킬 (BS 전사 E 스킬)
    //  - 카메라가 바라보는 방향(aimDirection)으로 dashDistance만큼 빠르게 돌진
    //  - 지나가면서 몸 주변(hitRadius)에 닿은 적 전부에게 한 번씩 데미지
    //  - 플레이어의 CharacterController로 움직여서 장애물에 부딪히면 그 앞에서 멈춘다
    //  - 돌진하는 동안 : 중력 X, 무적 (끝나면 다시 중력 적용, 무적 해제)
    public class DashSkill : UnitSkill
    {
        [Header("돌진")]
        [SerializeField] private float dashDistance = 8f;        // 최대 돌진 거리
        [SerializeField] private float dashSpeed = 30f;          // 돌진 속도 (초당 거리)
        [SerializeField] private float hitRadius = 1.5f;         // 몸 주변 타격 반경 (범위 증가% 적용)

        [Header("돌진 이펙트")]
        [SerializeField] private ParticleEffect effectPrefab;    // 시작 위치 이펙트 (없으면 생략)

        private CharacterController ownerController;   // 플레이어를 움직일 컨트롤러 (벽 충돌 처리용)
        private CharacterController3D ownerMovement;   // 새로 추가 : 중력 끄기/켜기용
        private bool isDashing;
        private Vector3 dashDirection;
        private float remainDistance;  // 남은 돌진 거리
        private int dashDamage;        // 이번 돌진 데미지 (시작할 때 계산)

        private List<Enemy> hitBuffer = new List<Enemy>();               // 이번 프레임 닿은 적 (재사용)
        private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();        // 이번 돌진에서 이미 맞은 적 (한 번만 맞게)

        // 공격 시점(애니메이션 / 딜레이 후)에 불린다 → 돌진 시작
        protected override void Use(Vector3 aimDirection)
        {
            if (ownerController == null) ownerController = Owner.GetComponent<CharacterController>();
            if (ownerMovement == null) ownerMovement = Owner.GetComponent<CharacterController3D>(); // 새로 추가
            if (ownerController == null)
            {
                Debug.LogWarning($"[SkillDash] {Owner.GameObject.name}에 CharacterController가 없음");
                return;
            }

            // [변경] 몸 중심에서 화면 정중앙이 가리키는 지점으로 3D 돌진 (위/아래 대각선 포함)
            dashDirection = GetScreenCenterDirection(Owner.HitCenter, aimDirection);
            if (dashDirection.sqrMagnitude < 0.01f) return;
            dashDirection.Normalize();

            remainDistance = dashDistance;
            dashDamage = GetDamage();
            hitEnemies.Clear();
            isDashing = true;
            IsCasting = true; // 돌진 중에는 이동/점프/다른 스킬 막기

            // 몸은 똑바로 선 채 좌우 방향만 돌진 방향으로 (본체는 기울이지 않는다)
            Vector3 lookDirection = dashDirection;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.01f) Owner.transform.rotation = Quaternion.LookRotation(lookDirection);
            SetDashState(true); // 무적 + 중력 끄기
            ObjectPoolManager.PlayEffect(effectPrefab, Owner.transform.position);

            HitEnemies(); // 시작 위치에 붙어 있던 적도 맞힌다
        }

        protected override void Update()
        {
            base.Update(); // 쿨타임 / 공격 시점 처리
            if (isDashing == false) return;

            // 1. 이번 프레임 이동 (남은 거리보다 더 가지 않게)
            float step = Mathf.Min(dashSpeed * WorldClock.Instance.DeltaTime, remainDistance);
            Vector3 before = Owner.transform.position;
            ownerController.Move(dashDirection * step); // 벽이 있으면 CharacterController가 그 앞에서 막아 준다
            remainDistance -= step;

            // 2. 지나간 자리의 적 맞히기
            HitEnemies();

            // 3. 다 갔거나, 장애물에 막혀 거의 못 움직였으면 끝
            Vector3 moved = Owner.transform.position - before; // [변경] 3D 이동 거리 그대로 비교 (y를 지우지 않음)
            bool isBlocked = moved.magnitude < step * 0.5f;
            if (remainDistance <= 0f || isBlocked) EndDash();
        }

        // 몸 주변 적 중 이번 돌진에서 아직 안 맞은 적에게 데미지
        private void HitEnemies()
        {
            float radius = ApplyRangeBonus(hitRadius);
            EnemyRegistry.FindHitTargets(Owner.HitCenter, radius, HitShape.Column, Owner.HurtHalfHeight, ref hitBuffer);

            for (int i = 0; i < hitBuffer.Count; i++)
            {
                Enemy enemy = hitBuffer[i];
                if (hitEnemies.Contains(enemy)) continue; // 이미 맞음

                hitEnemies.Add(enemy);
                DealDamage(enemy, dashDamage);
            }
        }

        private void EndDash()
        {
            isDashing = false;
            IsCasting = false;
            SetDashState(false); // [변경] 무적 해제 + 중력 다시 켜기
        }

        // [변경] 돌진 중 상태 : 무적 켜기/끄기 + 중력 끄기/켜기
        private void SetDashState(bool isDash)
        {
            Player player = Owner as Player;
            if (player != null) player.IsInvincible = isDash; // 돌진 중에는 데미지를 받지 않는다

            if (ownerMovement != null) ownerMovement.SetUseGravity(isDash == false); // 돌진 중에는 중력 X
        }

        // 돌진 중에 꺼지면(플레이어 비활성화 등) 상태를 되돌린다
        protected override void OnDisable()
        {
            base.OnDisable();
            if (isDashing) EndDash();
        }
    }
}