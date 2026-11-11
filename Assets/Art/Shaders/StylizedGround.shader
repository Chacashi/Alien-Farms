// Alien Farms - stylized, texture-free ground (grass / dirt / mud) with soft toon lighting.
// The pattern is generated in world space (XZ), so separate ground pieces line up seamlessly.
Shader "AlienFarms/StylizedGround"
{
    Properties
    {
        [Header(Base Colors)]
        _GrassDark ("Dark Color", Color) = (0.30, 0.55, 0.22, 1)
        _GrassLight ("Light Color", Color) = (0.52, 0.76, 0.30, 1)
        _GrassDry ("Dry Highlight Color", Color) = (0.80, 0.80, 0.42, 1)

        [Header(Patches)]
        _PatchScale ("Patch Scale (1 per m)", Range(0.01, 2)) = 0.18
        _PatchContrast ("Patch Contrast", Range(0, 4)) = 1.6
        _DryScale ("Dry Scale (1 per m)", Range(0.01, 2)) = 0.09
        _DryAmount ("Dry Amount", Range(0, 1)) = 0.25
        _DetailScale ("Detail Scale (1 per m)", Range(0.1, 20)) = 3.5
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.18

        [Header(Mowing Stripes)]
        _StripeStrength ("Stripe Strength", Range(0, 0.5)) = 0.06
        _StripeWidth ("Stripe Width (m)", Range(0.25, 10)) = 2.5
        _StripeAngle ("Stripe Angle (deg)", Range(0, 180)) = 0

        [Header(Dirt Spots)]
        _DirtColor ("Dirt Color", Color) = (0.55, 0.38, 0.22, 1)
        _DirtAmount ("Dirt Amount", Range(0, 1)) = 0.12
        _DirtScale ("Dirt Scale (1 per m)", Range(0.01, 2)) = 0.35

        [Header(Flowers or Pebbles)]
        _FlowerColorA ("Accent Color A", Color) = (1.0, 0.97, 0.90, 1)
        _FlowerColorB ("Accent Color B", Color) = (0.98, 0.55, 0.75, 1)
        _FlowerCenterColor ("Accent Center Color", Color) = (1.0, 0.82, 0.25, 1)
        _FlowerDensity ("Accent Density", Range(0, 1)) = 0.06
        _FlowerScale ("Accent Grid (1 per m)", Range(0.5, 10)) = 3
        _FlowerSize ("Accent Size", Range(0.02, 0.5)) = 0.11

        [Header(Team Tint)]
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0

        [Header(Toon Lighting)]
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.45
        _RampSmoothness ("Ramp Smoothness", Range(0.001, 0.5)) = 0.18
        _ShadowTint ("Shadow Tint", Color) = (0.48, 0.52, 0.72, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
        }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Art/Shaders/Include/FarmNoise.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _GrassDark;
            half4 _GrassLight;
            half4 _GrassDry;
            float _PatchScale;
            half _PatchContrast;
            float _DryScale;
            half _DryAmount;
            float _DetailScale;
            half _DetailStrength;
            half _StripeStrength;
            float _StripeWidth;
            float _StripeAngle;
            half4 _DirtColor;
            half _DirtAmount;
            float _DirtScale;
            half4 _FlowerColorA;
            half4 _FlowerColorB;
            half4 _FlowerCenterColor;
            half _FlowerDensity;
            float _FlowerScale;
            half _FlowerSize;
            half4 _TintColor;
            half _TintStrength;
            half _RampThreshold;
            half _RampSmoothness;
            half4 _ShadowTint;
            half _AmbientStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Assets/Art/Shaders/Include/FarmToonLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half3 GroundAlbedo(float2 p)
            {
                // Large light/dark grass patches.
                half patches = FarmFbm3(p * _PatchScale);
                half t = saturate((patches - 0.5h) * _PatchContrast + 0.5h);
                half3 color = lerp(_GrassDark.rgb, _GrassLight.rgb, t);

                // Sun-dried areas.
                half dry = FarmFbm3(p * _DryScale + 71.3);
                color = lerp(color, _GrassDry.rgb, smoothstep(1.0h - _DryAmount, 1.15h - _DryAmount, dry) * 0.8h);

                // Fine breakup.
                half detail = FarmValueNoise(p * _DetailScale);
                color *= 1.0h + (detail - 0.5h) * _DetailStrength;

                // Mowed-lawn stripes.
                float s, c;
                sincos(radians(_StripeAngle), s, c);
                half stripe = sin(dot(p, float2(c, s)) / max(_StripeWidth, 0.01) * PI);
                color *= 1.0h + smoothstep(-0.2h, 0.2h, stripe) * 2.0h * _StripeStrength - _StripeStrength;

                // Bare dirt spots.
                half dirtNoise = FarmFbm3(p * _DirtScale + 37.1);
                half dirtMask = smoothstep(1.0h - _DirtAmount, 1.06h - _DirtAmount, dirtNoise);
                color = lerp(color, _DirtColor.rgb * (0.85h + detail * 0.3h), dirtMask);

                // Scattered flowers (pebbles on dirt materials): one candidate per grid cell.
                float2 flowerP = p * _FlowerScale;
                float2 cell = floor(flowerP);
                float2 random = FarmHash22(cell);
                float2 center = 0.25 + 0.5 * FarmHash22(cell + 5.3);
                half dist = length(frac(flowerP) - center);
                half aa = max(fwidth(dist), 1e-4h);
                half present = step(1.0h - _FlowerDensity, random.x) * (1.0h - dirtMask);
                half petals = present * (1.0h - smoothstep(_FlowerSize - aa, _FlowerSize + aa, dist));
                half core = 1.0h - smoothstep(_FlowerSize * 0.35h - aa, _FlowerSize * 0.35h + aa, dist);
                half3 flowerColor = lerp(_FlowerColorA.rgb, _FlowerColorB.rgb, step(0.5h, random.y));
                flowerColor = lerp(flowerColor, _FlowerCenterColor.rgb, core);
                color = lerp(color, flowerColor, petals);

                // Team tint: shift the hue towards the tint while keeping the luminance pattern.
                half tintLum = max(Luminance(_TintColor.rgb), 0.05h);
                half3 tinted = _TintColor.rgb * (Luminance(color) / tintLum);
                return lerp(color, tinted, _TintStrength);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 albedo = GroundAlbedo(input.positionWS.xz);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                InputData inputData = FarmCreateInputData(input.positionWS, normalWS, input.positionCS, input.fogFactor);

                half3 color = FarmToonLighting(albedo, inputData, _RampThreshold, _RampSmoothness,
                                               _ShadowTint.rgb, _AmbientStrength);
                color = MixFog(color, inputData.fogCoord);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
            #if defined(_GBUFFER_NORMALS_OCT)
                float3 normalWS = normalize(input.normalWS);
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                return half4(half3(PackFloat2To888(remappedOctNormalWS)), 0.0);
            #else
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
