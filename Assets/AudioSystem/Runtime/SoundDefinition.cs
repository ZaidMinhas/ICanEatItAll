using UnityEngine;

namespace ICanEatItAll.Audio
{
    public enum SoundCategory { Music, SoundEffects, Ambience }

    [CreateAssetMenu(menuName = "Audio/Sound", fileName = "New Sound")]
    public sealed class SoundDefinition : ScriptableObject
    {
        [Tooltip("A clip is chosen randomly each time this sound plays.")]
        public AudioClip[] clips = new AudioClip[0];
        public SoundCategory category = SoundCategory.SoundEffects;
        [Range(0f, 1f)] public float volume = 1f;
        [Min(0.01f)] public float minPitch = 1f;
        [Min(0.01f)] public float maxPitch = 1f;
        public bool loop;
        [Tooltip("Music is always 2D. Enable this for positional effects or ambience.")]
        public bool spatial;
        [Min(0.01f)] public float minDistance = 1f;
        [Min(0.01f)] public float maxDistance = 30f;

        public AudioClip PickClip()
        {
            // Ignore empty slots so partially configured assets still work.
            int count = 0;
            AudioClip chosen = null;
            if (clips == null) return null;
            foreach (AudioClip clip in clips)
                if (clip != null && Random.Range(0, ++count) == 0) chosen = clip;
            return chosen;
        }

        private void OnValidate()
        {
            minPitch = Mathf.Clamp(minPitch, 0.01f, 3f);
            maxPitch = Mathf.Clamp(maxPitch, minPitch, 3f);
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
        }
    }
}
