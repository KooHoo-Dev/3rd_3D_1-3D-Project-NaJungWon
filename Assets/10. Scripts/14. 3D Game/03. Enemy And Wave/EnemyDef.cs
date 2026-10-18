using UnityEngine;

namespace PROJECT3D
{
    public class EnemyDto
    {
        public int ID;
        public string Name;
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
        public Enemy Enemy;
        public int MaxHp;
        public int Damage;
        public float AttackInterval;
        public float Speed;
        public int Armor;
        public float Resist;
    }
}