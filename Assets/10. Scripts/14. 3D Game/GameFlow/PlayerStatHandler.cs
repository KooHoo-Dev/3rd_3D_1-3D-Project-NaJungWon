using UnityEngine;

namespace PROJECT3D
{
    public class PlayerStatHandler : MonoBehaviour
    {
        public static PlayerStatHandler Instance;
        
        public StatClass ChrStat; //캐릭터 기본 스탯
        public StatClass AbilityStat; //능력치 증강 스탯
        public StatClass ShopStat; //상점 구매 스탯
        public StatClass SkillStat; //스킬로 인한 상승치
        public StatClass BuffStat; //버프로 인한 변동 스탯
        public StatClass DeBuffStat; //디버프로 인한 변동 스탯
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        public StatClass ApplyPlayerStatChange(StatClass unitStat)
        {
            StatClass changeClass = new StatClass();

            
            return changeClass;
        }
    }
}