using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ICanEatItAll.Audio.Tests
{
    public sealed class AudioSystemTests
    {
        private AudioManager audio;
        private AudioClip clip;
        private readonly List<Object> objects = new List<Object>();
        private readonly float[] previousVolumes = new float[3];
        private readonly bool[] previousMutes = new bool[3];
        private float previousMaster;
        private bool previousMasterMuted;

        [SetUp]
        public void SetUp()
        {
            audio = AudioManager.Instance;
            audio.StopAll();
            previousMaster = audio.MasterVolume;
            previousMasterMuted = audio.MasterMuted;
            for (int i = 0; i < 3; i++)
            {
                previousVolumes[i] = audio.GetVolume((SoundCategory)i);
                previousMutes[i] = audio.IsMuted((SoundCategory)i);
            }
            audio.ResetVolumes();
            clip = AudioClip.Create("Test clip", 441000, 1, 44100, false);
            objects.Add(clip);
        }

        private SoundDefinition Sound(SoundCategory category, bool loop = true)
        {
            var sound = ScriptableObject.CreateInstance<SoundDefinition>();
            sound.clips = new[] { clip };
            sound.category = category;
            sound.loop = loop;
            objects.Add(sound);
            return sound;
        }

        [TearDown]
        public void TearDown()
        {
            audio.GetComponent<AudioSettingsPanel>().Hide();
            audio.StopAll();
            audio.SetMasterVolume(previousMaster);
            audio.SetMasterMuted(previousMasterMuted);
            for (int i = 0; i < 3; i++)
            {
                audio.SetVolume((SoundCategory)i, previousVolumes[i]);
                audio.SetMuted((SoundCategory)i, previousMutes[i]);
            }
            audio.SaveSettings();
            foreach (Object item in objects) if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
        }

        [Test]
        public void LiveMixSeparatesCategoriesAndRestoresMutedGain()
        {
            var music = Sound(SoundCategory.Music);
            music.volume = 0.5f;
            music.clips = new[] { AudioClip.Create("Music", 441000, 1, 44100, false) };
            objects.Add(music.clips[0]);
            audio.Play(music);
            var ambience = Sound(SoundCategory.Ambience);
            ambience.clips = new[] { AudioClip.Create("Ambience", 441000, 1, 44100, false) };
            objects.Add(ambience.clips[0]);
            audio.Play(ambience);
            audio.PlayClip(clip, Vector3.zero, 0.6f);
            audio.SetMasterVolume(0.5f);
            audio.SetVolume(SoundCategory.Music, 0.4f);
            audio.SetVolume(SoundCategory.Ambience, 0.2f);
            var allSources = audio.GetComponentsInChildren<AudioSource>();
            var sources = new[] {
                System.Array.Find(allSources, source => source.clip == music.clips[0]),
                System.Array.Find(allSources, source => source.clip == ambience.clips[0]),
                System.Array.Find(allSources, source => source.clip == clip)
            };
            Assert.That(sources[0].volume, Is.EqualTo(0.1f).Within(0.001));
            Assert.That(sources[1].volume, Is.EqualTo(0.1f).Within(0.001));
            Assert.That(sources[2].volume, Is.EqualTo(0.3f).Within(0.001));
            audio.SetMuted(SoundCategory.Music, true);
            Assert.That(sources[0].volume, Is.Zero);
            Assert.That(sources[2].volume, Is.EqualTo(0.3f).Within(0.001));
            audio.SetMuted(SoundCategory.Music, false);
            Assert.That(sources[0].volume, Is.EqualTo(0.1f).Within(0.001));
            audio.SetMasterMuted(true);
            foreach (AudioSource source in sources) Assert.That(source.volume, Is.Zero);
        }

        [Test]
        public void RepeatedMusicKeepsHandleAndNewTrackReplacesOnlyMusic()
        {
            var music = Sound(SoundCategory.Music);
            int first = audio.Play(music);
            Assert.That(audio.Play(music), Is.EqualTo(first));
            int ambience = audio.Play(Sound(SoundCategory.Ambience));
            var other = Sound(SoundCategory.Music);
            other.clips = new[] { AudioClip.Create("Other", 441000, 1, 44100, false) };
            objects.Add(other.clips[0]);
            audio.Play(other);
            Assert.That(audio.IsPlaying(first), Is.False);
            Assert.That(audio.IsPlaying(ambience), Is.True);
        }

        [UnityTest]
        public IEnumerator FollowedSoundMovesAndStopsWhenOwnerIsDestroyed()
        {
            var owner = new GameObject("Emitter");
            objects.Add(owner);
            var sound = Sound(SoundCategory.Ambience);
            sound.spatial = true;
            int handle = audio.Play(sound, follow: owner.transform);
            owner.transform.position = new Vector3(3, 4, 5);
            yield return null;
            AudioSource source = System.Array.Find(audio.GetComponentsInChildren<AudioSource>(), s => s.isPlaying);
            Assert.That(source.transform.position, Is.EqualTo(owner.transform.position));
            Object.Destroy(owner);
            yield return null;
            yield return null;
            Assert.That(audio.IsPlaying(handle), Is.False);
        }

        [Test]
        public void PoolIsBoundedAndNeverStealsLoops()
        {
            var loop = Sound(SoundCategory.Ambience);
            for (int i = 0; i < AudioManager.MaxVoices; i++) Assert.That(audio.Play(loop), Is.Not.Zero);
            LogAssert.Expect(LogType.Warning, "Audio voice limit reached; stop a loop before adding another.");
            Assert.That(audio.Play(loop), Is.Zero);
            Assert.That(audio.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(AudioManager.MaxVoices));
        }

        [Test]
        public void OneShotStealingInvalidatesOldHandle()
        {
            int oldest = audio.PlayClip(clip, Vector3.zero);
            for (int i = 1; i < AudioManager.MaxVoices; i++) audio.PlayClip(clip, Vector3.zero);
            int newest = audio.PlayClip(clip, Vector3.zero);
            Assert.That(audio.IsPlaying(oldest), Is.False);
            audio.Stop(oldest);
            Assert.That(audio.IsPlaying(newest), Is.True);
        }

        [Test]
        public void MenuRestoresCursorAndExistingTimeScale()
        {
            var panel = audio.GetComponent<AudioSettingsPanel>();
            float before = Time.timeScale;
            var cursorLock = Cursor.lockState;
            bool visible = Cursor.visible;
            try
            {
                Time.timeScale = 0.5f;
                panel.Show(); panel.Show();
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(Cursor.visible, Is.True);
                panel.Hide(); panel.Hide();
                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
                Assert.That(Cursor.lockState, Is.EqualTo(cursorLock));
                Assert.That(Cursor.visible, Is.EqualTo(visible));
            }
            finally { Time.timeScale = before; }
        }

        [Test]
        public void SettingsSurviveManagerRecreation()
        {
            audio.SetMasterVolume(0.35f);
            audio.SetVolume(SoundCategory.Ambience, 0.27f);
            audio.SetMuted(SoundCategory.SoundEffects, true);
            audio.SaveSettings();
            Object.DestroyImmediate(audio.gameObject);
            audio = AudioManager.Instance;
            Assert.That(audio.MasterVolume, Is.EqualTo(0.35f).Within(0.001));
            Assert.That(audio.GetVolume(SoundCategory.Ambience), Is.EqualTo(0.27f).Within(0.001));
            Assert.That(audio.IsMuted(SoundCategory.SoundEffects), Is.True);
        }
    }
}
