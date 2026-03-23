using UnityEngine;

namespace ThiefSimulatorHack
{
    public class Loader
    {
        private static GameObject _load;

        public static void Init()
        {
            if (_load != null) return; // Prevent multiple initializations

            _load = new GameObject();
            _load.AddComponent<Main>();
            Object.DontDestroyOnLoad(_load);
        }

        public static void Unload()
        {
            Object.Destroy(_load);
            _load = null;
        }
    }
}
