using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class EnemyTable
    {
        private const string TableName = "Table/PROJECT3D/enemies.tsv";
        
        private Dictionary<int, EnemyDef> enemyDefs = new();

        public int Count => enemyDefs.Count;
        
        public EnemyDef Get(int id) => enemyDefs[id];

        public void Load()
        {
            enemyDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<EnemyDto> rows = TSVReader.ReadTable<EnemyDto>(filePath);
            foreach (EnemyDto dto in rows)
            {
                Enemy enemy = Resources.Load<Enemy>(dto.PrefabKey);
                if(enemy == null) Debug.LogWarning($"[EnemyTable] {dto.PrefabKey} 경로에 파일 없음");

                enemyDefs[dto.ID] = new EnemyDef()
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    EnemyType = dto.EnemyType,
                    EnemyGrade = dto.EnemyGrade,
                    Enemy = enemy,
                    MaxHp =  dto.MaxHp,
                    Damage = dto.Damage,
                    AttackSpeed = dto.AttackSpeed,
                    Speed = dto.Speed,
                    Armor = dto.Armor,
                    Resist = dto.Resist,
                    Exp = dto.Exp,
                    Gold = dto.Gold 
                };
            }
        }
    }
}