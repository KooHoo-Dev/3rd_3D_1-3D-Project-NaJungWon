using UnityEngine;

namespace PROJECT3D
{
    public class UnitDto
    {
        public int ID;
        public string Name;
        public string PrefabKey;
        public string SpriteKey;
        public int PassiveSkillID;
        public int ESkillID;
        public int QSkillID;
        public int WeaponID;  // 새로 추가 : 게임 시작 때 가지고 시작하는 무기증강 ID (-1 = 없음)
        public int MaxHp;
        public float HealthRegeneration;
        public int Damage;
        public float AttackSpeed;
        public int Defense;
        public float Speed;
        public float CritChance;
        public int ProjectileCount;
        public float AttackRange;
        public float Duration;
        public float AcquisitionRange;
        public float ExperienceGain;
        public float GoldGain;
    }
    
    [System.Serializable]
    public class UnitDef
    {
        public int ID;
        public string Name;
        public GameObject UnitBody; //몸 데이터. 유닛이아니라 GameObject나 meshRenderer로바꿔야할듯?
        public SkillDef PassiveSkill;
        public SkillDef ESkill;
        public SkillDef QSkill;
        public int StartWeaponID; // 새로 추가 : 시작 무기증강 ID (-1 = 없음). 증강 표가 유닛 표보다 늦게 로드돼서 ID로 들고 있다
        public Sprite Sprite; //캐릭터 아이콘
        public StatClass UnitStat;
    }
}