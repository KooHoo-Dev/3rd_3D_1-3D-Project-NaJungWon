using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class WaveTable
    {
        private const string TableName = "Table/PROJECT3D/waves.tsv";
        
        private Dictionary<int, WaveDef> waveDefs = new();

        public int Count => waveDefs.Count;

        #region 외부 설정

        public WaveDef Get(int id) => waveDefs[id];

        public void Load(EnemyTable enemyTable)
        {
            waveDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<WaveDto> rows = TSVReader.ReadTable<WaveDto>(filePath);
            foreach (WaveDto dto in rows)
            {
                EnemyDef enemyDef = enemyTable.Get(dto.EnemyID);
                if(enemyDef == null) Debug.LogWarning($"[WaveTable] {dto.EnemyID} 경로에 파일 없음");

                waveDefs[dto.ID] = new WaveDef()
                {
                    ID = dto.ID,
                    SpawnTimer = dto.SpawnTimer,
                    EnemyDef = enemyDef,
                    Count = dto.Count,
                };
            }
        }

        #endregion
    }
}