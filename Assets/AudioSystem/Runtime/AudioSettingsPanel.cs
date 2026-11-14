using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ICanEatItAll.Audio
{
    /// <summary>Automatic runtime volume menu; Show/Hide can also be wired to your own UI.</summary>
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        private CursorLockMode previousLock;
        private bool previousVisible;
        private float previousTimeScale;
#if ENABLE_INPUT_SYSTEM
        private readonly List<PlayerInput> suspendedPlayers = new List<PlayerInput>();
#endif

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) Toggle();
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Hide();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F10)) Toggle();
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Hide();
#endif
        }

        public void Toggle() { if (IsOpen) Hide(); else Show(); }
        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
#if ENABLE_INPUT_SYSTEM
            suspendedPlayers.Clear();
            foreach (PlayerInput player in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
            {
                if (!player.inputIsActive) continue;
                suspendedPlayers.Add(player);
                player.DeactivateInput();
            }
#endif
            UnlockCursor();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
#if ENABLE_INPUT_SYSTEM
            foreach (PlayerInput player in suspendedPlayers) if (player != null && player.isActiveAndEnabled) player.ActivateInput();
            suspendedPlayers.Clear();
#endif
            AudioManager.Instance?.SaveSettings();
        }

        private static void UnlockCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void LateUpdate() { if (IsOpen) UnlockCursor(); }
        private void OnDisable() { Hide(); }

        private void OnGUI()
        {
            // A small always-visible entry point also works on touch screens.
            if (!IsOpen)
            {
                if (GUI.Button(new Rect(Screen.width - 146, 12, 134, 32), "Audio [F10]")) Show();
                return;
            }
            float scale = Mathf.Min(1.5f, Mathf.Min(Screen.width / 480f, Screen.height / 390f));
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.Box(new Rect(0, 0, width, height), GUIContent.none);
            GUILayout.BeginArea(new Rect((width - 440) / 2, (height - 350) / 2, 440, 350), GUI.skin.window);
            GUILayout.Label("AUDIO SETTINGS", new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter });
            GUILayout.Label("Changes apply immediately and save automatically.");
            GUILayout.Space(14);
            var audio = AudioManager.Instance;
            DrawRow("Master", audio.MasterVolume, audio.MasterMuted, audio.SetMasterVolume, audio.SetMasterMuted);
            foreach (SoundCategory category in System.Enum.GetValues(typeof(SoundCategory)))
                DrawRow(category == SoundCategory.SoundEffects ? "Sound effects" : category.ToString(),
                    audio.GetVolume(category), audio.IsMuted(category), value => audio.SetVolume(category, value), value => audio.SetMuted(category, value));
            GUILayout.Space(16);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset defaults", GUILayout.Height(34))) audio.ResetVolumes();
            if (GUILayout.Button("Done [Esc]", GUILayout.Height(34))) Hide();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.matrix = oldMatrix;
        }

        private static void DrawRow(string label, float level, bool muted, System.Action<float> setLevel, System.Action<bool> setMuted)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(40));
            GUILayout.Label(label, GUILayout.Width(96));
            float next = GUILayout.HorizontalSlider(level, 0f, 1f, GUILayout.Width(185));
            if (!Mathf.Approximately(next, level)) setLevel(next);
            GUILayout.Label(Mathf.RoundToInt(next * 100) + "%", GUILayout.Width(42));
            bool nextMuted = GUILayout.Toggle(muted, "Mute", GUILayout.Width(60));
            if (nextMuted != muted) setMuted(nextMuted);
            GUILayout.EndHorizontal();
        }
    }
}
