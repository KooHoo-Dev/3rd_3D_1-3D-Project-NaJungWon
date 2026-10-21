using UnityEngine;

namespace PROJECT3D
{
    public enum EnemyType
    {
        Ground,
        Air
    }

    public enum EnemyGrade
    {
        Normal,         // 기본
        Elite,          // 엘리트
        MiniBoss,       // 중간보스
        Boss            // 보스
    }
    
    public class EnemyDto
    {
        public int ID;
        public string Name;
        public EnemyType EnemyType;
        public EnemyGrade EnemyGrade;
        public string PrefabKey;
        public int MaxHp;
        public int Damage;
        public float AttackInterval;
        public float Speed;
        public int Armor;
        public float Resist; //밀림 정도
    }
    
    [System.Serializable]
    public class EnemyDef
    {
        public int ID;
        public string Name;
        public EnemyType EnemyType;
        public EnemyGrade EnemyGrade;
        public Enemy Enemy;
        public int MaxHp;
        public int Damage;
        public float AttackInterval;
        public float Speed;
        public int Armor;
        public float Resist;
    }
}