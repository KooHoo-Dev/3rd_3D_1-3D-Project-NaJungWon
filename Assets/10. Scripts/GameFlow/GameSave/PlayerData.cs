namespace PROJECT3D
{
    // 게임을 꺼도 남아야 하는 내 데이터 (SaveService가 JSON으로 저장/불러오기)
    [System.Serializable]
    public class PlayerData
    {
        public int Gold;            // 보유 골드 (로비 상점에서 사용)
        public int SelectedUnitID;  // 마지막으로 고른 캐릭터

        // 스탯별 상점 구매 단계 (0 = 안 삼). 칸 번호 = StatType 번호
        public int[] ShopLevels = new int[(int)StatType.Count];

        public int GetShopLevel(StatType type)
        {
            return ShopLevels[(int)type];
        }
    }
}