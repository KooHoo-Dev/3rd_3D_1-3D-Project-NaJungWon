using UnityEngine;

namespace PROJECT3D
{
    public class LocalPlayer : Player
    {
        public static LocalPlayer Instance;

        public void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    }
}