using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PSXShaderKit.URP
{
    public enum PSXColorEmulationMode
    {
        Off = 0,
        Fullscreen_Customizable = 1,
        Fullscreen_Accurate = 2,
        PerObject_Accurate = 3
    }

    public enum PSXDitheringMatrixSize
    {
        Dither2x2 = 0,
        Dither4x4 = 1,
        Dither4x4_PS1Pattern = 2
    }

    [Serializable]
    public sealed class PSXColorEmulationModeParameter : VolumeParameter<PSXColorEmulationMode>
    {
        public PSXColorEmulationModeParameter(PSXColorEmulationMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class PSXDitheringMatrixSizeParameter : VolumeParameter<PSXDitheringMatrixSize>
    {
        public PSXDitheringMatrixSizeParameter(PSXDitheringMatrixSize value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// URP replacement for the kit's PSXPostProcessEffect camera component (OnRenderImage does not run in URP).
    /// Add it as an override on a Volume (or in the URP default Volume Profile). Requires PSXPostProcessFeature on the renderer.
    /// </summary>
    [Serializable, VolumeComponentMenu("PSX/PSX Post Process")]
    public sealed class PSXPostProcessVolume : VolumeComponent
    {
        [Header("Resolution")]
        [Tooltip("Resolution-independent pixelation: the screen is rendered as if it had this many rows of pixels " +
                 "(PS1 = 240). 0 = use Pixelation Factor instead.")]
        public ClampedIntParameter targetHeight = new ClampedIntParameter(0, 0, 1080);

        [Tooltip("Fraction of the current resolution (used when Target Height is 0). Note: 0.25 at 4K is still 540p. 1 = off.")]
        public ClampedFloatParameter pixelationFactor = new ClampedFloatParameter(1f, 0.05f, 1f);

        [Tooltip("Scale the dither pattern so each dither dot is one fake low-res pixel. Overrides Dithering Scale.")]
        public BoolParameter matchDitherToPixelation = new BoolParameter(true);

        [Header("Color")]
        [Tooltip("Dither per-material on PSX objects (console-accurate pattern), or full-screen as a post-process.")]
        public PSXColorEmulationModeParameter colorEmulationMode = new PSXColorEmulationModeParameter(PSXColorEmulationMode.PerObject_Accurate);

        [Tooltip("Values per color channel before dithering (Fullscreen_Customizable). 256 = 24-bit color.")]
        public Vector3Parameter fullscreenColorDepth = new Vector3Parameter(new Vector3(256, 256, 256));

        [Tooltip("Values per color channel of the dithered output (Fullscreen_Customizable). 32 = PS1 15-bit color.")]
        public Vector3Parameter fullscreenDitherDepth = new Vector3Parameter(new Vector3(32, 32, 32));

        [Tooltip("4x4 has more patterns. The PS1 pattern is the most accurate.")]
        public PSXDitheringMatrixSizeParameter ditheringMatrixSize = new PSXDitheringMatrixSizeParameter(PSXDitheringMatrixSize.Dither4x4_PS1Pattern);

        [Tooltip("Scales the dither pattern so it's visible at high resolutions. Match it to Pixelation Factor.")]
        public ClampedFloatParameter ditheringScale = new ClampedFloatParameter(1f, 0.05f, 1f);

        [Header("Interlacing")]
        [Tooltip("Rows of pixels affected by interlacing. 0 = off, 1 = console-accurate (best at low resolutions).")]
        public ClampedIntParameter interlacingSize = new ClampedIntParameter(0, 0, 16);

        [Header("Debug")]
        [Tooltip("Tints the screen magenta. Use it to confirm the PSX post-process pass reaches the screen.")]
        public BoolParameter debugTint = new BoolParameter(false);

        public bool NeedsFullscreenPass =>
            debugTint.value
            || targetHeight.value > 0
            || pixelationFactor.value < 1f
            || colorEmulationMode.value == PSXColorEmulationMode.Fullscreen_Customizable
            || colorEmulationMode.value == PSXColorEmulationMode.Fullscreen_Accurate
            || interlacingSize.value > 0;
    }
}
