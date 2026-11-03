using System.IO;
using Jay.FileIO.JsonDataModule;
using UnityEngine;

namespace PROJECT3D
{
    // 내 데이터(PlayerData)를 파일에 저장하고 불러오는 담당
    // - 테이블/설정 : StreamingAssets (읽기만)
    // - 유저 데이터 : [변경] 에디터 = 프로젝트 폴더 안 SaveData/ (지우기 쉽게), 빌드 = persistentDataPath (읽고 쓰기)
    public static class SaveService
    {
        private const string FILE_NAME = "project3d_save.json";
        private const string SAVE_FOLDER = "SaveData"; // 새로 추가 : 에디터에서 저장할 폴더 (Assets 밖이라 유니티가 에셋으로 안 읽는다)

        private const string KEY_GOLD = "gold";
        private const string KEY_UNIT = "selectedUnit";
        private const string KEY_SHOP = "shopLevels";

        // [변경] 에디터에서는 프로젝트 폴더/SaveData/ 에 저장 (폴더째 지우면 초기화). 빌드는 예전처럼 persistentDataPath
        //  - Application.dataPath = 프로젝트/Assets 라서 ".." 로 한 칸 위(프로젝트 폴더)
        //  - 빌드에서 dataPath(게임_Data 폴더)는 설치 위치에 따라 쓰기가 막힐 수 있어서 그대로 둔다
        private static string FilePath
        {
            get
            {
#if UNITY_EDITOR
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SAVE_FOLDER));
                if (Directory.Exists(folder) == false) Directory.CreateDirectory(folder);
                return Path.Combine(folder, FILE_NAME);
#else
                return Path.Combine(Application.persistentDataPath, FILE_NAME);
#endif
            }
        }

        public static bool HasSave => File.Exists(FilePath);

        #region 외부 설정

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

        #endregion
    }
}