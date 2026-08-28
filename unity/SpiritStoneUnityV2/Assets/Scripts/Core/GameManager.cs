using UnityEngine;

namespace SpiritStone.Core
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarningFormat("[GameManager] Duplicate instance on {0} was removed.", name);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            Debug.LogFormat("[GameManager] Pause state changed: {0}", isPaused);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}

