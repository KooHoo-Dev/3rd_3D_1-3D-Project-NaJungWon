using System.Collections;
using UnityEngine;

namespace PROJECT3D
{
    // 생성·파괴되는 장애물에 붙인다. 지정 영역은 월드 축 기준 상자다.
    public class GridObstacle : MonoBehaviour
    {
        public Vector3 areaCenter = Vector3.zero;
        public Vector3 areaSize = new Vector3(8f, 5f, 8f);
        public float riseTime = 1.5f;
        private MapGrid grid;
        private Bounds Area => new Bounds(transform.position + areaCenter, areaSize);

        #region UnityMethod

        private IEnumerator Start()
        {
            grid = MapGrid.Instance;
            if (grid == null)
            {
                Debug.LogError("격자맵이 없어 장애물을 반영하지 못했습니다.", this);
                yield break;
            }
            grid.BlockArea(Area);
            if (riseTime > 0f) yield return new WaitForSeconds(riseTime);
            grid.RebakeArea(Area);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Area.center, Area.size);
        }

        #endregion

        #region 외부 설정

        // 파괴 시 Destroy 대신 호출한다. 솟아오르기 대기 중인 검사도 취소한다.
        public void Remove()
        {
            StopAllCoroutines();
            foreach (Collider obstacleCollider in GetComponentsInChildren<Collider>())
                obstacleCollider.enabled = false;
            if (grid == null) grid = MapGrid.Instance;
            if (grid != null) grid.RebakeArea(Area);
            Destroy(gameObject);
        }

        #endregion
    }
}