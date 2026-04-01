using UnityEngine;

namespace ThiefSimulatorHack
{
    public class Loader
    {
        private static GameObject _load;

        public static void Init()
        {
            if (_load != null) return;

            _load = new GameObject();
            _load.AddComponent<Main>();
            Object.DontDestroyOnLoad(_load);
        }

        public static void Unload()
        {
            if (_load != null)
            {
                Object.Destroy(_load);
                _load = null;
            }
        }
    }
}
