using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 살아 있는 플레이어 "명부" (로컬 + 리모트, 최대 4명). EnemyRegistry의 플레이어판.
    public static class PlayerRegistry
    {
        private static readonly List<Player> players = new List<Player>();

        public static IReadOnlyList<Player> All => players;

        public static void Register(Player player)
        {
            if (players.Contains(player)) return; // 두 번 등록 방지
            players.Add(player);
        }

        public static void Unregister(Player player)
        {
            players.Remove(player);
        }

        // center에서 radius 안의 가장 가까운 살아 있는 플레이어. 없으면 null.
        public static Player FindNearest(Vector3 center, float radius)
        {
            Player nearest = null;
            float min = radius * radius;

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player.Stat.IsDead) continue;

                float distance = (player.transform.position - center).sqrMagnitude;
                if (distance <= min)
                {
                    min = distance;
                    nearest = player;
                }
            }

            return nearest;
        }

        // 판을 다시 시작할 때 명부를 비운다 (static이라 씬을 넘어 남는다)
        public static void Clear()
        {
            players.Clear();
        }
    }
}