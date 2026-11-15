// URP port of PSX Shader Kit (PSX-Utils.cginc + PSX-ShaderSrc.cginc).
//
// Feature defines (set in the .shader before including this file):
//   PSX_VERTEX_LIT        - vertex lighting + emission
//   PSX_CUBEMAP           - cubemap reflection (_Cubemap/_CubemapColor/_ReflectionMap)
//   PSX_CUTOUT            - alpha clip against _Cutoff
//   PSX_USE_GEOMETRY      - geometry-shader path (flat shading + triangle sorting).
//                           Without it the "Lite" path is used (no geometry shaders, e.g. Metal).
//
// Globals come from the kit's own PSXShaderManager / PSXPostProcess, so they work unchanged.

#ifndef PSX_URP_CORE_INCLUDED
#define PSX_URP_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ---------------------------------------------------------------------------
// Globals (PSXShaderManager.cs)
// ---------------------------------------------------------------------------
float _PSX_GridSize;
float _PSX_DepthDebug;
float _PSX_LightingNormalFactor;
float _PSX_TextureWarpingFactor;
float _PSX_TextureWarpingMode;
float _PSX_LightFalloffPercent;
float _PSX_FlatShadingMode;     // 0 = average light, 1 = center light
float _PSX_VertexWobbleMode;    // 0 = view space, 1 = clip space
float _PSX_ObjectDithering;     // set by the PSX post-process (per-object dithering mode)
float4 _PSX_AmbientFloor;      // set by PSXAmbientFill (guaranteed minimum ambient, independent of the baked ambient probe)

// ---------------------------------------------------------------------------
// Material (same property names as the original kit)
// ---------------------------------------------------------------------------
TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
TEXTURE2D(_EmissiveTex);    SAMPLER(sampler_EmissiveTex);
TEXTURE2D(_ReflectionMap);  SAMPLER(sampler_ReflectionMap);
TEXTURECUBE(_Cubemap);      SAMPLER(sampler_Cubemap);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _EmissiveTex_ST;
    half4  _Color;
    half4  _EmissionColor;
    half4  _CubemapColor;
    half   _Cutoff;
    half   _ObjectDithering;
    half   _FlatShading;
    float  _CustomDepthOffset;
CBUFFER_END

// ---------------------------------------------------------------------------
// Math / snapping / affine
// ---------------------------------------------------------------------------
float PSX_InvLerp(float from, float to, float value)
{
    return (value - from) / (to - from);
}

float3 PSX_SnapVertexToGrid(float3 v)
{
    return _PSX_GridSize < 0.00001 ? v : (floor(v * _PSX_GridSize) / _PSX_GridSize);
}

float4 PSX_SnapAndProject(float3 positionVS)
{
    float4 viewSnapped = TransformWViewToHClip(PSX_SnapVertexToGrid(positionVS));
    float4 clipSnapped = TransformWViewToHClip(positionVS);
    clipSnapped.xy = PSX_SnapVertexToGrid(clipSnapped.xyz).xy;
    return lerp(viewSnapped, clipSnapped, _PSX_VertexWobbleMode);
}

float4 PSX_CalculateAffineUV(float3 positionVS, float2 uv)
{
    float affineFactor = _PSX_TextureWarpingMode < 0.5
        ? length(positionVS)
        : max(TransformWViewToHClip(positionVS).w, 0.1);
    affineFactor = lerp(1.0, affineFactor, _PSX_TextureWarpingFactor);
    return float4(uv * affineFactor, affineFactor, 0);
}

// ---------------------------------------------------------------------------
// Dithering (PS1 4x4 pattern, 15-bit output). Done in gamma space so it also
// looks right in Linear colour-space projects.
// ---------------------------------------------------------------------------
static const int PSX_DITHER_4X4[16] =
{
    -4, +0, -3, +1,
    +2, -2, +3, -1,
    -3, +1, -4, +0,
    +3, -1, +2, -2
};

half3 PSX_DitherColor(half3 color, uint2 pixel)
{
#if !defined(UNITY_COLORSPACE_GAMMA)
    color = LinearToSRGB(saturate(color));
#endif
    int3 c = int3(round(saturate(color) * 255.0)) + PSX_DITHER_4X4[(pixel.x % 4) + (pixel.y % 4) * 4];
    c = clamp(c, 0, 255) >> 3;
    color = c / 31.0;
#if !defined(UNITY_COLORSPACE_GAMMA)
    color = SRGBToLinear(color);
#endif
    return color;
}

