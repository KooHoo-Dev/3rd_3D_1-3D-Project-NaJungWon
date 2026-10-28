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
            if (info == HitShape.Sphere) Gizmos.DrawWireSphere(center, radius);
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
        private static void DrawCircle(Vector3 center, float radius)
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
    }
}