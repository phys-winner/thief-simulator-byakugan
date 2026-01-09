using UnityEngine;

namespace ThiefSimulatorHack
{
    public class Loader
    {
        private static GameObject _load;

        public static void Init()
        {
            _load = new GameObject();
            _load.AddComponent<Main>();
            Object.DontDestroyOnLoad(_load);
        }

        public static void Unload()
        {
            Object.Destroy(_load);
        }
    }
}
