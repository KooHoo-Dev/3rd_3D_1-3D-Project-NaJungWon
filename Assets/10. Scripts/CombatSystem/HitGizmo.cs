using UnityEngine;

namespace PROJECT3D
{
    // AI로 뽑은 범위 그리게 (OnDrawGizmos 안에서 호출)
    // 피격 원통(CombatEntity)과 공격 범위(투사체 등)가 같은 모양으로 그려짐
    public static class HitGizmo
    {
        private const int CircleSegments = 20; // 원을 몇 개의 선분으로 그릴지

        // 판정 모양대로 그린다 (구 / 원통). halfHeight는 원통일 때만 쓴다
        public static void Draw(Vector3 center, float radius, HitShape info, float halfHeight)
        {
            Draw(center, radius, info, halfHeight, Quaternion.identity, 0f); // [변경] 회전 / 두께 없는 그리기
        }
        
        // rotation / halfDepth는 Cube만 사용
        public static void Draw(Vector3 center, float radius, HitShape info, float halfHeight, Quaternion rotation, float halfDepth)
        {
            if (info == HitShape.Sphere) Gizmos.DrawWireSphere(center, radius);
            else if (info == HitShape.Cube) DrawCube(center, radius, halfHeight, halfDepth, rotation); // [변경]
            else DrawCylinder(center, radius, halfHeight);
        }

        // 바닥과 수직인 원통 : 위/아래 원 두 개 + 세로선 4개 (Gizmos에는 원통 그리기가 없다)
        public static void DrawCylinder(Vector3 center, float radius, float halfHeight)
        {
            Vector3 top = center + Vector3.up * halfHeight;
            Vector3 bottom = center - Vector3.up * halfHeight;

            DrawCircle(top, radius);
            DrawCircle(bottom, radius);

            Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
            Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
            Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
            Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
        }

        // 바닥과 평행한 원을 선분으로 그린다
        public static void DrawCircle(Vector3 center, float radius) // [변경] private → public (몹 사거리처럼 높이를 안 보는 범위 그리기용)
        {
            Vector3 prev = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
        
        // 직육면체 (가로 = 반경 x2, 세로 = 절반 높이 x2, 앞뒤 = 절반 두께 x2)
        public static void DrawCube(Vector3 center, float radius, float halfHeight, float halfDepth, Quaternion rotation)
        {
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one); // 공격 범위의 위치/회전 그대로
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(radius * 2f, halfHeight * 2f, halfDepth * 2f));
            Gizmos.matrix = Matrix4x4.identity; // 다른 Gizmo에 영향 없게 되돌린다
        }
    }
}