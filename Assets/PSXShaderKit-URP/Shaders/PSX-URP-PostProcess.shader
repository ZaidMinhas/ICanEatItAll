// URP port of the PSX Shader Kit image effects (Pixelation, PostProcess, PostProcess-Accurate, Interlacing).
// Driven by PSXPostProcessFeature (renderer feature) + "PSX Post Process" Volume override.
Shader "Hidden/PSX/URP/PostProcess"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    float  _PixelationFactor;
    float3 _ColorResolution;
    float3 _DitherResolution;
    float  _HighResDitherMatrix;
    float  _DitheringScale;
    float  _InterlacedFrameIndex;
    float  _InterlacingSize;
    TEXTURE2D_X(_PreviousFrame);

    // The PS1 quantised in gamma space; do the same in Linear projects.
    half3 ToDitherSpace(half3 c)
    {
    #if !defined(UNITY_COLORSPACE_GAMMA)
        c = LinearToSRGB(saturate(c));
    #endif
        return c;
    }

    half3 FromDitherSpace(half3 c)
    {
    #if !defined(UNITY_COLORSPACE_GAMMA)
        c = SRGBToLinear(c);
    #endif
        return c;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        // 0 - Pixelation
        Pass
        {
            Name "PSX Pixelation"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 scaledRes = max(floor(_ScreenParams.xy * _PixelationFactor), 1.0);
                float2 uv = (floor(input.texcoord * scaledRes) + 0.5) / scaledRes;
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
            }
            ENDHLSL
        }

        // 1 - Fullscreen customizable color banding + dithering
        Pass
        {
            Name "PSX Dithering"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            static const float DITHER_4X4[16] =
            {
                0,  8,  2,  10,
                12, 4,  14, 6,
                3,  11, 1,  9,
                15, 7,  13, 5
            };
            static const float DITHER_4X4_PS1[16] =
            {
                -4, +0, -3, +1,
                +2, -2, +3, -1,
                -3, +1, -4, +0,
                +3, -1, +2, -2
            };
            static const float DITHER_2X2[4] = { 0, 3, 2, 1 };

            float DitherColorChannel(float color, float ditherOffset, float ditherStep)
            {
                float distance = fmod(color, ditherStep);
                float baseValue = floor(color / ditherStep) * ditherStep;
                float nudgedValue = (distance + ditherOffset * ditherStep) / ditherStep;
                return baseValue + ditherStep * floor(nudgedValue);
            }

            float DitherCol(float color, float ditherOffset, float ditherStep)
            {
                float distance = fmod(color, ditherStep);
                float baseValue = floor(color / ditherStep) * ditherStep;
                return (ditherOffset < distance / ditherStep - 0.001) ? baseValue + ditherStep : baseValue;
            }

            float GetDitherThreshold(uint2 p)
            {
                if (_HighResDitherMatrix > 0.75)
                    return DITHER_4X4_PS1[(p.x % 4) + (p.y % 4) * 4] * 0.125;
                if (_HighResDitherMatrix > 0.25)
                    return DITHER_4X4[(p.x % 4) + (p.y % 4) * 4] * 0.0625;
                return DITHER_2X2[(p.x % 2) + (p.y % 2) * 2] * 0.25;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float threshold = GetDitherThreshold((uint2)floor(input.positionCS.xy * _DitheringScale));
                float3 ditherStep = 1.0 / max(_DitherResolution, 1.0);
                float3 colorStep = 1.0 / max(_ColorResolution, 1.0);

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                half3 c = ToDitherSpace(col.rgb);

                c.r = DitherCol(c.r, 0.5, colorStep.r);
                c.g = DitherCol(c.g, 0.5, colorStep.g);
                c.b = DitherCol(c.b, 0.5, colorStep.b);

                c.r = DitherColorChannel(c.r, threshold, ditherStep.r);
                c.g = DitherColorChannel(c.g, threshold, ditherStep.g);
                c.b = DitherColorChannel(c.b, threshold, ditherStep.b);

                return half4(FromDitherSpace(saturate(c)), col.a);
            }
            ENDHLSL
        }

        // 2 - Fullscreen console-accurate 15-bit dithering
        Pass
        {
            Name "PSX Dithering Accurate"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            static const int DITHER_4X4_PS1[16] =
            {
                -4, +0, -3, +1,
                +2, -2, +3, -1,
                -3, +1, -4, +0,
                +3, -1, +2, -2
            };

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                uint2 p = (uint2)floor(input.positionCS.xy * _DitheringScale);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                int3 c = int3(round(ToDitherSpace(col.rgb) * 255.0)) + DITHER_4X4_PS1[(p.x % 4) + (p.y % 4) * 4];
                c = clamp(c, 0, 255) >> 3;
                return half4(FromDitherSpace(c / 31.0), col.a);
            }
            ENDHLSL
        }

        // 3 - Interlacing
        Pass
        {
            Name "PSX Interlacing"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                half4 prev = SAMPLE_TEXTURE2D_X(_PreviousFrame, sampler_PointClamp, input.texcoord);
                uint row = (uint)floor(input.positionCS.y / max(_InterlacingSize, 1.0));
                return (row % 2) == (uint)round(_InterlacedFrameIndex) ? prev : col;
            }
            ENDHLSL
        }

        // 4 - Debug tint (confirms the pass reaches the screen)
        Pass
        {
            Name "PSX Debug Tint"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                return half4(lerp(col.rgb, half3(1, 0, 1), 0.5), col.a);
            }
            ENDHLSL
        }
    }
}
