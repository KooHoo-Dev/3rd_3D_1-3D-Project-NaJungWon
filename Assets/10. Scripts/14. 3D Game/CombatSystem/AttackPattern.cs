using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Study.MiniDefence
{
    public interface IAttackPattern
    {
        void Fire();
    }

    // 고른 대상 하나만 때린다.
    public class SingleAttack : IAttackPattern
    {
        public void Fire()
        {
            
        }
    }

    
    public class SplashAttack : IAttackPattern
    {
        public void Fire()
        {
            
        }
    }
    
    
    // [Factory] 표에 적힌 UnitKind를 실제 공격 방식으로 바꿔 준다
    /*
    public static class AttackPatternFactory
    {
        public static IAttackPattern Create(UnitKind kind)
        {
            return kind switch
            {
                UnitKind.Cannon => new SplashAttack(),
                _ => new SingleAttack(), // 캐논 빼고는 전부다 단일 대상 
            };
        }
    }
    */
}