// ---------------------------------------------------------------------------
// Lighting
// ---------------------------------------------------------------------------
#if defined(PSX_VERTEX_LIT)

void PSX_GetLightData(int index, out float4 position, out half3 color, out half4 attenuation, out half4 spotDir)
{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    position    = _AdditionalLightsBuffer[index].position;
    color       = _AdditionalLightsBuffer[index].color.rgb;
    attenuation = _AdditionalLightsBuffer[index].attenuation;
    spotDir     = _AdditionalLightsBuffer[index].spotDirection;
#else
    position    = _AdditionalLightsPosition[index];
    color       = _AdditionalLightsColor[index].rgb;
    attenuation = _AdditionalLightsAttenuation[index];
    spotDir     = _AdditionalLightsSpotDir[index];
#endif
}

// One URP additional light, evaluated at a vertex.
half3 PSX_ShadeAdditionalLight(int lightIndex, float3 positionWS, half3 normalWS)
{
#if defined(PSX_ENABLE_CUSTOM_VERTEX_LIGHTING)
    // Kit's "retro" lighting: linear falloff over the light's range.
    float4 lightPos; half3 color; half4 atten; half4 spotDir;
    PSX_GetLightData(lightIndex, lightPos, color, atten, spotDir);

    float3 toLight = lightPos.xyz - positionWS * lightPos.w;
    float dist = max(length(toLight), 0.000001);
    half3 dir = toLight / dist;
    half diff = saturate(dot(normalWS, dir));

    if (lightPos.w < 0.5) // directional
        return color * diff;

    float range = rsqrt(max(atten.x, 0.000001));            // URP stores 1/range^2
    half a = saturate(PSX_InvLerp(range, range * _PSX_LightFalloffPercent, dist));
    a *= saturate(dot(spotDir.xyz, dir) * atten.z + atten.w); // spot cone (1 for point lights)
    return color * a * lerp(1.0, diff, _PSX_LightingNormalFactor);
#else
    // Standard URP attenuation, Lambert diffuse.
    Light light = GetAdditionalPerObjectLight(lightIndex, positionWS);
    return light.color * (saturate(dot(normalWS, light.direction)) * light.distanceAttenuation);
#endif
}

float2 PSX_ClipToNormalizedScreenUV(float4 positionCS)
{
    float2 uv = (positionCS.xy / max(positionCS.w, 0.00001)) * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
    TransformNormalizedScreenUV(uv);
#endif
    return clamp(uv, 0.0, 0.9999);
}

// Main light is returned separately so it can be shadowed per-pixel.
void PSX_VertexLighting(float3 positionWS, half3 normalWS, out half3 mainLighting, out half3 otherLighting)
{
    Light mainLight = GetMainLight();
    mainLighting = mainLight.color * (saturate(dot(normalWS, mainLight.direction)) * mainLight.distanceAttenuation);

    otherLighting = max(SampleSH(normalWS), _PSX_AmbientFloor.rgb);

#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
  #if USE_CLUSTER_LIGHT_LOOP
    // Forward+: extra directional lights, then the cluster this vertex falls into.
    for (uint d = 0; d < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); d++)
        otherLighting += PSX_ShadeAdditionalLight(d, positionWS, normalWS);

    float4 positionCS = TransformWorldToHClip(positionWS);
    InputData inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.normalizedScreenSpaceUV = PSX_ClipToNormalizedScreenUV(positionCS);
    uint pixelLightCount = 0;
    LIGHT_LOOP_BEGIN(pixelLightCount)
        otherLighting += PSX_ShadeAdditionalLight(lightIndex, positionWS, normalWS);
    LIGHT_LOOP_END
  #else
    // Forward: per-object light list (like the Built-in "Vertex" light mode).
    uint count = GetAdditionalLightsCount();
    for (uint i = 0; i < count; i++)
        otherLighting += PSX_ShadeAdditionalLight(GetPerObjectLightIndex(i), positionWS, normalWS);
  #endif
