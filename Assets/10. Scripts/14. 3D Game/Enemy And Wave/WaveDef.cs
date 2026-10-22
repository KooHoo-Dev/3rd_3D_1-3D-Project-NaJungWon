using UnityEngine;

namespace PROJECT3D
{
    public class WaveDto
    {
        public int ID;
        public float SpawnTimer;
        public int EnemyID;
        public int Count;
    }
    
    [System.Serializable]
    public class WaveDef
    {
        public int ID;
        public float SpawnTimer;
        public EnemyDef EnemyDef;
        public int Count;
    }
}