// ===== 새로 추가 : 파일 전체 (비어 있던 클래스를 구조물 표 데이터로 새로 작성) =====
using UnityEngine;

namespace PROJECT3D
{
    // [변경] 빈 클래스 → 구조물 표(structures.tsv) 데이터
    public enum StructureType
    {
        Heal,   // 회복 : 활성화한 플레이어 최대 체력의 Value% 회복
        Magnet, // 자석 : 맵의 모든 경험치 구슬이 활성화한 플레이어에게
        Boss,   // 보스 소환 (아직 없음. 프리팹을 비워 두면 소환되지 않는다)
    }

    public class StructureDto
    {
        public int ID;
        public string Name;
        public StructureType StructureType;
        public string PrefabKey;
        public float Value;
        public float SpawnInterval; // 새로 추가
        public int MaxCount;        // 새로 추가
        public string Description;
    }

    [System.Serializable]
    public class StructureDef
    {
        public int ID;
        public string Name;
        public StructureType Type;
        public Structure Prefab; // null이면 소환 안 함 (보스처럼 아직 안 만든 구조물)
        public float Value;      // 구조물마다 쓰는 값 (회복 = 최대 체력 %)
        public float SpawnInterval; // 새로 추가 : 이 종류의 소환 주기(초)
        public int MaxCount;        // 새로 추가 : 맵에 이 종류가 동시에 있을 수 있는 수
        public string Description;
    }
}
// ===== 새로 추가 끝 =====
