namespace PROJECT3D
{
    // 상시 소환 데이터
    public class SpawnRuleDto
    {
        public int ID;
        public float StartTime;
        public float EndTime;
        public int EnemyID;
        public float Interval;
        public int Count;
        public int MinAlive;
    }

    [System.Serializable]
    public class SpawnRuleDef
    {
        public int ID;
        public float StartTime;   // 이 시간(초)부터 소환 시작
        public float EndTime;     // 이 시간(초)부터 소환 끝 (아주 크게 두면 끝까지)
        public EnemyDef EnemyDef; // 소환할 몹
        public float Interval;    // 소환 주기(초)
        public int Count;         // 한 번에 소환할 수
        public int MinAlive;      // 맵 전체 몹이 이 수보다 적으면 바로 채운다 (0이면 안 채움)
    }
}
