using System.Collections.Generic;
using UnityEngine;

namespace ICanEatItAll.Audio
{
    /// <summary>Persistent, bounded voice pool. All gain changes also affect sounds already playing.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public const int MaxVoices = 48;
        private const string PrefPrefix = "ICanEatItAll.Audio.";
        private static AudioManager instance;
        private static bool quitting;
        private readonly List<Voice> voices = new List<Voice>();
        private readonly float[] levels = { 0.8f, 1f, 0.8f };
        private readonly bool[] muted = new bool[3];
        private float master = 1f;
        private bool masterMuted;
        private int nextHandle;
        private bool dirty;
        private float saveAt;

        private sealed class Voice
        {
            public AudioSource source;
            public SoundDefinition sound;
            public SoundCategory category;
            public Transform follow;
            public bool following;
            public float gain;
            public int handle;
        }

        public static AudioManager Instance
        {
            get
            {
                if (instance == null && !quitting)
                    instance = new GameObject("Audio System").AddComponent<AudioManager>();
                return instance;
            }
        }

        public float MasterVolume => master;
        public bool MasterMuted => masterMuted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; quitting = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() { _ = Instance; }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            master = ReadLevel("Master", 1f);
            masterMuted = PlayerPrefs.GetInt(PrefPrefix + "MasterMuted", 0) != 0;
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = ReadLevel(((SoundCategory)i).ToString(), levels[i]);
                muted[i] = PlayerPrefs.GetInt(PrefPrefix + "Muted" + i, 0) != 0;
            }
            if (GetComponent<AudioSettingsPanel>() == null) gameObject.AddComponent<AudioSettingsPanel>();
        }

        private static float ReadLevel(string key, float fallback) =>
            Mathf.Clamp01(PlayerPrefs.GetFloat(PrefPrefix + key, fallback));

        public float GetVolume(SoundCategory category) => levels[(int)category];
        public bool IsMuted(SoundCategory category) => muted[(int)category];
        public void SetMasterVolume(float value) { master = Mathf.Clamp01(value); Changed(); }
        public void SetMasterMuted(bool value) { masterMuted = value; Changed(); }
        public void SetVolume(SoundCategory category, float value) { levels[(int)category] = Mathf.Clamp01(value); Changed(); }
        public void SetMuted(SoundCategory category, bool value) { muted[(int)category] = value; Changed(); }

        public void ResetVolumes()
        {
            master = 1f;
            masterMuted = false;
            levels[0] = 0.8f; levels[1] = 1f; levels[2] = 0.8f;
            for (int i = 0; i < muted.Length; i++) muted[i] = false;
            Changed();
        }

        private void Changed()
        {
            foreach (Voice voice in voices) if (voice.handle != 0) ApplyVolume(voice);
            dirty = true;
            saveAt = Time.realtimeSinceStartup + 0.5f;
        }

        /// <returns>A playback handle, or zero if no clip/voice is available. Keep the handle to stop this playback.</returns>
        public int Play(SoundDefinition sound, Vector3 position = default, Transform follow = null)
        {
            if (sound == null) return 0;
            if (sound.category == SoundCategory.Music)
                foreach (Voice current in voices)
                    if (current.sound == sound && current.handle != 0 && (current.source.isPlaying || AudioListener.pause)) return current.handle;
            AudioClip clip = sound.PickClip();
            if (clip == null) { Debug.LogWarning("No clips assigned to sound: " + sound.name, sound); return 0; }
            return PlayInternal(sound, clip, sound.category, sound.volume,
                Random.Range(Mathf.Clamp(sound.minPitch, 0.01f, 3f), Mathf.Clamp(sound.maxPitch, Mathf.Clamp(sound.minPitch, 0.01f, 3f), 3f)),
                sound.loop, sound.spatial, sound.minDistance, sound.maxDistance, position, follow);
        }

        /// <summary>Convenience for existing clip-based gameplay code. Uses the same pool and volume controls.</summary>
        public int PlayClip(AudioClip clip, Vector3 position, float volume = 1f,
            SoundCategory category = SoundCategory.SoundEffects, bool spatial = true)
        {
            if (clip == null) return 0;
            return PlayInternal(null, clip, category, volume, 1f, false, spatial, 1f, 30f, position, null);
        }

        private int PlayInternal(SoundDefinition sound, AudioClip clip, SoundCategory category, float volume,
            float pitch, bool loop, bool spatial, float minDistance, float maxDistance, Vector3 position, Transform follow)
        {
            // A music asset represents the current soundtrack; repeated requests do not restart it.
            if (category == SoundCategory.Music)
            {
                foreach (Voice current in voices)
                    if (sound == null && current.sound == null && current.handle != 0 && current.category == category &&
                        current.source.clip == clip && (current.source.isPlaying || AudioListener.pause)) return current.handle;
                StopCategory(SoundCategory.Music);
            }
            Voice voice = voices.Find(v => v.handle == 0);
            if (voice == null && voices.Count < MaxVoices)
            {
                var child = new GameObject("Sound Voice");
                child.transform.SetParent(transform);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                voice = new Voice { source = source };
                voices.Add(voice);
            }
            // Recycle the oldest one-shot at capacity; never silently steal a looping bed.
            if (voice == null) voice = voices.Find(v => !v.source.loop);
            if (voice == null) { Debug.LogWarning("Audio voice limit reached; stop a loop before adding another.", sound); return 0; }
            voice.source.Stop();
            voice.sound = sound;
            voice.category = category;
            voice.follow = follow;
            voice.following = follow != null;
            voice.gain = Mathf.Clamp01(volume);
            voice.handle = ++nextHandle;
            voice.source.clip = clip;
            voice.source.pitch = pitch;
            voice.source.loop = loop;
            voice.source.spatialBlend = spatial && category != SoundCategory.Music ? 1f : 0f;
            voice.source.minDistance = Mathf.Max(0.01f, minDistance);
            voice.source.maxDistance = Mathf.Max(voice.source.minDistance, maxDistance);
            voice.source.rolloffMode = AudioRolloffMode.Logarithmic;
            voice.source.dopplerLevel = 0f;
            voice.source.transform.position = follow != null ? follow.position : position;
            ApplyVolume(voice);
            voice.source.Play();
            // Move to the end so stealing follows playback age, even after source reuse.
            voices.Remove(voice);
            voices.Add(voice);
            return voice.handle;
        }

        public bool IsPlaying(int handle) => handle != 0 && voices.Exists(v => v.handle == handle && (v.source.isPlaying || AudioListener.pause));
        public void Stop(int handle)
        {
            foreach (Voice voice in voices) if (voice.handle == handle) Release(voice);
        }
        public void StopCategory(SoundCategory category)
        {
            foreach (Voice voice in voices) if (voice.handle != 0 && voice.category == category) Release(voice);
        }
        public void StopAll() { foreach (Voice voice in voices) Release(voice); }

        private static void Release(Voice voice)
        {
            voice.source.Stop(); voice.source.clip = null; voice.sound = null;
            voice.follow = null; voice.following = false; voice.handle = 0;
        }
        private void ApplyVolume(Voice voice)
        {
            int category = (int)voice.category;
            voice.source.volume = masterMuted || muted[category] ? 0f : master * levels[category] * voice.gain;
        }
        private void Update()
        {
            foreach (Voice voice in voices)
            {
                if (voice.handle == 0) continue;
                if (voice.following && voice.follow == null) { Release(voice); continue; }
                if (voice.follow != null) voice.source.transform.position = voice.follow.position;
                // Listener pause makes isPlaying false; keep those voices until it resumes.
                if (!AudioListener.pause && !voice.source.isPlaying) Release(voice);
            }
            if (dirty && Time.realtimeSinceStartup >= saveAt) SaveSettings();
        }
        public void SaveSettings()
        {
            if (!dirty) return;
            PlayerPrefs.SetFloat(PrefPrefix + "Master", master);
            PlayerPrefs.SetInt(PrefPrefix + "MasterMuted", masterMuted ? 1 : 0);
            for (int i = 0; i < levels.Length; i++)
            {
                PlayerPrefs.SetFloat(PrefPrefix + (SoundCategory)i, levels[i]);
                PlayerPrefs.SetInt(PrefPrefix + "Muted" + i, muted[i] ? 1 : 0);
            }
            PlayerPrefs.Save(); dirty = false;
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveSettings(); }
        private void OnApplicationQuit() { quitting = true; SaveSettings(); }
        private void OnDestroy() { if (instance == this) { SaveSettings(); instance = null; } }
    }
}
