using UnityEngine;

// ===== 새로 추가 : 파일 전체 (패시브 스킬 부모) =====
namespace PROJECT3D
{
    // 패시브 스킬의 부모 클래스
    //  - 키 입력 / 쿨타임 없이 항상 켜져 있는 효과 (E / Q 스킬 UnitSkill과 따로)
    //  - 스킬 표(skills.tsv)의 PrefabKey 프리팹에 붙이고, 캐릭터 표의 PassiveSkillID로 연결
    //  - PlayerController.Init이 만들어서 Init(주인, 표 데이터)을 부른다
    public abstract class PassiveSkill : MonoBehaviour
    {
        protected Player Owner { get; private set; }
        protected SkillDef Def { get; private set; }

        #region 외부 설정

        public void Init(Player owner, SkillDef def)
        {
            Owner = owner;
            Def = def;
            OnInit();
        }

        // 자식이 이벤트 연결 등 시작 처리
        protected abstract void OnInit();

        #endregion
    }
}
// ===== 새로 추가 끝 =====
