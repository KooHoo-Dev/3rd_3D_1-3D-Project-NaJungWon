using UnityEngine;

namespace PROJECT3D
{
    // 맵 전체 타일을 씬 화면에 그려서 확인하는 테스트용 (Play 중, 맵 굽기가 끝난 뒤)
    //   빨강       = 막힌 칸
    //   초록 → 파랑 = 걸을 수 있는 칸 (높을수록 파랑)
    //   노랑       = 걸을 수는 있지만 플레이어에게 갈 길이 없는 칸 (장애물에 갇힌 곳)
    public class MapTester : MonoBehaviour
    {
        [Header("그리기 설정")]
        [SerializeField] private bool onlyWhenSelected = false; // 켜면 이 오브젝트를 선택했을 때만 그린다
        [SerializeField] private bool showBlocked = true;
        [SerializeField] private bool showWalkable = true;
        [SerializeField] private bool showUnreachable = true;   // 흐름장 거리가 -1 인 칸 (몹이 플레이어에게 못 가는 칸)
        [SerializeField] private bool showDirection = false;    // 이동 방향 화살표 (칸이 많아서 켜면 느려진다)
        [SerializeField] private float maxColorHeight = 20f;    // 이 높이에서 가장 파란색
        [SerializeField, Min(1)] private int drawStep = 1;      // 몇 칸마다 그릴지 (화면이 느리면 2~4)

        private readonly Color blockedColor = new Color(1f, 0.2f, 0.2f);
        private readonly Color lowColor = new Color(0.3f, 0.9f, 0.3f);
        private readonly Color highColor = new Color(0.2f, 0.4f, 1f);
        private readonly Color unreachableColor = new Color(1f, 0.9f, 0f);

        #region UnityMethod

        private void OnDrawGizmos()
        {
            if (onlyWhenSelected == false) DrawTiles();
        }

        private void OnDrawGizmosSelected()
        {
            if (onlyWhenSelected) DrawTiles();
        }

        #endregion

        #region 외부 설정

        // 인스펙터 컴포넌트 오른쪽 위 ⋮ 메뉴에서 실행 (Play 중)
        [ContextMenu("타일 통계 출력")]
        private void LogTileCount()
        {
            if (MapGrid.Instance == null || MapGrid.Instance.Grid == null)
            {
                Debug.LogWarning("격자가 아직 없습니다. Play 중 맵 굽기가 끝난 뒤 실행하세요.");
                return;
            }

            SquareGrid grid = MapGrid.Instance.Grid;
            bool canCheckReach = CanCheckReach();
            int walkable = 0;
            int blocked = 0;
            int unreachable = 0;

            for (int z = 0; z < grid.Depth; z++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Tile tile = grid.GetTileOrNull(x, z);
                    if (tile.IsWalkable == false) blocked++;
                    else if (canCheckReach && tile.Distance < 0) unreachable++;
                    else walkable++;
                }
            }

            int total = grid.Width * grid.Depth;
            Debug.Log($"[MapTester] 전체 {total}칸 / 걸을 수 있는 칸 {walkable} / 막힌 칸 {blocked} / 갇힌 칸 {unreachable}");
        }

        #endregion

        #region 내부 설정

        private void DrawTiles()
        {
            if (MapGrid.Instance == null || MapGrid.Instance.Grid == null) return; // 아직 Play 전이거나 격자가 없음
            SquareGrid grid = MapGrid.Instance.Grid;
            bool canCheckReach = CanCheckReach();

            Vector3 cubeSize = new Vector3(0.8f, 0.05f, 0.8f) * grid.CellSize;

            for (int z = 0; z < grid.Depth; z += drawStep)
            {
                for (int x = 0; x < grid.Width; x += drawStep)
                {
                    Tile tile = grid.GetTileOrNull(x, z);
                    if (tile == null) continue;

                    Vector3 point = grid.CellToWorld(tile.Coord);
                    point.y = tile.GroundHeight + 0.05f;

                    // 1. 막힌 칸
                    if (tile.IsWalkable == false)
                    {
                        if (showBlocked == false) continue;
                        Gizmos.color = blockedColor;
                        Gizmos.DrawCube(point, cubeSize);
                        continue;
                    }

                    // 2. 걸을 수 있지만 플레이어에게 못 가는 칸 (갇힌 곳)
                    if (canCheckReach && tile.Distance < 0)
                    {
                        if (showUnreachable == false) continue;
                        Gizmos.color = unreachableColor;
                        Gizmos.DrawCube(point, cubeSize);
                        continue;
                    }

                    // 3. 걸을 수 있는 칸 (높이별 색)
                    if (showWalkable)
                    {
                        float t = Mathf.Clamp01(tile.GroundHeight / maxColorHeight);
                        Gizmos.color = Color.Lerp(lowColor, highColor, t);
                        Gizmos.DrawCube(point, cubeSize);
                    }

                    // 4. 이동 방향 화살표
                    if (showDirection && tile.Direction != MoveDirection.None)
                    {
                        Gizmos.color = Color.white;
                        Vector3 arrow = tile.DirectionVector * grid.CellSize * 0.4f;
                        Gizmos.DrawLine(point - arrow, point + arrow);
                    }
                }
            }
        }

        // 흐름장이 플레이어 기준으로 한 번이라도 계산됐을 때만 "갇힌 칸"을 판단할 수 있다
        private bool CanCheckReach()
        {
            if (FlowFieldManager.Instance == null) return false;
            if (FlowFieldManager.Instance.Field == null) return false;
            return FlowFieldManager.Instance.players.Count > 0;
        }

        #endregion
    }
}