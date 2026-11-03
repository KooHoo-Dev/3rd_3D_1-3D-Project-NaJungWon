using UnityEngine;

// ===== 새로 추가 : 파일 전체 (몹 피격 데미지 숫자. 3rd-3000 FPS 프로젝트 HitEffectController 기반) =====
namespace PROJECT3D
{
    // 몹이 맞을 때마다 머리 위에 데미지 숫자를 띄운다
    //  - CombatSystem.OnCombatEvent(실제로 데미지가 들어간 뒤 호출)를 구독. 공격 코드는 손대지 않는다
    //  - 위치 = 몹의 HeadUpPivot (없으면 피격 원통 꼭대기) // [변경]
    //  - 색 : 일반 주황 / 치명타 빨강 // [변경]
    //  - 인게임 UI 캔버스(Screen Space)에 이 컴포넌트를 붙인다. 숫자는 이 캔버스 밑에 생긴다
    [RequireComponent(typeof(Canvas))]
    public class DamageTextSpawner : MonoBehaviour
    {
        [SerializeField] private DamageText damageTextPrefab; // TMP 글자 하나짜리 UI 프리팹
        [SerializeField] private float duration = 0.6f;       // 떠 있는 시간
        [SerializeField] private Color normalColor = new Color(1f, 0.5f, 0f); // [변경] 일반 데미지 : 주황
        [SerializeField] private Color criticalColor = Color.red;              // 새로 추가 : 치명타 : 빨강

        private Canvas canvas;
        private bool isSubscribed;

        #region UnityMethod

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void Start()
        {
            if (CombatSystem.Instance == null) return;
            CombatSystem.Instance.OnCombatEvent += OnHit;
            isSubscribed = true;
        }

        private void OnDestroy()
        {
            if (isSubscribed && CombatSystem.Instance != null)
                CombatSystem.Instance.OnCombatEvent -= OnHit;
        }

        #endregion

        #region 내부 설정

        private void OnHit(CombatEvent combatEvent)
        {
            if (damageTextPrefab == null) return;
            Enemy enemy = combatEvent.Receiver as Enemy;
            if (enemy == null) return; // 몹이 맞을 때만 (플레이어 피격은 HP바로 본다)

            DamageText damageText = ObjectPoolManager.Instance.GetObjectOrNull(damageTextPrefab.gameObject) as DamageText;
            if (damageText == null) return;

            // 풀은 ObjectPoolManager 밑에 만들어지니까, 처음 한 번 캔버스 밑으로 옮긴다 (반납돼도 캔버스 밑에 그대로 남음)
            if (damageText.transform.parent != transform) damageText.transform.SetParent(transform, false);

            // [변경] 몹마다 정해둔 HeadUpPivot 위치 (HP바 등과 같은 기준). 안 넣은 몹은 피격 원통 꼭대기
            Vector3 headPosition = enemy.HitCenter + Vector3.up * enemy.HurtHalfHeight;
            if (enemy.HeadUpPivot != null) headPosition = enemy.HeadUpPivot.position;

            Color color = combatEvent.IsCritical ? criticalColor : normalColor; // 새로 추가
            damageText.Set(canvas, headPosition, combatEvent.Damage.ToString(), duration, color);
            damageText.gameObject.SetActive(true);
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
