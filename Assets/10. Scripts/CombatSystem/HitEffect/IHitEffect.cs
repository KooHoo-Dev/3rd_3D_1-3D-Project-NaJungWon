namespace PROJECT3D
{
    // 맞은 대상에게 거는 부가 효과 (스턴, 나중에 둔화 / 화상 / 넉백 등)
    //  - HitUnit 프리팹에 이 인터페이스를 구현한 컴포넌트를 붙이면, HitUnit이 맞힐 때마다 Apply를 부른다
    //  - HitUnit(부모)은 효과 종류를 몰라도 된다 → 새 효과는 컴포넌트만 새로 만들어 붙이면 된다
    public interface IHitEffect
    {
        // 풀에서 다시 꺼낼 때 (HitUnit.Launch) 지난번 값 지우기
        void ResetEffect();

        // 대상에게 효과 적용 (HitUnit.Hit에서 데미지 뒤에)
        void Apply(CombatEntity target);
    }
}