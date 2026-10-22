using System;
using UnityEngine;

public class MapGrid : MonoBehaviour
{
    public static MapGrid Instance { get; private set; }

    [Header("그리드 (이 오브젝트 위치 = 맵 왼쪽 아래 모서리)")]
    public int width = 200;
    public int depth = 200;
    public float cellSize = 1f;

    [Header("레이어")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;

    [Header("몹 이동 규칙")]
    public float mobHeight = 1.8f;     // 이 높이만큼 위 공간이 비어 있어야 설 수 있음
    public float maxStepUp = 0.5f;     // 올라갈 수 있는 단차
    public float maxDropDown = 4f;     // 뛰어내릴 수 있는 높이
    public float maxSlopeAngle = 40f;  // 이보다 가파르면 못 섬
    public float rayStartHeight = 100f;

    [NonSerialized] public float[] height;
    [NonSerialized] public Vector3[] normal;
    [NonSerialized] public bool[] walkable;
    [NonSerialized] public byte[] moveMask;

    // 지형이 바뀌면 바뀐 영역을 알려줌 (FlowField, MobManager가 구독)
    public event Action<RectInt> OnGridChanged;

    // 8방향: 0 E, 1 N, 2 W, 3 S, 4 NE, 5 NW, 6 SW, 7 SE
    public static readonly int[] DX = { 1, 0, -1, 0, 1, -1, -1, 1 };
    public static readonly int[] DZ = { 0, 1, 0, -1, 1, 1, -1, -1 };
    public static int Opposite(int k) => k < 4 ? (k + 2) & 3 : 4 + ((k - 2) & 3);

    Vector3 Origin => transform.position;

    void Awake()
    {
        Instance = this;
        int count = width * depth;
        height = new float[count];
        normal = new Vector3[count];
        walkable = new bool[count];
        moveMask = new byte[count];
        BakeAll();
    }

    // ================= 베이크 =================
    public void BakeAll() => BakeRect(new RectInt(0, 0, width, depth));

    // 지형이 바뀐 영역만 다시 베이크 (제단 생성, 장애물 파괴 등)
    public void RebakeArea(Bounds area)
    {
        Physics.SyncTransforms(); // 이번 프레임에 생기거나 움직인 Collider를 물리에 반영
        BakeRect(BoundsToRect(area, 1));
    }

    // 물리 검사 없이 해당 영역을 즉시 막기 (지형이 솟아오르는 연출 중에 사용)
    public void BlockArea(Bounds area)
    {
        RectInt r = BoundsToRect(area, 0);
        for (int z = r.yMin; z < r.yMax; z++)
        for (int x = r.xMin; x < r.xMax; x++)
            walkable[Index(x, z)] = false;
        RefreshMasks(r);
    }

    void BakeRect(RectInt r)
    {
        for (int z = r.yMin; z < r.yMax; z++)
        for (int x = r.xMin; x < r.xMax; x++)
            BakeCell(x, z);
        RefreshMasks(r);
    }

    // 셀이 바뀌면 이웃 셀의 moveMask도 바뀌므로 1칸 넓혀서 다시 계산
    void RefreshMasks(RectInt r)
    {
        RectInt m = ClampRect(new RectInt(r.xMin - 1, r.yMin - 1, r.width + 2, r.height + 2));
        for (int z = m.yMin; z < m.yMax; z++)
        for (int x = m.xMin; x < m.xMax; x++)
            BakeMask(x, z);
        OnGridChanged?.Invoke(m);
    }

    void BakeCell(int x, int z)
    {
        int i = Index(x, z);
        Vector3 c = CellCenter(x, z);
        height[i] = Origin.y;
        normal[i] = Vector3.up;
        walkable[i] = false;

        // 질문 1: 바닥이 어디 있나? (트리거는 무시)
        Vector3 from = c + Vector3.up * rayStartHeight;
        if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, rayStartHeight * 2f,
                             groundMask, QueryTriggerInteraction.Ignore))
            return; // 바닥 없음 (낭떠러지, 맵 밖)

        height[i] = hit.point.y;
        normal[i] = hit.normal;
        if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
            return; // 너무 가파름

        // 질문 2: 바닥 위에 몹이 설 공간이 있나?
        float boxH = mobHeight - 0.1f;
        Vector3 boxCenter = new Vector3(c.x, hit.point.y + 0.1f + boxH * 0.5f, c.z);
        Vector3 half = new Vector3(cellSize * 0.45f, boxH * 0.5f, cellSize * 0.45f);
        if (Physics.CheckBox(boxCenter, half, Quaternion.identity,
                             obstacleMask, QueryTriggerInteraction.Ignore))
            return; // 장애물 있음

