# Sound system

The audio manager and settings panel start automatically in every scene and survive scene changes. No scene setup or mixer wiring is required. The scene still needs an enabled **Audio Listener**, normally on its camera.

## Add a sound without code

1. Open **Tools > Audio > Sound Control** and choose **Create Sound Asset**. You can also right-click in the Project window and choose **Create > Audio > Sound**.
2. Drop one or more audio clips into **Clips**. Empty slots are ignored; multiple clips provide random variations.
3. Choose **Music**, **Sound Effects**, or **Ambience**. Set the sound's volume, pitch range, and whether it loops. For music or background ambience, usually enable **Loop**.
4. Select a scene object and click **Add Sound Player to Selected Object**, or add **Audio > Sound Player** from its Inspector and assign the asset.
5. Enable **Play On Enable** for automatic music/ambience. For triggered sounds, connect a button, UnityEvent, or animation event to **SoundPlayer.Play()**. **Stop()** stops that component's sounds.

The Sound Control window previews clips in Edit mode. During Play mode its Play button uses the game sound system, and its Live Mix sliders adjust the same saved settings as the game panel. The Stop button stops the selected sound's entire category in Play mode.

For a quick first preview, assign the included `Assets/AudioSystem/Examples/Footstep.asset` to the Sound Control window. It uses an existing project clip.

Enable **Spatial** on a sound asset for positional effects or ambience; set its minimum and maximum distance. Music always plays in 2D. With **Follow Object** enabled, the sound moves with its Sound Player and stops when that object is destroyed. To let a one-shot finish after destroying/disabling its emitter (for example a pickup), turn off both **Follow Object** and **Stop On Disable**.

## Change volume in game

Press **F10** or click **Audio [F10]** at the top right. Use separate sliders and mute buttons for Master, Music, Sound Effects, and Ambience. **Esc** or **Done** closes the panel. It pauses scaled gameplay and temporarily deactivates active Unity PlayerInput components, then restores the previous time scale, cursor state, and inputs. Audio keeps playing so you can hear adjustments. Custom controllers using raw input or unscaled updates should also check `AudioSettingsPanel.IsOpen`.

Levels and mute states save automatically via PlayerPrefs, including when closing the panel or quitting. **Reset defaults** restores Master 100%, Music 80%, Sound Effects 100%, Ambience 80%, with all channels unmuted.

Final source volume = sound asset volume × category volume × master volume. Muting preserves the slider level. The controls apply to sounds played through this system; route future audio through Sound Player or AudioManager rather than independent AudioSources. The existing third-person footsteps and landing clips already use the system.

## Play from code

```csharp
using ICanEatItAll.Audio;
using UnityEngine;

public class EatSound : MonoBehaviour
{
    [SerializeField] private SoundDefinition bite;

    public void OnEat()
    {
        // One-shot at a fixed location; safe if this object is destroyed next.
        AudioManager.Instance.Play(bite, transform.position);
    }
}
```

`Play` returns a handle. Keep it to stop that particular playback with `AudioManager.Instance.Stop(handle)`. For a moving emitter, use `Play(sound, transform.position, transform)`. `StopCategory(SoundCategory.Ambience)` and `StopAll()` are also available. Existing clip-based code can use `PlayClip(clip, position, volume)`; it defaults to spatial Sound Effects.

Only one soundtrack plays at a time. Requesting the same music asset again preserves its current playback; choosing another replaces it immediately. Repeated Sound Player requests for an already-playing loop are ignored. Music transitions are immediate, without crossfades.

The manager reuses up to 48 AudioSources. At capacity it recycles the oldest one-shot; if all voices are loops, it rejects new playback with a warning. Playback handles prevent an old Stop call from stopping a reused voice. Asset volume/pitch changes apply on the next playback; Live Mix controls affect sounds already playing immediately.

## Verification

Open **Window > General > Test Runner > PlayMode** and run `ICanEatItAll.Audio.Tests`. Tests cover live category gain and mute, music replacement, moving emitter cleanup, voice limits, stale handles, menu restoration, and saved settings. For a listening check, enter a scene with an Audio Listener, play a sound from Sound Control, and adjust the category and master sliders.
