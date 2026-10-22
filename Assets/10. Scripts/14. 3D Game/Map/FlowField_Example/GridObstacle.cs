using System.Collections;
using UnityEngine;

// 런타임에 생기거나 사라지는 지형에 붙임 (보스 제단, 파괴 가능한 바위 등)
public class GridObstacle : MonoBehaviour
{
    [Tooltip("그리드에 영향을 주는 영역 (기획자가 씬에서 노란 박스로 확인)")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(8, 5, 8);
    [Tooltip("솟아오르는 연출 시간. 0이면 즉시 반영")]
    public float riseTime = 1.5f;

    Bounds Area => new Bounds(transform.position + areaCenter, areaSize);

    IEnumerator Start()
    {
        var grid = MapGrid.Instance;
        grid.BlockArea(Area);                       // 즉시 막기
        if (riseTime > 0f) yield return new WaitForSeconds(riseTime);
        grid.RebakeArea(Area);                      // 최종 모양 반영
    }

    // 파괴할 때는 Destroy 대신 이 함수 호출
    public void Remove()
    {
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        MapGrid.Instance.RebakeArea(Area);          // Collider를 끈 상태로 다시 베이크 → 길이 열림
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Area.center, Area.size);
    }
}