// ===== 새로 추가 : 파일 전체 (비어 있던 클래스를 구조물 표 데이터로 새로 작성) =====
using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    // [변경] 빈 클래스 → 구조물 표(structures.tsv) 로드 (스킬 표와 같은 방식)
    public class StructureTable
    {
        private const string TableName = "Table/PROJECT3D/structures.tsv";

        private List<StructureDef> structureDefs = new();

        public int Count => structureDefs.Count;

        #region 외부 설정

        public StructureDef Get(int index) => structureDefs[index];

        public void Load()
        {
            structureDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<StructureDto> rows = TSVReader.ReadTable<StructureDto>(filePath);
            foreach (StructureDto dto in rows)
            {
                // 프리팹을 비워 둔 줄(아직 안 만든 구조물)은 경고 없이 null
                Structure prefab = null;
                if (string.IsNullOrEmpty(dto.PrefabKey) == false && dto.PrefabKey != "\"\"")
                {
                    prefab = Resources.Load<Structure>(dto.PrefabKey);
                    if (prefab == null) Debug.LogWarning($"[StructureTable] {dto.PrefabKey} 경로에 파일 없음");
                }

                structureDefs.Add(new StructureDef()
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    Type = dto.StructureType,
                    Prefab = prefab,
                    Value = dto.Value,
                    SpawnInterval = dto.SpawnInterval, // 새로 추가
                    MaxCount = dto.MaxCount,           // 새로 추가
                    Description = dto.Description,
                });
            }
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
