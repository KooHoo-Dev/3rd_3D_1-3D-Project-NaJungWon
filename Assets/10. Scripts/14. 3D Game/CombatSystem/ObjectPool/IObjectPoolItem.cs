using UnityEngine;

namespace PROJECT3D
{
    // 오브젝트 풀에서 사용할 최소단위 인터페이스 입니다
    // - 몹, 투사체, 이펙트에 사용
    // - 인터페이스를 상속받아서 Key와 GameObject 프로퍼티를 재정의하여 사용합니다.
    // - 풀로 귀환하는 함수 (ReturnToPool)을 상속받은 클래스의
    //  기능에 따라 재정의하여 사용합니다.
    
    public interface IObjectPoolItem
    {
        string Key { get; set; }
        GameObject GameObject { get; }

        void ReturnToPool();
    }
}