#endif
}

#endif // PSX_VERTEX_LIT

// ---------------------------------------------------------------------------
// Triangle sorting (geometry path). Inputs are world-space triangle vertices.
// ---------------------------------------------------------------------------
float3 PSX_NudgeView(float3 positionWS)
{
    float3 vs = TransformWorldToView(positionWS);
    vs += normalize(vs) * _CustomDepthOffset;
    vs.z = min(vs.z, -0.0001);
    return vs;
}

float PSX_LinearToBufferDepth(float d01)
{
#if UNITY_REVERSED_Z
    return saturate(1.0 - d01);
#else
    return saturate(d01);
#endif
}

float GetTriangleSortingDepth_CenterDepth(float3 v1, float3 v2, float3 v3)
{
    float4 cs = TransformWViewToHClip(PSX_NudgeView((v1 + v2 + v3) / 3.0));
    return saturate(cs.z / cs.w);
}

float GetTriangleSortingDepth_ClosestVertexDepth(float3 v1, float3 v2, float3 v3)
{
    float4 c1 = TransformWViewToHClip(PSX_NudgeView(v1));
    float4 c2 = TransformWViewToHClip(PSX_NudgeView(v2));
    float4 c3 = TransformWViewToHClip(PSX_NudgeView(v3));
    float depth = 100;
    depth = lerp(depth, min(depth, c1.z / c1.w), step(0, c1.w));
    depth = lerp(depth, min(depth, c2.z / c2.w), step(0, c2.w));
    depth = lerp(depth, min(depth, c3.z / c3.w), step(0, c3.w));
    return saturate(depth);
}

float GetTriangleSortingDepth_LinearClosestVertexDistance(float3 v1, float3 v2, float3 v3)
{
    float d = max(length(PSX_NudgeView(v1)), max(length(PSX_NudgeView(v2)), length(PSX_NudgeView(v3))));
    return PSX_LinearToBufferDepth(d * _ProjectionParams.w);
}

float GetTriangleSortingDepth_LinearCenterDistance(float3 v1, float3 v2, float3 v3)
{
    float d = length(PSX_NudgeView((v1 + v2 + v3) / 3.0));
    return PSX_LinearToBufferDepth(d * _ProjectionParams.w);
}

// Custom template - edit freely.
float GetTriangleSortingDepth_Custom(float3 v1, float3 v2, float3 v3)
{
    float4 cs = TransformWorldToHClip((v1 + v2 + v3) / 3.0);
    return saturate(cs.z / cs.w);
}

#if defined(PSX_TRIANGLE_SORT_CENTER_Z)
    #define PSX_TRIANGLE_SORTING_FUNC(v1, v2, v3) GetTriangleSortingDepth_CenterDepth(v1, v2, v3)
#elif defined(PSX_TRIANGLE_SORT_CLOSEST_Z)
    #define PSX_TRIANGLE_SORTING_FUNC(v1, v2, v3) GetTriangleSortingDepth_ClosestVertexDepth(v1, v2, v3)
#elif defined(PSX_TRIANGLE_SORT_CENTER_VIEWDIST)
    #define PSX_TRIANGLE_SORTING_FUNC(v1, v2, v3) GetTriangleSortingDepth_LinearCenterDistance(v1, v2, v3)
#elif defined(PSX_TRIANGLE_SORT_CLOSEST_VIEWDIST)
    #define PSX_TRIANGLE_SORTING_FUNC(v1, v2, v3) GetTriangleSortingDepth_LinearClosestVertexDistance(v1, v2, v3)
#else
    #define PSX_TRIANGLE_SORTING_FUNC(v1, v2, v3) GetTriangleSortingDepth_Custom(v1, v2, v3)
#endif

#if defined(PSX_USE_GEOMETRY) && !defined(PSX_TRIANGLE_SORT_OFF)
    #define PSX_WRITE_CUSTOM_DEPTH 1
#endif

