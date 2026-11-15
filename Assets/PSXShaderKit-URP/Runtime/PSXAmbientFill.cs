using UnityEngine;

namespace PSXShaderKit.URP
{
    /// <summary>
    /// Guaranteed ambient light for PSX/URP shaders: the darkest a surface can get away from lamps.
    /// Works without baking lighting (Unity's ambient probe only updates on "Generate Lighting").
    /// Use a cool, dark colour (e.g. navy) for a moody interior where shadows stay readable.
    /// </summary>
    [ExecuteAlways]
    public class PSXAmbientFill : MonoBehaviour
    {
        static readonly int AmbientFloor = Shader.PropertyToID("_PSX_AmbientFloor");

        [ColorUsage(false, true)]
        [Tooltip("Minimum light on every PSX surface. Brighter = less dark shadows; the hue tints the shadows.")]
        public Color ambient = new Color(0.25f, 0.28f, 0.45f);

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void OnDisable() => Shader.SetGlobalVector(AmbientFloor, Vector4.zero);

        void Apply()
        {
            if (isActiveAndEnabled)
                Shader.SetGlobalColor(AmbientFloor, ambient); // converted to linear automatically
        }
    }
}
