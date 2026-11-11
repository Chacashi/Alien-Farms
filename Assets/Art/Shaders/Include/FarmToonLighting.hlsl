#ifndef ALIENFARMS_FARM_TOON_LIGHTING_INCLUDED
#define ALIENFARMS_FARM_TOON_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Soft toon lighting: half-lambert remapped through a smooth ramp, tinted (not black) shadows,
// spherical-harmonics ambient, SSAO and Forward / Forward+ additional lights.

half3 FarmAdditionalLight(Light light, half3 normalWS)
{
    half wrapped = saturate(dot(normalWS, light.direction) * 0.5h + 0.5h);
    return light.color * (light.distanceAttenuation * light.shadowAttenuation * wrapped * wrapped);
}

InputData FarmCreateInputData(float3 positionWS, half3 normalWS, float4 positionCS, half fogFactor)
{
    InputData inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.positionCS = positionCS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    inputData.fogCoord = fogFactor;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
    inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);
    return inputData;
}

half3 FarmToonLighting(half3 albedo, InputData inputData, half rampThreshold, half rampSmoothness,
                       half3 shadowTint, half ambientStrength)
{
    half occlusion = 1.0h;
#if defined(_SCREEN_SPACE_OCCLUSION)
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
    occlusion = aoFactor.indirectAmbientOcclusion;
#endif

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
    half wrapped = dot(inputData.normalWS, mainLight.direction) * 0.5h + 0.5h;
    half ramp = smoothstep(rampThreshold - rampSmoothness, rampThreshold + rampSmoothness, wrapped);
    half lit = ramp * mainLight.shadowAttenuation * mainLight.distanceAttenuation;

    half3 direct = mainLight.color * lerp(shadowTint, half3(1.0h, 1.0h, 1.0h), lit);
    half3 ambient = SampleSH(inputData.normalWS) * ambientStrength * occlusion;
    half3 color = albedo * (direct * lerp(1.0h, occlusion, 0.5h) + ambient);

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light directionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
        color += albedo * FarmAdditionalLight(directionalLight, inputData.normalWS);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
        color += albedo * FarmAdditionalLight(light, inputData.normalWS);
    LIGHT_LOOP_END
#endif

    return color;
}

#endif