        walkable[i] = true;
    }

    void BakeMask(int x, int z)
    {
        int i = Index(x, z);
        byte m = 0;
        if (walkable[i])
        {
            for (int k = 0; k < 8; k++)
            {
                int nx = x + DX[k], nz = z + DZ[k];
                if (!InBounds(nx, nz)) continue;
                int n = Index(nx, nz);
                if (!walkable[n]) continue;

                float dh = height[n] - height[i];
                if (dh > maxStepUp) continue;    // 너무 높아서 못 올라감
                if (-dh > maxDropDown) continue; // 너무 깊어서 안 뛰어내림

                // 대각선: 양옆 칸이 모두 열려 있어야 함 (벽 모서리 통과 방지)
                if (k >= 4 && (!walkable[Index(nx, z)] || !walkable[Index(x, nz)])) continue;

                m |= (byte)(1 << k);
            }
        }
        moveMask[i] = m;
    }

    // ================= 조회 함수 =================
    public int Index(int x, int z) => z * width + x;
    public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < width && z < depth;
    public bool CanMove(int i, int dir) => (moveMask[i] & (1 << dir)) != 0;

    public Vector3 CellCenter(int x, int z) =>
        Origin + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);

    public void CellCoord(Vector3 p, out int x, out int z)
    {
        x = Mathf.FloorToInt((p.x - Origin.x) / cellSize);
        z = Mathf.FloorToInt((p.z - Origin.z) / cellSize);
    }

    public bool WorldToCell(Vector3 p, out int x, out int z)
    {
        CellCoord(p, out x, out z);
        return InBounds(x, z);
    }

    public bool IsWalkable(Vector3 p) =>
        WorldToCell(p, out int x, out int z) && walkable[Index(x, z)];

    // 높이 샘플: 주변 4칸을 섞어 부드럽게. 단, 절벽 경계에서는 섞지 않음
    public float SampleHeight(Vector3 p)
    {
        float fx = (p.x - Origin.x) / cellSize - 0.5f;
        float fz = (p.z - Origin.z) / cellSize - 0.5f;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, width - 2);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, depth - 2);
        float h00 = height[Index(x0, z0)],     h10 = height[Index(x0 + 1, z0)];
        float h01 = height[Index(x0, z0 + 1)], h11 = height[Index(x0 + 1, z0 + 1)];

        float min = Mathf.Min(Mathf.Min(h00, h10), Mathf.Min(h01, h11));
        float max = Mathf.Max(Mathf.Max(h00, h10), Mathf.Max(h01, h11));
        if (max - min > maxStepUp) // 절벽에 걸쳐 있으면 공중에 뜨지 않게 자기 칸 높이 사용
            return WorldToCell(p, out int cx, out int cz) ? height[Index(cx, cz)] : h00;

        float tx = Mathf.Clamp01(fx - x0), tz = Mathf.Clamp01(fz - z0);
        return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
    }

    // 가장 가까운 walkable 셀 찾기 (끼인 몹 꺼내기, 바위 위 플레이어 처리에 사용)
    public bool FindNearestWalkable(int x, int z, int maxRadius, out int result)
    {
        for (int r = 0; r <= maxRadius; r++)
        for (int dz = -r; dz <= r; dz++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue; // 테두리만 검사
            int nx = x + dx, nz = z + dz;
            if (InBounds(nx, nz) && walkable[Index(nx, nz)]) { result = Index(nx, nz); return true; }
        }
        result = -1;
        return false;
    }

    RectInt BoundsToRect(Bounds b, int margin)
    {
        CellCoord(b.min, out int x0, out int z0);
        CellCoord(b.max, out int x1, out int z1);
        return ClampRect(new RectInt(x0 - margin, z0 - margin,
                                     x1 - x0 + 1 + margin * 2, z1 - z0 + 1 + margin * 2));
    }

    RectInt ClampRect(RectInt r)
    {
        int xMin = Mathf.Max(0, r.xMin), zMin = Mathf.Max(0, r.yMin);
        int xMax = Mathf.Min(width, r.xMax), zMax = Mathf.Min(depth, r.yMax);
        return new RectInt(xMin, zMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, zMax - zMin));
    }
}
