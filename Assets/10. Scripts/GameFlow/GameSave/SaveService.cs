using System.IO;
using Jay.FileIO.JsonDataModule;
using UnityEngine;

namespace PROJECT3D
{
    // 내 데이터(PlayerData)를 파일에 저장하고 불러오는 담당
    // - 테이블/설정 : StreamingAssets (읽기만)
    // - 유저 데이터 : persistentDataPath (읽고 쓰기)
    public static class SaveService
    {
        private const string FILE_NAME = "project3d_save.json";
        
        private const string KEY_GOLD = "gold";
        private const string KEY_UNIT = "selectedUnit";
        private const string KEY_SHOP = "shopLevels";

        private static string FilePath
            => Path.Combine(Application.persistentDataPath, FILE_NAME);

        public static bool HasSave => File.Exists(FilePath);

        public static void Save(PlayerData data)
        {
            JsonDataFile file = new JsonDataFile();

            file.Register(new JsonDataModule<int>(KEY_GOLD, data.Gold))
                .Register(new JsonDataModule<int>(KEY_UNIT, data.SelectedUnitID))
                .Register(new JsonDataModule<int[]>(KEY_SHOP, data.ShopLevels));

            file.Save(FilePath);
            Debug.Log($"[SaveService] Saved : {FilePath}");
        }

        // 저장 파일이 없으면 기본값(골드 0, 캐릭터 0, 구매 없음)으로 돌려준다
        public static PlayerData Load()
        {
            PlayerData data = new PlayerData();

            JsonDataFile file = new JsonDataFile();
            var gold = new JsonDataModule<int>(KEY_GOLD, data.Gold);
            var unit = new JsonDataModule<int>(KEY_UNIT, data.SelectedUnitID);
            var shop = new JsonDataModule<int[]>(KEY_SHOP, data.ShopLevels);

            file.Register(gold)
                .Register(unit)
                .Register(shop);

            if (HasSave) file.Load(FilePath);

            data.Gold = gold.Value;
            data.SelectedUnitID = unit.Value;

            // 스탯 종류가 늘어나도 깨지지 않게 겹치는 칸만 복사한다
            int[] saved = shop.Value;
            for (int i = 0; i < data.ShopLevels.Length && i < saved.Length; i++)
            {
                data.ShopLevels[i] = saved[i];
            }

            Debug.Log($"[SaveService] Loaded : {FilePath} (저장 파일 있음 : {HasSave})");
            return data;
        }
    }
}