using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 살아 있는 몹 "명부" + 공간 격자(Spatial Grid).
    // 몹을 '칸(cell)'별 리스트로 나눠 담아 두고, 주변 칸만 검사.
    public static class EnemyRegistry
    {
        [Header("기본 설정")]
        // 칸 크기. separationDistance(1.5)보다 크게 잡으면 주변 9칸만 보면 된다.
        // MapGrid의 칸(지형용)과는 다른, 몹 검색 전용 칸이다.
        private const float CellSize = 2f;
        
        // 칸 좌표 → 그 칸에 있는 몹들
        private static readonly Dictionary<Vector2Int, List<Enemy>> cells = new Dictionary<Vector2Int, List<Enemy>>();

        // FindNearest에서 후보를 담아 둘 리스트 (매번 new 하지 않고 재사용)
        private static List<Enemy> nearestBuffer = new List<Enemy>();
        
        // 등록된 몹 중 '몹 위치(피벗)에서 피격 원통의 가장 먼 모서리까지' 가장 긴 거리.
        // 격자는 피벗 위치로 나뉘어 있어서, 공격 판정 때 이만큼 넉넉하게 후보를 가져와야
        // 몸이 크거나 HitPivot이 피벗에서 떨어진 몹을 놓치지 않는다.
        private static float maxHitReach = 0f;

        public static int Count { get; private set; } // 새로 추가 : 지금 명부에 있는(살아 있는) 몹 수

        [Header("출현 연출 설정")]
        // 소환 수 제한(WaveManager)은 올라오는 중인 몹도 세야 해서 따로 센다
        public static int RisingCount { get; private set; } // 출현 연출 중인 몹 수
        public static int AliveCount => Count + RisingCount; // 소환 수 제한용 : 명부 + 올라오는 중

        #region 외부 설정

        #region 출현 연출

        public static void AddRising() { RisingCount++; }
        public static void RemoveRising() { RisingCount--; }

        #endregion

        #region 명부 관리

        // 월드 좌표 → 칸 좌표 (높이는 무시하고 x, z만 쓴다)
        public static Vector2Int ToCell(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / CellSize),
                                  Mathf.FloorToInt(position.z / CellSize));
        }

        // 몹이 태어날 때(OnEnable) 명부에 올린다
        public static void Register(Enemy enemy)
        {
            enemy.RegistryCell = ToCell(enemy.transform.position);
            GetOrCreateCell(enemy.RegistryCell).Add(enemy);
            Count++; // 새로 추가

            // 피벗 → 원통 중심 거리 + 원통 중심 → 가장 먼 모서리 거리(√(반경² + 절반높이²))
            float offset = (enemy.HitCenter - enemy.transform.position).magnitude;
            float corner = Mathf.Sqrt(enemy.HurtRadius * enemy.HurtRadius + enemy.HurtHalfHeight * enemy.HurtHalfHeight);
            if (offset + corner > maxHitReach) maxHitReach = offset + corner;
        }

        // 몹이 사라질 때(OnDisable) 명부에서 뺀다
        public static void Unregister(Enemy enemy)
        {
            if (cells.TryGetValue(enemy.RegistryCell, out List<Enemy> list))
            {
                if (list.Remove(enemy)) Count--; // 실제로 빠졌을 때만 수를 줄인다
            }
        }

        // 몹이 움직인 뒤 매 프레임 불러준다. 칸이 바뀌었을 때만 옮겨 담는다.
        public static void UpdateCell(Enemy enemy)
        {
            Vector2Int now = ToCell(enemy.transform.position);
            if (now == enemy.RegistryCell) return; // 같은 칸이면 할 일 없음

            // [변경] Unregister(enemy) → 이전 칸 리스트에서만 빼기
            //  Unregister는 몹 수(Count)도 줄여서, 칸을 옮길 때마다 Count가 1씩 줄었다 (다시 더하지 않음)
            //  → 살아 있는 몹 수가 실제보다 작게 세져서 최대 몹 수(maxAliveEnemies) 제한이 안 걸리고,
            //    MinAlive(너무 적으면 바로 채움)가 계속 켜져 상시 소환이 끝없이 나오던 문제
            if (cells.TryGetValue(enemy.RegistryCell, out List<Enemy> oldList)) oldList.Remove(enemy);
            enemy.RegistryCell = now;
            GetOrCreateCell(now).Add(enemy);
        }

        // 판을 다시 시작할 때 명부를 비운다
        public static void Clear()
        {
            cells.Clear();
            maxHitReach = 0f;
            Count = 0; // 새로 추가
            RisingCount = 0; // 새로 추가
        }

        #endregion

        #region 검색

        // center에서 radius 안에 '몹 위치(피벗)'가 있는 몹들을 result에 담는다. (분리 이동 등 위치 기준용)
        public static void FindInRange(Vector3 center, float radius, ref List<Enemy> result)
        {
            result.Clear();

            float sqrRadius = radius * radius;
            int cellRange = Mathf.CeilToInt(radius / CellSize); // 몇 칸 옆까지 볼지
            Vector2Int centerCell = ToCell(center);

            for (int x = -cellRange; x <= cellRange; x++)
            {
                for (int z = -cellRange; z <= cellRange; z++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + z);
                    if (cells.TryGetValue(cell, out List<Enemy> list) == false) continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        // 높이까지 포함한 거리라서, 높이가 다른 공중몹과 지상몹은 서로 밀어내지 않는다
                        if ((list[i].transform.position - center).sqrMagnitude <= sqrRadius)
                            result.Add(list[i]);
                    }
                }
            }
        }

        // 공격 범위(center, radius, 모양)가 '피격 원통'에 닿는 살아 있는 몹들을 result에 담는다. (공격 판정용)
        // 투사체, 범위 공격, 장판 등 몹을 때리는 쪽은 전부 이걸 쓴다.
        public static void FindHitTargets(Vector3 center, float radius, HitShape info, float height, ref List<Enemy> result)
        {
            FindHitTargets(center, radius, info, height, Quaternion.identity, 0f, ref result); // 회전 / 두께 없는 판정
        }

        // rotation = 공격 범위의 회전, depth = 앞뒤 절반 두께 (둘 다 Cube만 사용)
        public static void FindHitTargets(Vector3 center, float radius, HitShape info, float height, Quaternion rotation, float depth, ref List<Enemy> result)
        {
            // 1. 넉넉하게 후보를 가져온다 (몸 크기 + 기둥 높이만큼 더)
            float searchRadius = radius + maxHitReach;
            if (info == HitShape.Column) searchRadius += height;
            if (info == HitShape.Cube) searchRadius += height + depth; // 직육면체는 높이 + 두께만큼 더
            FindInRange(center, searchRadius, ref result);

            // 2. 실제로 피격 원통에 닿는 몹만 남긴다 (뒤에서부터 지워야 순서가 안 꼬인다)
            for (int i = result.Count - 1; i >= 0; i--)
            {
                Enemy enemy = result[i];
                if (enemy.Stat.IsDead || enemy.IsHitBy(center, radius, info, height, rotation, depth) == false) // 두께 전달
                    result.RemoveAt(i);
            }
        }

        // center에서 radius 안의 가장 가까운 살아 있는 몹. exclude에 든 몹(이미 맞힌 몹 등)은 건너뛴다. 없으면 null.
        // 타겟팅 공격의 첫 대상 찾기 / 대상이 죽었을 때 다시 찾기에 쓴다.
        public static Enemy FindNearest(Vector3 center, float radius, HashSet<CombatEntity> exclude)
        {
            FindInRange(center, radius, ref nearestBuffer);

            Enemy nearest = null;
            float min = float.MaxValue;

            for (int i = 0; i < nearestBuffer.Count; i++)
            {
                Enemy enemy = nearestBuffer[i];
                if (enemy.Stat.IsDead) continue;
                if (exclude != null && exclude.Contains(enemy)) continue;

                float distance = (enemy.transform.position - center).sqrMagnitude;
                if (distance < min)
                {
                    min = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        #endregion

        #endregion

        #region 내부 설정

        private static List<Enemy> GetOrCreateCell(Vector2Int cell)
        {
            if (cells.TryGetValue(cell, out List<Enemy> list)) return list;

            list = new List<Enemy>();
            cells[cell] = list;
            return list;
        }

        #endregion
    }
}