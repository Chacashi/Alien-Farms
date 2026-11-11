// Alien Farms - procedural farm skybox (assign the material to Lighting > Environment > Skybox Material).
// Sky gradient with a warm horizon, sun disc + halo driven by the main directional light,
// slowly drifting toon clouds and a faint ringed alien planet.
Shader "AlienFarms/FarmSky"
{
    Properties
    {
        [Header(Sky Gradient)]
        _ZenithColor ("Zenith Color", Color) = (0.24, 0.52, 0.92, 1)
        _SkyColor ("Sky Color", Color) = (0.45, 0.73, 0.97, 1)
        _HorizonColor ("Horizon Color", Color) = (1.00, 0.87, 0.68, 1)
        _GroundColor ("Below Horizon Color", Color) = (0.58, 0.68, 0.52, 1)
        _HorizonOffset ("Horizon Offset", Range(-0.3, 0.3)) = 0.02
        _HorizonBlend ("Horizon Band Width", Range(0.01, 1)) = 0.22
        _ZenithBlend ("Zenith Blend Height", Range(0.05, 1)) = 0.65

        [Header(Sun)]
        [Toggle] _UseMainLight ("Follow Main Light", Float) = 1
        _SunDirection ("Fallback Sun Direction", Vector) = (0.3, 0.55, 0.4, 0)
        _SunColor ("Sun Color", Color) = (1.0, 0.96, 0.82, 1)
        _SunSize ("Sun Size", Range(0.005, 0.3)) = 0.05
        _SunHaloSize ("Halo Size", Range(0.01, 2)) = 0.35
        _SunHaloIntensity ("Halo Intensity", Range(0, 3)) = 0.6
        _HorizonSunGlow ("Horizon Sun Glow", Range(0, 2)) = 0.5

        [Header(Clouds)]
        _CloudColor ("Cloud Color", Color) = (1.0, 1.0, 1.0, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.72, 0.79, 0.93, 1)
        _CloudScale ("Cloud Scale", Range(0.1, 6)) = 1.3
        _CloudCoverage ("Cloud Coverage Threshold", Range(0, 1)) = 0.56
        _CloudSoftness ("Cloud Edge Softness", Range(0.005, 0.4)) = 0.05
        _CloudCurvature ("Cloud Dome Curvature", Range(0.01, 2)) = 0.3
        _CloudWind ("Cloud Wind (XY)", Vector) = (0.012, 0.004, 0, 0)
        _CloudOpacity ("Cloud Opacity", Range(0, 1)) = 0.95

        [Header(Alien Planet)]
        _PlanetDirection ("Planet Direction", Vector) = (-0.6, 0.42, 0.68, 0)
        _PlanetSize ("Planet Size", Range(0.01, 0.6)) = 0.11
        _PlanetColorA ("Planet Color A", Color) = (0.66, 0.50, 0.95, 1)
        _PlanetColorB ("Planet Color B", Color) = (0.48, 0.90, 0.78, 1)
        _PlanetBands ("Planet Bands", Range(0, 20)) = 7
        _PlanetRingColor ("Ring Color", Color) = (0.92, 0.86, 1.0, 1)
        _PlanetRingAngle ("Ring Angle (deg)", Range(-90, 90)) = -22
        _PlanetOpacity ("Planet Opacity", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Skybox"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "FarmSky"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Assets/Art/Shaders/Include/FarmNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _SkyColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                float _HorizonOffset;
                float _HorizonBlend;
                float _ZenithBlend;
                float _UseMainLight;
                float4 _SunDirection;
                half4 _SunColor;
                float _SunSize;
                float _SunHaloSize;
                half _SunHaloIntensity;
                half _HorizonSunGlow;
                half4 _CloudColor;
                half4 _CloudShadowColor;
                float _CloudScale;
                float _CloudCoverage;
                float _CloudSoftness;
                float _CloudCurvature;
                float4 _CloudWind;
                half _CloudOpacity;
                float4 _PlanetDirection;
                float _PlanetSize;
                half4 _PlanetColorA;
                half4 _PlanetColorB;
                float _PlanetBands;
                half4 _PlanetRingColor;
                float _PlanetRingAngle;
                half _PlanetOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionWS = input.positionOS.xyz;
                return output;
            }

            float3 GetSunDirection()
            {
                float3 fallback = normalize(_SunDirection.xyz + float3(0.0, 1e-4, 0.0));
                float3 mainLight = _MainLightPosition.xyz;
                float useMain = _UseMainLight * step(0.01, dot(mainLight, mainLight));
                return normalize(lerp(fallback, mainLight, useMain));
            }

            half3 SkyGradient(float3 dir, float3 sunDir)
            {
                float y = dir.y - _HorizonOffset;
                half3 sky = lerp(_SkyColor.rgb, _ZenithColor.rgb, smoothstep(0.0, _ZenithBlend, y));
                half horizon = 1.0h - smoothstep(0.0, _HorizonBlend, abs(y));

                // Warm the horizon more on the sun's side.
                float sunAzimuth = saturate(dot(normalize(dir.xz + 1e-5), normalize(sunDir.xz + 1e-5)) * 0.5 + 0.5);
                half3 horizonColor = _HorizonColor.rgb * (1.0h + _HorizonSunGlow * sunAzimuth * sunAzimuth * 0.35h);

                half3 color = lerp(sky, horizonColor, horizon * horizon);
                half below = smoothstep(0.0, -0.25, y);
                return lerp(color, _GroundColor.rgb, below);
            }

            // Returns rgb = planet color, a = coverage.
            half4 AlienPlanet(float3 dir, float3 sunDir)
            {
                float3 planetDir = normalize(_PlanetDirection.xyz + float3(0.0, 1e-4, 0.0));
                float3 right = normalize(cross(float3(0.0, 1.0, 0.0), planetDir));
                float3 up = cross(planetDir, right);

                float facing = dot(dir, planetDir);
                float2 p = float2(dot(dir, right), dot(dir, up)) / _PlanetSize;
                float r2 = dot(p, p);
                float aa = max(fwidth(sqrt(r2)), 1e-4);

                half disc = (1.0h - smoothstep(1.0 - aa, 1.0 + aa, sqrt(r2))) * step(0.0, facing);
                float z = sqrt(saturate(1.0 - r2));
                float3 sphereNormal = right * p.x + up * p.y - planetDir * z;
                half light = saturate(dot(-sphereNormal, sunDir) * 0.5 + 0.6);

                half bandNoise = FarmValueNoise(p * 3.0 + 4.0);
                half bands = sin((p.y + bandNoise * 0.25) * _PlanetBands) * 0.5h + 0.5h;
                half3 planet = lerp(_PlanetColorA.rgb, _PlanetColorB.rgb, smoothstep(0.3h, 0.7h, bands));
                planet *= light;
                planet += (1.0h - z) * 0.35h * _PlanetColorB.rgb; // atmosphere rim

                // Tilted ring: ellipse in the rotated planet plane.
                float s, c;
                sincos(radians(_PlanetRingAngle), s, c);
                float2 q = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                float ringRadius = length(float2(q.x, q.y / 0.28));
                float ringAa = max(fwidth(ringRadius), 1e-4);
                half ring = smoothstep(1.35 - ringAa, 1.35 + ringAa, ringRadius) * (1.0h - smoothstep(1.95 - ringAa, 1.95 + ringAa, ringRadius));
                ring *= 0.65h + 0.35h * sin(ringRadius * 28.0);
                ring *= step(0.0, facing);
                half ringBehind = step(0.0, q.y) * disc; // back half of the ring is hidden by the planet
                ring *= 1.0h - ringBehind;

                half3 color = lerp(planet, _PlanetRingColor.rgb, ring * (1.0h - disc * step(0.0, q.y)));
                half coverage = saturate(disc + ring * 0.8h);
                return half4(color, coverage);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 dir = normalize(input.directionWS);
                float3 sunDir = GetSunDirection();

                half3 color = SkyGradient(dir, sunDir);
                half aboveHorizon = smoothstep(-0.02, 0.04, dir.y - _HorizonOffset);

                // Sun halo + disc.
                float sunDistance = length(dir - sunDir);
                half halo = exp(-sunDistance / max(_SunHaloSize, 1e-3)) * _SunHaloIntensity;
                float sunAa = max(fwidth(sunDistance), 1e-4);
                half disc = 1.0h - smoothstep(_SunSize - sunAa, _SunSize + sunAa, sunDistance);
                color += _SunColor.rgb * halo * aboveHorizon;

                // Alien planet, faint in the daytime sky.
                half4 planet = AlienPlanet(dir, sunDir);
                color = lerp(color, planet.rgb, planet.a * _PlanetOpacity * aboveHorizon);

                color = lerp(color, _SunColor.rgb * 1.6h, disc * aboveHorizon);

                // Clouds projected on a dome.
                float2 cloudUv = dir.xz / (max(dir.y, 0.0) + _CloudCurvature) * _CloudScale + _Time.y * _CloudWind.xy;
                float density = FarmFbm4(cloudUv);
                float densityTowardsSun = FarmFbm4(cloudUv + normalize(sunDir.xz + 1e-5) * 0.06);
                half cover = smoothstep(_CloudCoverage, _CloudCoverage + _CloudSoftness, density);
                half lit = smoothstep(-0.02h, 0.03h, density - densityTowardsSun + 0.01h);
                half3 cloud = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, lit);
                cloud += _SunColor.rgb * halo * 0.4h;
                half cloudFade = smoothstep(0.0, 0.25, dir.y);
                color = lerp(color, cloud, cover * cloudFade * _CloudOpacity);

                // Dither against gradient banding.
                color += (FarmHash21(input.positionCS.xy) - 0.5h) / 255.0h;
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
