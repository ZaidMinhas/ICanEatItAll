// URP port of the PSX Shader Kit shader "Unlit".
// Same property names as the original, so materials can be switched without losing settings.
// SubShader 1 uses a geometry shader (flat shading + triangle sorting); SubShader 2 is the
// "Lite" fallback for platforms without geometry shaders.
Shader "PSX/URP/Unlit"
{
    Properties
    {
        _Color("Color (RGBA)", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
        _ObjectDithering("Per-Object Dithering Enable", Range(0,1)) = 1
        _CustomDepthOffset("Custom Depth Offset", Float) = 0
    }

    HLSLINCLUDE

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 4.0
            #pragma require geometry
            #pragma vertex PSXVert
            #pragma geometry PSXGeom
            #pragma fragment PSXFrag
            #define PSX_USE_GEOMETRY
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile PSX_TRIANGLE_SORT_OFF PSX_TRIANGLE_SORT_CENTER_Z PSX_TRIANGLE_SORT_CLOSEST_Z PSX_TRIANGLE_SORT_CENTER_VIEWDIST PSX_TRIANGLE_SORT_CLOSEST_VIEWDIST PSX_TRIANGLE_SORT_CUSTOM
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex PSXShadowVert
            #pragma fragment PSXShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex PSXDepthVert
            #pragma fragment PSXDepthFrag
            #pragma multi_compile_instancing
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex PSXDepthVert
            #pragma fragment PSXDepthNormalsFrag
            #pragma multi_compile_instancing
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex PSXVert
            #pragma fragment PSXFrag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex PSXShadowVert
            #pragma fragment PSXShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex PSXDepthVert
            #pragma fragment PSXDepthFrag
            #pragma multi_compile_instancing
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex PSXDepthVert
            #pragma fragment PSXDepthNormalsFrag
            #pragma multi_compile_instancing
            #include "PSX-URP-Core.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
