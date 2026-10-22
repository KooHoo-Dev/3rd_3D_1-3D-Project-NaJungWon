using System.Collections.Generic;
using UnityEngine;

public class FlowField
{
    public const byte NoDir = 255;
    public static readonly Vector3[] DirVec = new Vector3[8];

    readonly MapGrid g;
    public readonly int[] dist;   // -1 = 도달 불가, 0 = 플레이어 칸
    public readonly byte[] dir;   // 0~7 방향, 255 = 없음
    readonly int[] queue;
    public bool dirty = true;

    static FlowField()
    {
        for (int k = 0; k < 8; k++)
            DirVec[k] = new Vector3(MapGrid.DX[k], 0, MapGrid.DZ[k]).normalized;
    }

    public FlowField(MapGrid grid)
    {
        g = grid;
        int count = g.width * g.depth;
        dist = new int[count];
        dir = new byte[count];
        queue = new int[count];
        g.OnGridChanged += _ => dirty = true; // 지형이 바뀌면 다시 계산 예약
    }

    public void Build(List<Vector3> targets)
    {
        dirty = false;
        for (int i = 0; i < dist.Length; i++) dist[i] = -1;
        int head = 0, tail = 0;

        // 1) 시작점 = 플레이어들이 서 있는 칸
        foreach (var t in targets)
        {
            if (!g.WorldToCell(t, out int x, out int z)) continue;
            int s = g.Index(x, z);
            // 플레이어가 바위 위처럼 몹이 못 서는 곳에 있으면 가장 가까운 걸을 수 있는 칸을 대신 사용
            if (!g.walkable[s] && !g.FindNearestWalkable(x, z, 5, out s)) continue;
            if (dist[s] == 0) continue; // 같은 칸에 두 명
            dist[s] = 0;
            queue[tail++] = s;
        }

        // 2) BFS로 거리 채우기
        while (head < tail)
        {
            int c = queue[head++];
            int cx = c % g.width, cz = c / g.width;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + MapGrid.DX[k], nz = cz + MapGrid.DZ[k];
                if (!g.InBounds(nx, nz)) continue;
                int n = g.Index(nx, nz);
                if (dist[n] != -1) continue;
                // 핵심: 몹이 n → c로 갈 수 있는가? (방향 반대로 확인)
                if (!g.CanMove(n, MapGrid.Opposite(k))) continue;
                dist[n] = dist[c] + 1;
                queue[tail++] = n;
            }
        }

        // 3) 각 칸에서 거리가 가장 작은 이웃 방향 저장
        for (int z = 0; z < g.depth; z++)
        for (int x = 0; x < g.width; x++)
        {
            int i = g.Index(x, z);
            dir[i] = NoDir;
            if (dist[i] <= 0) continue; // 도달 불가 또는 목표 칸

            int best = dist[i];
            for (int k = 0; k < 8; k++) // 0~3(직선)을 먼저 보므로 동점이면 직선 우선
            {
                if (!g.CanMove(i, k)) continue;
                int n = g.Index(x + MapGrid.DX[k], z + MapGrid.DZ[k]);
                if (dist[n] >= 0 && dist[n] < best) { best = dist[n]; dir[i] = (byte)k; }
            }
        }
    }

    // 몹이 호출: 이 위치에서 어느 방향으로 가야 하나 (없으면 zero → 직접 추적으로 대체)
    public Vector3 GetDirection(Vector3 p)
    {
        if (!g.WorldToCell(p, out int x, out int z)) return Vector3.zero;
        byte d = dir[g.Index(x, z)];
        return d == NoDir ? Vector3.zero : DirVec[d];
    }

    public bool IsReachable(Vector3 p) =>
        g.WorldToCell(p, out int x, out int z) && dist[g.Index(x, z)] >= 0;
}
