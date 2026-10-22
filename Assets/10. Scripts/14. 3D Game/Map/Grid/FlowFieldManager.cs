using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 플레이어 위치를 모으고, 일정 시간마다 길 안내를 다시 계산.
    public class FlowFieldManager : MonoBehaviour
    {
        public static FlowFieldManager Instance { get; private set; }
        public MapGrid grid;
        public List<Transform> players = new List<Transform>();
        public float rebuildInterval = 0.2f;
        public FlowField Field { get; private set; }

        private readonly List<Vector3> targets = new List<Vector3>();
        private float timer;
        private bool rebuildRequested;

        private void Awake()
        {
            if(Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Field = new FlowField(grid);
            grid.OnGridChanged += RequestRebuild;
            Rebuild();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (rebuildRequested || timer <= 0f) Rebuild();
        }

        private void RequestRebuild()
        {
            rebuildRequested = true;
        }

        public void Rebuild()
        {
            targets.Clear();
            foreach (Transform player in players)
            {
                //여기에 플레이어 사망 시에도 추격안하게 하는 코드 추가 필요
                if (player != null && player.gameObject.activeInHierarchy)
                    targets.Add(player.position);
            }
            Field.Build(targets);
            rebuildRequested = false;
            timer = rebuildInterval;
        }

        private void OnDestroy()
        {
            if (grid != null) grid.OnGridChanged -= RequestRebuild;
            if (Instance == this) Instance = null;
        }

        // 첫 플레이어 주변의 방향표를 씬 화면에서 확인한다.
        private void OnDrawGizmos()
        {
            if (Field == null || grid == null || grid.Grid == null) return;
            if (players.Count == 0 || players[0] == null) return;
            SquareGrid squareGrid = grid.Grid;
            Vector2Int center = squareGrid.WorldToCell(players[0].position);
            for (int z = center.y - 20; z <= center.y + 20; z++)
            for (int x = center.x - 20; x <= center.x + 20; x++)
            {
                Tile tile = squareGrid.GetTileOrNull(x, z);
                if (tile == null) continue;
                Vector3 point = squareGrid.CellToWorld(tile.Coord);
                point.y = tile.GroundHeight + 0.05f;
                if (!tile.IsWalkable)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(point, new Vector3(0.8f, 0.05f, 0.8f) * squareGrid.CellSize);
                    continue;
                }
                if (tile.Direction == MoveDirection.None) continue;
                Gizmos.color = Color.green;
                Vector3 arrow = tile.DirectionVector * squareGrid.CellSize * 0.4f;
                Gizmos.DrawLine(point - arrow, point + arrow);
                Gizmos.DrawSphere(point + arrow, squareGrid.CellSize * 0.06f);
            }
        }
    }
}
