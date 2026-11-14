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

        #region 높이 조회

        // position의 바닥 높이 / 경사 : 주변 4칸 값을 거리 비율로 섞는다 (쌍선형 보간)
        //  - 절벽처럼 넘을 수 없는 높이 차이의 칸은 섞지 않는다 (절벽 가장자리에서 몸이 뜨거나 가라앉지 않게)
        //  - 맵 밖이면 false
        public bool TryGetGroundHeight(Vector3 position, out float height, out Vector3 normal)
        {
            height = position.y;
            normal = Vector3.up;
            if (Grid == null) return false;

            Tile own = Grid.GetTileOrNull(position);
            if (own == null) return false;

            // 왼쪽 아래 칸 중심 기준으로 4칸과 섞는 비율(tx, tz)
            float fx = (position.x - Grid.Origin.x) / Grid.CellSize - 0.5f;
            float fz = (position.z - Grid.Origin.z) / Grid.CellSize - 0.5f;
            int x0 = Mathf.FloorToInt(fx);
            int z0 = Mathf.FloorToInt(fz);
            float tx = fx - x0;
            float tz = fz - z0;

            Tile t00 = GetBlendTile(own, x0, z0);
            Tile t10 = GetBlendTile(own, x0 + 1, z0);
            Tile t01 = GetBlendTile(own, x0, z0 + 1);
            Tile t11 = GetBlendTile(own, x0 + 1, z0 + 1);

            float bottom = Mathf.Lerp(t00.GroundHeight, t10.GroundHeight, tx);
            float top = Mathf.Lerp(t01.GroundHeight, t11.GroundHeight, tx);
            height = Mathf.Lerp(bottom, top, tz);

            Vector3 normalBottom = Vector3.Lerp(t00.GroundNormal, t10.GroundNormal, tx);
            Vector3 normalTop = Vector3.Lerp(t01.GroundNormal, t11.GroundNormal, tx);
            normal = Vector3.Lerp(normalBottom, normalTop, tz).normalized;
            return true;
        }

        // position 주변 4칸 중 가장 높은 표면 (바닥 + 장애물 꼭대기). 공중 몹이 장애물 모서리에 걸리지 않게 가장 높은 값
        public bool TryGetSurfaceHeight(Vector3 position, out float height)
        {
            height = position.y;
            if (Grid == null) return false;

            Tile own = Grid.GetTileOrNull(position);
            if (own == null) return false;

            float fx = (position.x - Grid.Origin.x) / Grid.CellSize - 0.5f;
            float fz = (position.z - Grid.Origin.z) / Grid.CellSize - 0.5f;
            int x0 = Mathf.FloorToInt(fx);
            int z0 = Mathf.FloorToInt(fz);

            height = own.SurfaceHeight;
            for (int z = z0; z <= z0 + 1; z++)
            {
                for (int x = x0; x <= x0 + 1; x++)
                {
                    Tile tile = Grid.GetTileOrNull(x, z);
                    if (tile != null) height = Mathf.Max(height, tile.SurfaceHeight);
                }
            }
            return true;
        }

        #endregion

        #endregion

        #region 내부 설정

        // 높이 섞기에 쓸 칸. 없거나 / 막혔거나 / 내 칸과 넘을 수 없는 높이 차이(절벽)면 내 칸으로 대신
        private Tile GetBlendTile(Tile own, int x, int z)
        {
            Tile tile = Grid.GetTileOrNull(x, z);
            if (tile == null || tile.IsWalkable == false) return own;

            float difference = tile.GroundHeight - own.GroundHeight;
            if (difference > maxStepUp || -difference > maxStepUp) return own; // 한 걸음에 오르내릴 수 있는 높이까지만 섞는다
            return tile;
        }

        #region 굽기

        private void BakeTile(Tile tile)
        {
            Vector3 center = Grid.CellToWorld(tile.Coord);
            tile.Kind = TileKind.Blocked;
            tile.GroundHeight = center.y;
            tile.GroundNormal = Vector3.up;
            tile.SurfaceHeight = center.y;

            // 1. 위에서 아래로 검사하여 바닥을 찾기
            Vector3 start = center + Vector3.up * rayStartHeight;
            RaycastHit hit;
            bool foundGround = Physics.Raycast(start, Vector3.down, out hit,
                rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore);
            if (!foundGround) return;

            tile.GroundHeight = hit.point.y;
            tile.GroundNormal = hit.normal;
            tile.SurfaceHeight = GetSurfaceHeight(start, hit.point.y); // 장애물 꼭대기까지 (공중 몹 고도용)

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

        // 위에서 아래로 바닥 + 장애물을 같이 검사해서 처음 맞는 표면 높이 (장애물이 없으면 바닥 높이)
        //  - 굽는 순간에 한 번만 쏜다 → 공중 몹이 매 프레임 레이를 쏘지 않아도 된다
        //  - 칸 중심 한 점만 재므로, 칸보다 가는 장애물은 빗나갈 수 있다
        private float GetSurfaceHeight(Vector3 start, float groundHeight)
        {
            if (Physics.Raycast(start, Vector3.down, out RaycastHit hit,
                rayStartHeight * 2f, groundMask | obstacleMask, QueryTriggerInteraction.Ignore))
                return Mathf.Max(hit.point.y, groundHeight);

            return groundHeight;
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
