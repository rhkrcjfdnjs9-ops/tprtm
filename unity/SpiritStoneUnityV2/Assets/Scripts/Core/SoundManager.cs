using UnityEngine;

namespace SpiritStone.Core
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource effectsSource;

        public static SoundManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarningFormat("[SoundManager] Duplicate instance on {0} was removed.", name);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null || effectsSource == null)
                Debug.LogErrorFormat("[SoundManager] Music and effects AudioSource references must be assigned.");
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource == null || clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void PlayEffect(AudioClip clip, float volume = 1f)
        {
            if (effectsSource == null || clip == null) return;
            effectsSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
