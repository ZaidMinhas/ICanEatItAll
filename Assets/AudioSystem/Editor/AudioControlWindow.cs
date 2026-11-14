using UnityEditor;
using UnityEngine;

namespace ICanEatItAll.Audio.Editor
{
    public sealed class AudioControlWindow : EditorWindow
    {
        [SerializeField] private SoundDefinition selectedSound;
        private UnityEditor.Editor soundEditor;
        private AudioSource preview;
        private Vector2 scroll;

        [MenuItem("Tools/Audio/Sound Control")]
        public static void Open() => GetWindow<AudioControlWindow>("Sound Control");

        private void OnEnable() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StopPreview();
            if (soundEditor != null) DestroyImmediate(soundEditor);
        }
        private void OnPlayModeChanged(PlayModeStateChange state) { StopPreview(); Repaint(); }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Sound Control", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create a Sound asset, drop in clips, then assign it to a Sound Player component. In game, use Audio [F10] for saved volume settings.", MessageType.Info);
            if (GUILayout.Button("Create Sound Asset")) CreateSound();
            selectedSound = (SoundDefinition)EditorGUILayout.ObjectField("Sound", selectedSound, typeof(SoundDefinition), false);
            if (selectedSound != null)
            {
                UnityEditor.Editor.CreateCachedEditor(selectedSound, null, ref soundEditor);
                soundEditor.OnInspectorGUI();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(Application.isPlaying ? "Play in game" : "Preview clip")) PlayPreview();
                if (GUILayout.Button("Stop"))
                {
                    StopPreview();
                    if (Application.isPlaying) AudioManager.Instance.StopCategory(selectedSound.category);
                }
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Add Sound Player to Selected Object"))
                {
                    if (Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject))
                    {
                        var player = Undo.AddComponent<SoundPlayer>(Selection.activeGameObject);
                        Undo.RecordObject(player, "Assign sound");
                        player.sound = selectedSound;
                        EditorUtility.SetDirty(player);
                    }
                    else ShowNotification(new GUIContent("Select an object in the scene first."));
                }
            }
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Live Mix", EditorStyles.boldLabel);
            if (Application.isPlaying)
            {
                var audio = AudioManager.Instance;
                float master = EditorGUILayout.Slider("Master", audio.MasterVolume, 0f, 1f);
                if (master != audio.MasterVolume) audio.SetMasterVolume(master);
                bool mute = EditorGUILayout.Toggle("Mute all", audio.MasterMuted);
                if (mute != audio.MasterMuted) audio.SetMasterMuted(mute);
                foreach (SoundCategory category in System.Enum.GetValues(typeof(SoundCategory)))
                {
                    string label = category == SoundCategory.SoundEffects ? "Sound effects" : category.ToString();
                    float next = EditorGUILayout.Slider(label, audio.GetVolume(category), 0f, 1f);
                    if (next != audio.GetVolume(category)) audio.SetVolume(category, next);
                    bool nextMuted = EditorGUILayout.Toggle("Mute " + label, audio.IsMuted(category));
                    if (nextMuted != audio.IsMuted(category)) audio.SetMuted(category, nextMuted);
                }
                if (GUILayout.Button("Reset volume defaults")) audio.ResetVolumes();
                if (GUILayout.Button("Stop all sounds")) audio.StopAll();
            }
            else EditorGUILayout.HelpBox("Enter Play mode to adjust saved category volumes. Clip preview uses the sound's own volume and pitch in 2D.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void CreateSound()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Sound", "New Sound", "asset", "Choose where to save this sound.");
            if (string.IsNullOrEmpty(path)) return;
            selectedSound = CreateInstance<SoundDefinition>();
            AssetDatabase.CreateAsset(selectedSound, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(selectedSound);
        }
        private void PlayPreview()
        {
            if (Application.isPlaying) { AudioManager.Instance.Play(selectedSound); return; }
            StopPreview();
            AudioClip clip = selectedSound.PickClip();
            if (clip == null) { ShowNotification(new GUIContent("Assign at least one audio clip.")); return; }
            var host = EditorUtility.CreateGameObjectWithHideFlags("Sound Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
            preview = host.GetComponent<AudioSource>();
            preview.playOnAwake = false;
            preview.clip = clip;
            preview.volume = selectedSound.volume;
            preview.pitch = Mathf.Clamp(selectedSound.minPitch, 0.01f, 3f);
            preview.loop = selectedSound.loop;
            preview.Play();
        }
        private void StopPreview() { if (preview != null) DestroyImmediate(preview.gameObject); }
        private void Update()
        {
            if (preview != null && !preview.isPlaying) StopPreview();
            if (Application.isPlaying) Repaint();
        }
    }
}
