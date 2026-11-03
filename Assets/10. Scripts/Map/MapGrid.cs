using System;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PROJECT3D
{
    // 씬의 지형을 검사하여 타일 정보를 채운다. 방향 계산은 하지 않는다.
    public class MapGrid : MonoBehaviour
    {
        public static MapGrid Instance { get; private set; }
        public SquareGrid Grid { get; private set; }
        public event Action OnGridChanged;

        [Header("격자: 이 오브젝트 위치가 맵의 왼쪽 아래 기준점")]
        public int width = 200;
        public int depth = 200;
        public float cellSize = 1f;

        [Header("바닥/장애물 레이어")]
        public LayerMask groundMask;
        public LayerMask obstacleMask;

        [Header("몹 이동 기준")]
        public float mobHeight = 1.8f;
        public float maxStepUp = 1f;
        public float maxDropDown = 4f;
        [Range(0f, 89f)] public float maxSlopeAngle = 40f;
        public float rayStartHeight = 100f;

        [Header("굽기")]
        // 한 프레임에 굽는 줄 수 (크면 빨리 끝나지만 화면이 멈칫한다고 한다.)
        [SerializeField] private int rowsPerFrame = 20;

        #region UnityMethod

        private void Awake()
        {
            Instance = this;
            Grid = new SquareGrid(width, depth, cellSize, transform.position);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region 외부 설정

        #region 굽기

        // [변경] 삭제 : BakeAll (내부 테스트용 동기 굽기. 부르는 곳 없음 → GameManager는 BakeAllAsync 사용)

        public async UniTask BakeAllAsync(CancellationToken token)
        {
            if (Grid == null) return;
            Physics.SyncTransforms();

            for (int z = 0; z < Grid.Depth; z++)
            {
                for (int x = 0; x < Grid.Width; x++)
                {
                    BakeTile(Grid.GetTileOrNull(x, z));
                }

                if ((z + 1) % rowsPerFrame == 0) await UniTask.Yield(token);
            }
            OnGridChanged?.Invoke();
        }

        // 바뀐 영역의 타일만 다시 재검사
        public void RebakeArea(Bounds area)
        {
            if (Grid == null) return;
            Physics.SyncTransforms();
            RectInt rect = GetAreaRect(area);
            for (int z = rect.yMin; z < rect.yMax; z++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                BakeTile(Grid.GetTileOrNull(x, z));
            OnGridChanged?.Invoke();
        }

        // 장애물이 솟아오르는 동안에는 물리 검사 없이 우선 막기
        public void BlockArea(Bounds area)
        {
            if (Grid == null) return;
            RectInt rect = GetAreaRect(area);
            for (int z = rect.yMin; z < rect.yMax; z++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                Grid.GetTileOrNull(x, z).Kind = TileKind.Blocked;
            OnGridChanged?.Invoke();
        }

        #endregion

        #region 이동 검사

        // 두 타일 사이를 이동할 수 있는가? 비트 저장 대신 읽기 쉬운 조건문으로 검사한다.
        public bool CanMove(Tile from, Tile to)
        {
            if (!CanCrossHeight(from, to)) return false;
            int dx = to.Coord.x - from.Coord.x;
            int dz = to.Coord.y - from.Coord.y;
            if (Mathf.Abs(dx) > 1 || Mathf.Abs(dz) > 1) return false;
            if (dx == 0 && dz == 0) return false;

            // 대각선 이동 시 양옆 벽이나 높은 턱의 모서리를 가로지르지 않는다.
            if (dx != 0 && dz != 0)
            {
                Tile sideX = Grid.GetTileOrNull(from.Coord.x + dx, from.Coord.y);
                Tile sideZ = Grid.GetTileOrNull(from.Coord.x, from.Coord.y + dz);
                if (!CanCrossHeight(from, sideX) || !CanCrossHeight(sideX, to)) return false;
                if (!CanCrossHeight(from, sideZ) || !CanCrossHeight(sideZ, to)) return false;
            }
            return true;
        }

        #endregion

        #endregion

        #region 내부 설정

        #region 굽기

        private void BakeTile(Tile tile)
        {
            Vector3 center = Grid.CellToWorld(tile.Coord);
            tile.Kind = TileKind.Blocked;
            tile.GroundHeight = center.y;
            tile.GroundNormal = Vector3.up;

            // 1. 위에서 아래로 검사하여 바닥을 찾기
            Vector3 start = center + Vector3.up * rayStartHeight;
            RaycastHit hit;
            bool foundGround = Physics.Raycast(start, Vector3.down, out hit,
                rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore);
            if (!foundGround) return;

            tile.GroundHeight = hit.point.y;
            tile.GroundNormal = hit.normal;

            // 2. 바닥이 너무 가파르면 막힌 칸
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle > maxSlopeAngle) return;

            // 3. 바닥 위에 몹이 설 공간이 있는지 검사
            float boxHeight = Mathf.Max(0.1f, mobHeight - 0.1f);
            Vector3 boxCenter = new Vector3(center.x,
                hit.point.y + 0.1f + boxHeight * 0.5f, center.z);
            Vector3 halfSize = new Vector3(Grid.CellSize * 0.45f,
                boxHeight * 0.5f, Grid.CellSize * 0.45f);
            if (Physics.CheckBox(boxCenter, halfSize, Quaternion.identity,
                obstacleMask, QueryTriggerInteraction.Ignore)) return;

            tile.Kind = TileKind.Walkable;
        }

        private RectInt GetAreaRect(Bounds area)
        {
            Vector2Int min = Grid.WorldToCell(area.min);
            Vector2Int max = Grid.WorldToCell(area.max);
            int xMin = Mathf.Clamp(min.x, 0, Grid.Width);
            int zMin = Mathf.Clamp(min.y, 0, Grid.Depth);
            int xMax = Mathf.Clamp(max.x + 1, 0, Grid.Width);
            int zMax = Mathf.Clamp(max.y + 1, 0, Grid.Depth);
            return new RectInt(xMin, zMin, xMax - xMin, zMax - zMin);
        }

        #endregion

        #region 이동 검사

        private bool CanCrossHeight(Tile from, Tile to)
        {
            if (from == null || to == null) return false;
            if (!from.IsWalkable || !to.IsWalkable) return false;

            // 경사에서도 칸 중심 높이 차이를 검사
            // 칸 크기가 커지면 완만한 경사의 높이 차이도 커지므로 제한값을 함께 조절
            float difference = to.GroundHeight - from.GroundHeight;
            if (difference > maxStepUp) return false;
            if (-difference > maxDropDown) return false;
            return true;
        }

        #endregion

        #endregion
    }
}
