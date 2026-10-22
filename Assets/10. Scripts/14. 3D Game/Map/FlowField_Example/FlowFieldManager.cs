using System.Collections.Generic;
using UnityEngine;

public class FlowFieldManager : MonoBehaviour
{
    public static FlowFieldManager Instance { get; private set; }

    public MapGrid grid;
    public List<Transform> players = new List<Transform>();
    public float rebuildInterval = 0.2f;

    public FlowField Field { get; private set; }
    readonly List<Vector3> targets = new List<Vector3>();
    float timer;

    void Awake() => Instance = this;

    void Start() // MapGrid.Awake에서 베이크가 끝난 뒤
    {
        Field = new FlowField(grid);
        Rebuild();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (Field.dirty || timer <= 0f) Rebuild();
    }

    void Rebuild()
    {
        targets.Clear();
        foreach (var p in players) if (p != null) targets.Add(p.position);
        Field.Build(targets);
        timer = rebuildInterval;
    }

    // ===== 디버그: 플레이어 주변 20칸 표시 =====
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || Field == null || players.Count == 0 || players[0] == null) return;
        grid.CellCoord(players[0].position, out int px, out int pz);
        const int R = 20;

        for (int z = pz - R; z <= pz + R; z++)
        for (int x = px - R; x <= px + R; x++)
        {
            if (!grid.InBounds(x, z)) continue;
            int i = grid.Index(x, z);
            Vector3 c = grid.CellCenter(x, z);
            c.y = grid.height[i] + 0.05f;

            if (!grid.walkable[i])
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(c, new Vector3(0.9f, 0.05f, 0.9f) * grid.cellSize);
                continue;
            }
            if (Field.dir[i] == FlowField.NoDir) continue;
            Gizmos.color = Color.green;
            Vector3 v = FlowField.DirVec[Field.dir[i]] * 0.4f * grid.cellSize;
            Gizmos.DrawLine(c - v, c + v);
            Gizmos.DrawSphere(c + v, 0.08f);
        }
    }
}