// ---------------------------------------------------------------------------
// Forward pass
// ---------------------------------------------------------------------------
struct PSXAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PSXVaryings
{
    float4 positionCS    : SV_POSITION;
    float4 affineUV1     : TEXCOORD0;
    float4 affineUV2     : TEXCOORD1;
    float3 positionWS    : TEXCOORD2;
    half4  otherLight    : TEXCOORD3;  // rgb = ambient + additional lights, a = fog factor
    half3  mainLight     : TEXCOORD4;
    float3 reflectionDir : TEXCOORD5;
    float  customDepth   : TEXCOORD6;
    half3  normalWS      : TEXCOORD7;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// _PSX_PIXEL_LIGHTING: evaluate the same PSX lighting per pixel instead of per vertex.
// Meant for big, low-poly surfaces (floors, walls) where vertex lighting can't show light pools.
#if defined(PSX_VERTEX_LIT) && !defined(_PSX_PIXEL_LIGHTING)
    #define PSX_LIGHT_PER_VERTEX 1
#endif

// Builds the final varyings for one vertex (snap, affine UVs, fog, reflection).
PSXVaryings PSX_BuildVaryings(float3 positionWS, half3 normalWS, float2 uv, half3 mainLight, half3 otherLight)
{
    PSXVaryings o = (PSXVaryings)0;
    float3 positionVS = TransformWorldToView(positionWS);

    o.positionCS = PSX_SnapAndProject(positionVS);
    o.positionWS = positionWS;
    o.normalWS = normalWS;
    o.affineUV1 = PSX_CalculateAffineUV(positionVS, TRANSFORM_TEX(uv, _MainTex));
    o.affineUV2 = PSX_CalculateAffineUV(positionVS, TRANSFORM_TEX(uv, _EmissiveTex));
    o.mainLight = mainLight;
    o.otherLight = half4(otherLight, ComputeFogFactor(o.positionCS.z));
#if defined(PSX_CUBEMAP)
    o.reflectionDir = reflect(positionWS - GetCameraPositionWS(), normalWS);
#endif
    return o;
}

#if defined(PSX_USE_GEOMETRY)

struct PSXVertexOut
{
    float4 positionCS : SV_POSITION; // unused by the rasterizer; geometry stage rebuilds it
    float3 positionWS : TEXCOORD0;
    half3  normalWS   : TEXCOORD1;
    float2 uv         : TEXCOORD2;
    half3  mainLight  : TEXCOORD3;
    half3  otherLight : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

PSXVertexOut PSXVert(PSXAttributes v)
{
    PSXVertexOut o = (PSXVertexOut)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.uv = v.uv;
#if defined(PSX_LIGHT_PER_VERTEX)
    PSX_VertexLighting(o.positionWS, o.normalWS, o.mainLight, o.otherLight);
#endif
    return o;
}

[maxvertexcount(3)]
void PSXGeom(triangle PSXVertexOut IN[3], inout TriangleStream<PSXVaryings> stream)
{
#if defined(PSX_WRITE_CUSTOM_DEPTH)
    float triDepth = PSX_TRIANGLE_SORTING_FUNC(IN[0].positionWS, IN[1].positionWS, IN[2].positionWS);
#else
    float triDepth = 0;
#endif

#if defined(PSX_LIGHT_PER_VERTEX)
    half3 avgMain  = (IN[0].mainLight  + IN[1].mainLight  + IN[2].mainLight)  / 3.0;
    half3 avgOther = (IN[0].otherLight + IN[1].otherLight + IN[2].otherLight) / 3.0;
    #if defined(PSX_FLAT_SHADING_MODE_CENTER)
    {
        float3 center = (IN[0].positionWS + IN[1].positionWS + IN[2].positionWS) / 3.0;
        half3 faceN = normalize(cross(IN[1].positionWS - IN[0].positionWS, IN[2].positionWS - IN[0].positionWS));
        // Keep the face normal on the same side as the vertex normals
        faceN *= sign(dot(faceN, IN[0].normalWS + IN[1].normalWS + IN[2].normalWS) + 0.0001);
        PSX_VertexLighting(center, faceN, avgMain, avgOther);
    }
    #endif
#endif

    [unroll]
    for (int i = 0; i < 3; i++)
    {
        half3 mainL = 0, otherL = 0;
#if defined(PSX_LIGHT_PER_VERTEX)
        mainL  = lerp(IN[i].mainLight,  avgMain,  _FlatShading);
        otherL = lerp(IN[i].otherLight, avgOther, _FlatShading);
#endif
        PSXVaryings o = PSX_BuildVaryings(IN[i].positionWS, IN[i].normalWS, IN[i].uv, mainL, otherL);
        o.customDepth = triDepth;
        UNITY_TRANSFER_INSTANCE_ID(IN[i], o);
        UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[i], o);
        stream.Append(o);
    }
    stream.RestartStrip();
}

#else // Lite path (no geometry shader)

PSXVaryings PSXVert(PSXAttributes v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
    half3 normalWS = TransformObjectToWorldNormal(v.normalOS);
    half3 mainL = 0, otherL = 0;
#if defined(PSX_LIGHT_PER_VERTEX)
    PSX_VertexLighting(positionWS, normalWS, mainL, otherL);
#endif
    PSXVaryings o = PSX_BuildVaryings(positionWS, normalWS, v.uv, mainL, otherL);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    return o;
}

#endif

half4 PSXFrag(PSXVaryings i
#if defined(PSX_WRITE_CUSTOM_DEPTH)
    , out float outDepth : SV_Depth
#endif
    ) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uv = i.affineUV1.xy / i.affineUV1.z;
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;

#if defined(PSX_CUTOUT)
    clip(color.a - _Cutoff);
#endif

#if defined(PSX_VERTEX_LIT)
    half3 mainLight = i.mainLight;
    half3 otherLight = i.otherLight.rgb;
    #if defined(_PSX_PIXEL_LIGHTING)
    PSX_VertexLighting(i.positionWS, normalize(i.normalWS), mainLight, otherLight);
    #endif

