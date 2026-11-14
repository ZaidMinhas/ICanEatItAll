using System.Collections.Generic;
using UnityEngine;

namespace ICanEatItAll.Audio
{
    [AddComponentMenu("Audio/Sound Player")]
    public sealed class SoundPlayer : MonoBehaviour
    {
        public SoundDefinition sound;
        public bool playOnEnable;
        [Tooltip("Stops this object's sounds when it is disabled. Turn off for sounds that must outlive it.")]
        public bool stopOnDisable = true;
        public bool followObject = true;
        private readonly List<int> handles = new List<int>();

        private void OnEnable() { if (playOnEnable) Play(); }
        // Parameterless methods work directly with buttons, UnityEvents and animation events.
        public void Play()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            handles.RemoveAll(handle => !audio.IsPlaying(handle));
            if (sound != null && sound.loop && handles.Count > 0) return;
            int next = audio.Play(sound, transform.position, followObject ? transform : null);
            if (next != 0 && !handles.Contains(next)) handles.Add(next);
        }
        public void Stop()
        {
            if (handles.Count == 0) return;
            var audio = AudioManager.Instance;
            if (audio != null) foreach (int handle in handles) audio.Stop(handle);
            handles.Clear();
        }
        private void OnDisable() { if (stopOnDisable) Stop(); }
    }
}
