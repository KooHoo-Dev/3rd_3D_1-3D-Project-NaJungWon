using System;
using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    public class ObjectPoolManager : MonoBehaviour
    {
        // 초기화를 위해 ObjectPoolManager에 입력하는 데이터가 됩니다.
        // 런타임에는 해당 데이터 클래스를 사용하지 않습니다.
        [Serializable]
        public class ObjectPoolData
        {
            [SerializeField] private string key;
            [SerializeField] private GameObject prefab;
            [SerializeField] private byte expandSize;
            [SerializeField] private Transform parent;

            public string Key => key;
            public GameObject Prefab => prefab;
            public byte ExpandSize => expandSize;
            public Transform Parent => parent;
        }

        private class ObjectPool
        {
            private readonly Stack<IObjectPoolItem> pool = new Stack<IObjectPoolItem>();
            private readonly GameObject prefab;
            private readonly Transform parent;
            private readonly int expandSize;
            private readonly string key;

            public ObjectPool(GameObject prefab, Transform parent, int expandSize, string key)
            {
                this.prefab = prefab;
                this.parent = parent;
                this.expandSize = Mathf.Max(1, expandSize);
                this.key = key;
                Expand();
            }

            private void Expand()
            {
                for (int i = 0; i < expandSize; i++)
                {
                    var instance = Instantiate(prefab, parent).GetComponent<IObjectPoolItem>();
                    instance.Key = key;
                    Return(instance);
                }
            }

            public IObjectPoolItem Get()
            {
                if (pool.Count == 0) Expand();
                return pool.Pop();
            }

            public void Return(IObjectPoolItem item)
            {
                item.GameObject.SetActive(false);
                pool.Push(item);
            }
        }

        public static ObjectPoolManager Instance { get; private set; }

        [Header("만들 풀 목록")]
        [SerializeField] private ObjectPoolData[] objectPoolDatas;

        [Header("자동 생성 풀")]
        [SerializeField] private byte defaultExpandSize = 10; // 프리팹으로 처음 꺼낼 때 한 번에 만드는 수

        private readonly Dictionary<string, ObjectPool> objectPoolDic = new Dictionary<string, ObjectPool>();

        #region UnityMethod

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{name}: ObjectPoolManager가 이미 있습니다. 이 컴포넌트는 쓰지 않습니다");
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            foreach (var data in objectPoolDatas)
            {
                CreatePool(data);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region 외부 설정

        #region 풀 생성

        // ObjectPoolData를 이용해서 ObjectPool 한 단위를 만들고
        // 내부 딕셔너리에 등록합니다
        public void CreatePool(ObjectPoolData data)
        {
            if (objectPoolDic.ContainsKey(data.Key))
            {
                Debug.LogWarning($"{data.Key}값이 Pool에 이미 존재합니다.");
                return;
            }

            if (data.Prefab.GetComponent<IObjectPoolItem>() == null)
            {
                Debug.LogWarning($"{data.Prefab.name}의 프리펩에 IObjectPoolItem을" +
                                 $"상속받은 컴포넌트가 없습니다.");
                return;
            }

            // 부모를 따로 지정하지 않았으면 "프리팹이름.Pool" 오브젝트를 만들어 그 아래에 모으기 (편의성 용)
            Transform poolParent = data.Parent;
            if (poolParent == null) poolParent = CreatePoolParent(data.Prefab.name);
            objectPoolDic.Add(data.Key, new ObjectPool(data.Prefab, poolParent, data.ExpandSize, data.Key));
        }

        #endregion

        #region 꺼내기

        public IObjectPoolItem GetObjectOrNull(string key)
        {
            if (objectPoolDic.TryGetValue(key, out var pool)) return pool.Get();

            Debug.LogWarning($"{key}값의 Pool이 존재하지 않습니다");
            return null;
        }

        public IObjectPoolItem GetObjectOrNull(GameObject prefab)
        {
            if (prefab == null) return null;

            string key = prefab.name;
            if (objectPoolDic.ContainsKey(key) == false)
            {
                if (prefab.GetComponent<IObjectPoolItem>() == null)
                {
                    Debug.LogWarning($"{prefab.name}의 프리펩에 IObjectPoolItem을 상속받은 컴포넌트가 없습니다.");
                    return null;
                }

                Transform poolParent = CreatePoolParent(key);
                objectPoolDic.Add(key, new ObjectPool(prefab, poolParent, defaultExpandSize, key));
            }

            return objectPoolDic[key].Get();
        }

        #endregion

        #region 반납

        // 아이템을 입력받아서 해당 아이템을 관리하는 풀에 집어넣는 함수
        public void ReturnToPool(IObjectPoolItem item)
        {
            if (objectPoolDic.TryGetValue(item.Key, out var pool) == false)
            {
                Debug.LogWarning($"{item.Key}값의 Pool이 존재하지 않습니다");
                return;
            }

            pool.Return(item);
        }

        #endregion

        #region 이펙트

        // 이펙트 재생
        public static void PlayEffect(ParticleEffect prefab, Vector3 position)
        {
            if (prefab == null) return; // 이펙트를 안 넣었으면 생략
            IObjectPoolItem effect = Instance.GetObjectOrNull(prefab.gameObject);
            if (effect == null) return;

            effect.GameObject.transform.position = position;
            effect.GameObject.SetActive(true);
        }

        #endregion

        #endregion

        #region 내부 설정

        private Transform CreatePoolParent(string objectName)
        {
            GameObject poolParent = new GameObject($"{objectName}.Pool");
            poolParent.transform.SetParent(transform, false);
            return poolParent.transform;
        }

        #endregion
    }
}