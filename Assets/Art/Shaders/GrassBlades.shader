// Alien Farms - stylized grass blades with world-space wind, distance shrink and soft toon lighting.
// Expected mesh layout (written by Tools/Alien Farms/Bake Grass Field):
//   UV0.y  = 0 at the root, 1 at the tip (bend weight).  UV0.x = across the blade.
//   UV1.xyz = blade root position in object space (wind pivot + distance shrink).
//   COLOR.g = per-clump random, COLOR.b = per-blade random.
Shader "AlienFarms/GrassBlades"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Root Color", Color) = (0.20, 0.42, 0.16, 1)
        _TipColor ("Tip Color", Color) = (0.58, 0.80, 0.32, 1)
        _TipColorAlt ("Tip Color (Patches)", Color) = (0.78, 0.84, 0.42, 1)
        _PatchScale ("Patch Scale (1 per m)", Range(0.01, 2)) = 0.18
        _ColorVariation ("Per Blade Variation", Range(0, 1)) = 0.2
        _RootDarkening ("Root Darkening", Range(0, 1)) = 0.35

        [Header(Team Tint)]
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0

        [Header(Wind)]
        _WindDirection ("Wind Direction (XZ)", Vector) = (1, 0, 0.35, 0)
        _WindStrength ("Wind Lean", Range(0, 1.5)) = 0.35
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.2
        _WindFrequency ("Wave Frequency (1 per m)", Range(0.01, 2)) = 0.25
        _GustScale ("Gust Scale (1 per m)", Range(0.005, 1)) = 0.06
        _GustStrength ("Gust Strength", Range(0, 1)) = 0.6
        _FlutterStrength ("Flutter", Range(0, 0.5)) = 0.08

        [Header(Shape and Distance)]
        _HeightScale ("Height Scale", Range(0.1, 3)) = 1
        _FadeStart ("Shrink Start (m)", Float) = 22
        _FadeEnd ("Shrink End (m)", Float) = 38

        [Header(Toon Lighting)]
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.4
        _RampSmoothness ("Ramp Smoothness", Range(0.001, 0.5)) = 0.25
        _ShadowTint ("Shadow Tint", Color) = (0.48, 0.55, 0.72, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.55
        _Translucency ("Back Light Translucency", Range(0, 2)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "DisableBatching" = "False"
        }
        LOD 200
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Art/Shaders/Include/FarmNoise.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _TipColor;
            half4 _TipColorAlt;
            float _PatchScale;
            half _ColorVariation;
            half _RootDarkening;
            half4 _TintColor;
            half _TintStrength;
            float4 _WindDirection;
            float _WindStrength;
            float _WindSpeed;
            float _WindFrequency;
            float _GustScale;
            float _GustStrength;
            float _FlutterStrength;
            float _HeightScale;
            float _FadeStart;
            float _FadeEnd;
            half _RampThreshold;
            half _RampSmoothness;
            half4 _ShadowTint;
            half _AmbientStrength;
            half _Translucency;
        CBUFFER_END

        struct GrassAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            float3 rootOS : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // Returns the displaced world position; outputs the root in world space.
        float3 GrassDisplacedPositionWS(GrassAttributes input, out float3 rootWS)
        {
            rootWS = TransformObjectToWorld(input.rootOS);
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

            // Height scale and distance shrink (collapsed blades produce no fragments).
            float distanceToCamera = distance(rootWS, GetCameraPositionWS());
            float fade = saturate((_FadeEnd - distanceToCamera) / max(_FadeEnd - _FadeStart, 0.01));
            positionWS = rootWS + (positionWS - rootWS) * (_HeightScale * fade);

            float bladeLength = max(positionWS.y - rootWS.y, 0.0);
            float bend = input.uv.y;

            float2 windDir = normalize(_WindDirection.xz + float2(1e-5, 0.0));
            float time = _Time.y * _WindSpeed;
            float gust = FarmValueNoise(rootWS.xz * _GustScale - windDir * time * 0.6);
            float wave = sin((dot(rootWS.xz, windDir) * _WindFrequency - time * 0.5) * TWO_PI + input.color.b * 1.7);
            float lean = _WindStrength * (1.0 + (gust - 0.5) * 2.0 * _GustStrength) * (0.65 + 0.35 * wave);
            float flutter = sin(time * 6.0 + input.color.b * 40.0) * _FlutterStrength;

            float3 offset = (float3(windDir.x, 0.0, windDir.y) * lean + float3(-windDir.y, 0.0, windDir.x) * flutter)
                            * bladeLength * bend;
            positionWS += offset;
            positionWS.y -= dot(offset, offset) * 0.5 / max(bladeLength, 0.01);
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

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

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 colorData : TEXCOORD2; // x = height 0..1, y = clump rnd, z = blade rnd, w = fog
                float2 rootXZ : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(GrassAttributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 rootWS;
                float3 positionWS = GrassDisplacedPositionWS(input, rootWS);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.colorData = half4(input.uv.y, input.color.g, input.color.b, ComputeFogFactor(output.positionCS.z));
                output.rootXZ = rootWS.xz;
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half height = input.colorData.x;

                // Tip color follows the same world-space patches as AlienFarms/StylizedGround.
                half patch = smoothstep(0.35h, 0.75h, FarmFbm3(input.rootXZ * _PatchScale));
                half3 tip = lerp(_TipColor.rgb, _TipColorAlt.rgb, patch);
                half3 albedo = lerp(_BaseColor.rgb, tip, smoothstep(0.0h, 0.9h, height));
                albedo *= 1.0h + (input.colorData.z - 0.5h) * _ColorVariation + (input.colorData.y - 0.5h) * _ColorVariation * 0.5h;
                albedo *= lerp(1.0h - _RootDarkening, 1.0h, saturate(height * 2.0h));

                half tintLum = max(Luminance(_TintColor.rgb), 0.05h);
                albedo = lerp(albedo, _TintColor.rgb * (Luminance(albedo) / tintLum), _TintStrength);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS) * IS_FRONT_VFACE(isFrontFace, 1.0h, -1.0h);
                // Bias towards up so blades shade like the ground under them.
                normalWS = normalize(lerp(normalWS, half3(0.0h, 1.0h, 0.0h), 0.6h));

                InputData inputData = FarmCreateInputData(input.positionWS, normalWS, input.positionCS, input.colorData.w);
                half3 color = FarmToonLighting(albedo, inputData, _RampThreshold, _RampSmoothness,
                                               _ShadowTint.rgb, _AmbientStrength);

                // Sun shining through the blade tips when looking towards the light.
                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                half backLight = pow(saturate(dot(-inputData.viewDirectionWS, mainLight.direction)), 4.0h);
                color += tip * mainLight.color * (backLight * _Translucency * height * mainLight.shadowAttenuation);

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

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(GrassAttributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 rootWS;
                float3 positionWS = GrassDisplacedPositionWS(input, rootWS);
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

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(GrassAttributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 rootWS;
                output.positionCS = TransformWorldToHClip(GrassDisplacedPositionWS(input, rootWS));
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

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVert(GrassAttributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 rootWS;
                output.positionCS = TransformWorldToHClip(GrassDisplacedPositionWS(input, rootWS));
                output.normalWS = normalize(lerp(TransformObjectToWorldNormal(input.normalOS), float3(0.0, 1.0, 0.0), 0.6));
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