    half shadow = 1;
    #if defined(_PSX_RECEIVE_SHADOWS)
    shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(i.positionWS));
    #endif
    color.rgb *= otherLight + mainLight * shadow;

    float2 uv2 = i.affineUV2.xy / i.affineUV2.z;
    color.rgb += SAMPLE_TEXTURE2D(_EmissiveTex, sampler_EmissiveTex, uv2).rgb * _EmissionColor.rgb * _EmissionColor.a;
#endif

#if defined(PSX_CUBEMAP)
    color.rgb += SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, i.reflectionDir).rgb
               * _CubemapColor.rgb * _CubemapColor.a
               * SAMPLE_TEXTURE2D(_ReflectionMap, sampler_ReflectionMap, uv).rgb;
#endif

    color.rgb = MixFog(color.rgb, i.otherLight.a);

    if (_PSX_ObjectDithering * _ObjectDithering > 0.5)
        color.rgb = PSX_DitherColor(color.rgb, (uint2)i.positionCS.xy);

#if defined(PSX_WRITE_CUSTOM_DEPTH)
    outDepth = i.customDepth;
    color.rgb = lerp(color.rgb, i.customDepth.xxx, _PSX_DepthDebug);
#endif

    return color;
}

// ---------------------------------------------------------------------------
// Depth / DepthNormals / ShadowCaster (no geometry shader needed)
// ---------------------------------------------------------------------------
struct PSXDepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    half3  normalWS   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

PSXDepthVaryings PSXDepthVert(PSXAttributes v)
{
    PSXDepthVaryings o = (PSXDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS = PSX_SnapAndProject(TransformWorldToView(positionWS)); // match forward-pass wobble
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    return o;
}

void PSX_DepthClip(float2 uv)
{
#if defined(PSX_CUTOUT)
    clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * _Color.a - _Cutoff);
#endif
}

half PSXDepthFrag(PSXDepthVaryings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    PSX_DepthClip(i.uv);
    return i.positionCS.z;
}

half4 PSXDepthNormalsFrag(PSXDepthVaryings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    PSX_DepthClip(i.uv);
    return half4(NormalizeNormalPerPixel(i.normalWS), 0);
}

float3 _LightDirection;
float3 _LightPosition;

PSXDepthVaryings PSXShadowVert(PSXAttributes v)
{
    PSXDepthVaryings o = (PSXDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDir = normalize(_LightPosition - positionWS);
#else
    float3 lightDir = _LightDirection;
#endif
    o.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir)));
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

half4 PSXShadowFrag(PSXDepthVaryings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    PSX_DepthClip(i.uv);
    return 0;
}

#endif // PSX_URP_CORE_INCLUDED
