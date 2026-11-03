using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class AugmentTable
    {
        private const string TableName = "Table/PROJECT3D/augments.tsv";

        private Dictionary<int, AugmentDef> augmentDefs = new();

        public int Count => augmentDefs.Count;

        // 등록된 모든 증강을 foreach로 조회할 때 사용
        public IReadOnlyCollection<AugmentDef> All => augmentDefs.Values;

        #region 외부 설정

        public AugmentDef Get(int id) => augmentDefs[id];

        // 새로 추가 : 없는 ID면 null (시작 무기처럼 다른 표에 적힌 ID를 확인할 때)
        public AugmentDef GetOrNull(int id)
        {
            if (augmentDefs.TryGetValue(id, out AugmentDef def)) return def;
            return null;
        }

        public void Load()
        {
            augmentDefs.Clear();
            
            string path = Path.Combine(Application.streamingAssetsPath, TableName);
            List<AugmentDto> rows = TSVReader.ReadTable<AugmentDto>(path);

            for (int i = 0; i < rows.Count; i++)
            {
                AugmentDto dto = rows[i];

                // 프리팹은 무기증강만 있다
                WeaponAugment prefab = null;
                if (dto.Type == AugmentType.Weapon)
                {
                    prefab = Resources.Load<WeaponAugment>(dto.PrefabKey);
                    if (prefab == null) 
                        Debug.LogWarning($"[AugmentTable] {dto.PrefabKey} 경로에 파일 없음");
                }
                
                Sprite sprite = Resources.Load<Sprite>(dto.SpriteKey);
                if (sprite == null) 
                    Debug.LogWarning($"[AugmentTable] {dto.SpriteKey} 경로에 파일 없음");

                AugmentDef def = new AugmentDef();
                def.ID = dto.ID;
                def.Name = dto.Name;
                def.Prefab = prefab;
                def.Sprite = sprite;
                def.Type = dto.Type;
                def.OwnerUnitID = dto.OwnerUnitID;
                def.MainStat = dto.MainStat;
                def.NeedArousal = dto.NeedArousal;
                def.Special = dto.Special;
                def.Value = dto.Value;
                def.Description = dto.Description;
                def.ArousalDescription = dto.ArousalDescription;
                
                augmentDefs.Add(dto.ID, def);
            }
        }

        #endregion
    }
}