using UnityEngine;

namespace PROJECT3D
{
    public class ShopDto
    {
        public int ID;
        public string Name;
        public string SpriteKey;
        public StatType StatType; // 올려줄 스탯
        public int Level;         // 구매 단계 (1부터)
        public float Value;       // 이 단계까지의 누적 수치 (곱하기 스탯은 %, 나머지는 그 스탯 단위)
        public int Cost;          // 이 단계를 사는 비용
    }

    [System.Serializable]
    public class ShopDef
    {
        public int ID;
        public string Name;
        public Sprite Sprite;
        public StatType StatType;
        public int Level;
        public float Value;
        public int Cost;
    }
}