// URP port of the PSX Shader Kit shader "Vertex Lit".
// Same property names as the original, so materials can be switched without losing settings.
// SubShader 1 uses a geometry shader (flat shading + triangle sorting); SubShader 2 is the
// "Lite" fallback for platforms without geometry shaders.
Shader "PSX/URP/Vertex Lit"
{
    Properties
    {
        _Color("Color (RGBA)", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
        _EmissionColor("Emission Color (RGBA)", Color) = (0,0,0,0)
        _EmissiveTex("Emissive", 2D) = "black" {}
        _CubemapColor("Cubemap Color (RGBA)", Color) = (0,0,0,0)
        _Cubemap("Cubemap", Cube) = "" {}
        _ReflectionMap("Reflection Map", 2D) = "white" {}
        _ObjectDithering("Per-Object Dithering Enable", Range(0,1)) = 1
        _FlatShading("Flat Shading", Range(0,1)) = 0
        _CustomDepthOffset("Custom Depth Offset", Float) = 0
        [Toggle(_PSX_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Main Light Shadows (URP)", Float) = 0
        [Toggle(_PSX_PIXEL_LIGHTING)] _PixelLighting("Per-Pixel Lighting (big low-poly surfaces)", Float) = 0
    }

    HLSLINCLUDE
    #define PSX_VERTEX_LIT
    #define PSX_CUBEMAP
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ PSX_ENABLE_CUSTOM_VERTEX_LIGHTING
            #pragma shader_feature_local_fragment _PSX_RECEIVE_SHADOWS
            #pragma shader_feature_local _PSX_PIXEL_LIGHTING
            #pragma multi_compile_instancing
            #pragma multi_compile _ PSX_FLAT_SHADING_MODE_CENTER
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ PSX_ENABLE_CUSTOM_VERTEX_LIGHTING
            #pragma shader_feature_local_fragment _PSX_RECEIVE_SHADOWS
            #pragma shader_feature_local _PSX_PIXEL_LIGHTING
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
