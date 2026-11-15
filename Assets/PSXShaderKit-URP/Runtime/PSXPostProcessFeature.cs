using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace PSXShaderKit.URP
{
    /// <summary>
    /// Renderer feature that runs the PSX Shader Kit image effects in URP (Render Graph).
    /// Settings come from the "PSX Post Process" Volume override.
    /// </summary>
    public class PSXPostProcessFeature : ScriptableRendererFeature
    {
        public const string ShaderName = "Hidden/PSX/URP/PostProcess";

        [SerializeField] Shader shader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        [Tooltip("Temporary: logs why the PSX pass runs or is skipped (once per reason).")]
        public bool logDiagnostics = true;

        Material _material;
        PSXPostProcessPass _pass;
        static readonly HashSet<string> s_Logged = new HashSet<string>();

        internal static void Diag(bool enabled, string msg)
        {
            if (enabled && s_Logged.Add(msg))
                Debug.Log("[PSX diag] " + msg);
        }

        public override void Create()
        {
            if (shader == null)
                shader = Shader.Find(ShaderName);
            if (shader == null)
                return;

            _material = CoreUtils.CreateEngineMaterial(shader);
            _pass = new PSXPostProcessPass(_material, this) { renderPassEvent = renderPassEvent };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cam = renderingData.cameraData.camera;
            string ctx = $"camera '{cam.name}' ({renderingData.cameraData.cameraType}), feature '{name}', playing={Application.isPlaying}";

            // Self-heal if the material was destroyed (e.g. renderer disposed) or Create() never ran.
            if (_material == null || _pass == null)
            {
                Diag(logDiagnostics, $"material/pass missing -> recreating. shader={(shader ? shader.name : "NULL")} | {ctx}");
                Create();
                if (_material == null || _pass == null)
                {
                    Diag(logDiagnostics, $"SKIP: could not create material (shader missing or unsupported) | {ctx}");
                    return;
                }
            }
            if (!_material.shader.isSupported)
                Diag(logDiagnostics, $"WARNING: shader '{_material.shader.name}' is NOT supported (compile error?) | {ctx}");

            var settings = VolumeManager.instance.stack.GetComponent<PSXPostProcessVolume>();
            if (settings == null)
            {
                Diag(logDiagnostics, $"SKIP: PSXPostProcessVolume not in the volume stack | {ctx}");
                return;
            }

            // Same global the original PSXPostProcessEffect sets: enables per-object dithering in the PSX shaders.
            Shader.SetGlobalFloat(PSXPostProcessPass.Ids.ObjectDithering,
                settings.active && settings.colorEmulationMode.value == PSXColorEmulationMode.PerObject_Accurate ? 1f : 0f);

            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
                return;

            string values = $"active={settings.active} targetHeight={settings.targetHeight.value} pixelation={settings.pixelationFactor.value} mode={settings.colorEmulationMode.value} " +
                            $"ditherScale={settings.ditheringScale.value} interlace={settings.interlacingSize.value} debugTint={settings.debugTint.value}";
            if (!settings.active || !settings.NeedsFullscreenPass)
            {
                Diag(logDiagnostics, $"SKIP: settings need no fullscreen pass ({values}) | {ctx}");
                return;
            }

            Diag(logDiagnostics, $"ENQUEUE pass ({values}) | {ctx}");
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            CoreUtils.Destroy(_material);
        }
    }

    class PSXPostProcessPass : ScriptableRenderPass
    {
        internal static class Ids
        {
            public static readonly int ObjectDithering = Shader.PropertyToID("_PSX_ObjectDithering");
            public static readonly int PixelationFactor = Shader.PropertyToID("_PixelationFactor");
            public static readonly int ColorResolution = Shader.PropertyToID("_ColorResolution");
            public static readonly int DitherResolution = Shader.PropertyToID("_DitherResolution");
            public static readonly int HighResDitherMatrix = Shader.PropertyToID("_HighResDitherMatrix");
            public static readonly int DitheringScale = Shader.PropertyToID("_DitheringScale");
            public static readonly int InterlacedFrameIndex = Shader.PropertyToID("_InterlacedFrameIndex");
            public static readonly int InterlacingSize = Shader.PropertyToID("_InterlacingSize");
            public static readonly int PreviousFrame = Shader.PropertyToID("_PreviousFrame");
        }

        const int PassPixelation = 0;
        const int PassDither = 1;
        const int PassDitherAccurate = 2;
        const int PassInterlacing = 3;
        const int PassDebugTint = 4;

        class InterlacePassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle previousFrame;
        }

        readonly Material _material;
        readonly PSXPostProcessFeature _feature;
        readonly Dictionary<Camera, RTHandle> _history = new Dictionary<Camera, RTHandle>();

        public PSXPostProcessPass(Material material, PSXPostProcessFeature feature)
        {
            _material = material;
            _feature = feature;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            bool log = _feature != null && _feature.logDiagnostics;

            if (resourceData.isActiveTargetBackBuffer)
            {
                PSXPostProcessFeature.Diag(log, $"SKIP in RecordRenderGraph: active target is the backbuffer (camera '{cameraData.camera.name}')");
                return;
            }

            var v = VolumeManager.instance.stack.GetComponent<PSXPostProcessVolume>();

            TextureHandle source = resourceData.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(source);

            // Effective pixelation: a fixed number of rows (resolution independent) or a fraction of the screen.
            float pixelation = v.targetHeight.value > 0
                ? Mathf.Clamp01((float)v.targetHeight.value / Mathf.Max(1, desc.height))
                : v.pixelationFactor.value;
            float ditherScale = v.matchDitherToPixelation.value && pixelation < 1f ? pixelation : v.ditheringScale.value;

            // Render Graph executes each camera right after recording it, so setting values here is per-camera safe.
            var m = _material;
            m.SetFloat(Ids.PixelationFactor, pixelation);
            m.SetVector(Ids.ColorResolution, v.fullscreenColorDepth.value);
            m.SetVector(Ids.DitherResolution, v.fullscreenDitherDepth.value);
            m.SetFloat(Ids.HighResDitherMatrix, v.ditheringMatrixSize.value switch
            {
                PSXDitheringMatrixSize.Dither2x2 => 0f,
                PSXDitheringMatrixSize.Dither4x4 => 0.5f,
                _ => 1f
            });
            m.SetFloat(Ids.DitheringScale, ditherScale);
            m.SetFloat(Ids.InterlacedFrameIndex, Time.frameCount % 2);
            m.SetFloat(Ids.InterlacingSize, v.interlacingSize.value);

            desc.clearBuffer = false;
            desc.discardBuffer = false;
            desc.filterMode = FilterMode.Point;
            desc.msaaSamples = MSAASamples.None;

            TextureHandle current = source;
            var steps = new List<string>();

            if (pixelation < 1f)
            {
                current = AddBlit(renderGraph, "PSX Pixelation", current, desc, PassPixelation);
                steps.Add("pixelation");
            }

            switch (v.colorEmulationMode.value)
            {
                case PSXColorEmulationMode.Fullscreen_Customizable:
                    current = AddBlit(renderGraph, "PSX Dithering", current, desc, PassDither);
                    steps.Add("dither");
                    break;
                case PSXColorEmulationMode.Fullscreen_Accurate:
                    current = AddBlit(renderGraph, "PSX Dithering Accurate", current, desc, PassDitherAccurate);
                    steps.Add("ditherAccurate");
                    break;
            }

            if (v.interlacingSize.value > 0 && cameraData.cameraType == CameraType.Game)
            {
                var camera = cameraData.camera;
                _history.TryGetValue(camera, out var historyRT);
                bool reallocated = RenderingUtils.ReAllocateHandleIfNeeded(ref historyRT, desc, "PSX_InterlacingHistory");
                _history[camera] = historyRT;
                TextureHandle history = renderGraph.ImportTexture(historyRT);

                TextureHandle frame = current;
                if (!reallocated)
                {
                    current = AddInterlace(renderGraph, frame, history, desc);
                    steps.Add("interlace");
                }
                // Store this frame (pre-interlacing) for the next one, like the original effect.
                renderGraph.AddBlitPass(frame, history, Vector2.one, Vector2.zero, passName: "PSX Interlacing History");
            }

            if (v.debugTint.value)
            {
                current = AddBlit(renderGraph, "PSX Debug Tint", current, desc, PassDebugTint);
                steps.Add("debugTint");
            }

            if (steps.Count > 0)
                resourceData.cameraColor = current;

            PSXPostProcessFeature.Diag(log, $"RECORD camera '{cameraData.camera.name}' ({cameraData.cameraType}): {string.Join(" > ", steps)} | " +
                $"src {desc.width}x{desc.height} {desc.format} | pixelation={pixelation:0.###} (~{Mathf.RoundToInt(desc.height * pixelation)} rows) ditherScale={ditherScale:0.###} | postFX={cameraData.postProcessEnabled}");
        }

        TextureHandle AddBlit(RenderGraph rg, string name, TextureHandle src, TextureDesc desc, int pass)
        {
            desc.name = name;
            TextureHandle dst = rg.CreateTexture(desc);
            var p = new RenderGraphUtils.BlitMaterialParameters(src, dst, _material, pass);
            rg.AddBlitPass(p, passName: name);
            return dst;
        }

        TextureHandle AddInterlace(RenderGraph rg, TextureHandle src, TextureHandle history, TextureDesc desc)
        {
            desc.name = "PSX Interlacing";
            TextureHandle dst = rg.CreateTexture(desc);
            using (var builder = rg.AddRasterRenderPass<InterlacePassData>("PSX Interlacing", out var data))
            {
                data.material = _material;
                data.source = src;
                data.previousFrame = history;
                builder.UseTexture(src);
                builder.UseTexture(history);
                builder.SetRenderAttachment(dst, 0);
                builder.SetRenderFunc(static (InterlacePassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetTexture(Ids.PreviousFrame, (RTHandle)d.previousFrame);
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, PassInterlacing);
                });
            }
            return dst;
        }

        public void Dispose()
        {
            foreach (var rt in _history.Values)
                rt?.Release();
            _history.Clear();
        }
    }
}
