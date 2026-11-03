using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    // 로비 상점 테이블 (shops.tsv)
    // 증강 레벨 테이블처럼 같은 스탯끼리 Level 1, 2, 3 순서대로 적는다
    public class ShopTable
    {
        private const string TableName = "Table/PROJECT3D/shops.tsv";

        // key : 스탯 종류, value : 단계별 데이터 (0번 = 1단계)
        private Dictionary<StatType, List<ShopDef>> shopDefs = new();

        // 상점에 보여줄 스탯 순서 (TSV에 처음 나온 순서)
        private List<StatType> statOrder = new();

        public List<StatType> StatOrder => statOrder;

        #region 외부 설정

        #region 데이터 조회

        public ShopDef Get(StatType type, int level)
        {
            return shopDefs[type][level - 1];
        }

        public int GetMaxLevel(StatType type)
        {
            if (shopDefs.ContainsKey(type) == false) return 0;
            return shopDefs[type].Count;
        }

        // 구매 단계대로 상점 스탯을 만든다 (Value가 누적값이라 그 단계 값만 넣으면 된다)
        public StatClass BuildShopStat(int[] levels)
        {
            StatClass stat = new StatClass();

            for (int i = 0; i < statOrder.Count; i++)
            {
                StatType type = statOrder[i];
                int level = levels[(int)type];
                if (level <= 0) continue;

                // 테이블 단계가 줄어든 경우를 대비해 최대 단계로 자른다
                int maxLevel = GetMaxLevel(type);
                if (level > maxLevel) level = maxLevel;

                stat.Set(type, Get(type, level).Value);
            }
            return stat;
        }

        #endregion

        #region 표 읽기

        public void Load()
        {
            shopDefs.Clear();
            statOrder.Clear();

            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<ShopDto> rows = TSVReader.ReadTable<ShopDto>(filePath);

            for (int i = 0; i < rows.Count; i++)
            {
                ShopDto dto = rows[i];

                Sprite sprite = Resources.Load<Sprite>(dto.SpriteKey);
                if (sprite == null)
                    Debug.LogWarning($"[ShopTable] {dto.SpriteKey} 경로에 파일 없음");

                ShopDef def = new ShopDef();
                def.ID = dto.ID;
                def.Name = dto.Name;
                def.Sprite = sprite;
                def.StatType = dto.StatType;
                def.Level = dto.Level;
                def.Value = dto.Value;
                def.Cost = dto.Cost;

                // 처음 나온 스탯이면 리스트부터 만들어준다
                if (shopDefs.ContainsKey(dto.StatType) == false)
                {
                    shopDefs.Add(dto.StatType, new List<ShopDef>());
                    statOrder.Add(dto.StatType);
                }

                shopDefs[dto.StatType].Add(def);
            }
        }

        #endregion

        #endregion
    }
}