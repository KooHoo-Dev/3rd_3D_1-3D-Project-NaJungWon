using UnityEngine;

namespace PROJECT3D
{
    public class LevelDto
    {
        public int Level;       // 현재 레벨
        public int RequiredExp; // 이 레벨에서 다음 레벨로 가는 데 필요한 경험치
    }

    [System.Serializable]
    public class LevelDef
    {
        public int Level;
        public int RequiredExp;
    }
}