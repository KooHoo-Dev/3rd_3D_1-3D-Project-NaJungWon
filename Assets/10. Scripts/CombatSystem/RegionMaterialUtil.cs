using UnityEngine;

namespace PROJECT3D
{
    // 범위표시마다 자기 머티리얼 복사본을 하나씩 생성
    //  - 풀에서 처음 만들어질 때(Awake) 한 번만 부른다. 이후 DTT 코드의 sharedMaterial은 이 복사본을 가리킨다
    public static class RegionMaterialUtil
    {
        #region 외부 설정

        public static void UseOwnMaterials(Component region)
        {
            if (region == null) return;

            MeshRenderer[] renderers = region.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = new Material(renderers[i].sharedMaterial);
            }
        }

        #endregion
    }
}
