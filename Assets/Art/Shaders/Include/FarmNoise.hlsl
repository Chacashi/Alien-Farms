#ifndef ALIENFARMS_FARM_NOISE_INCLUDED
#define ALIENFARMS_FARM_NOISE_INCLUDED

// Cheap, texture-free hash/noise helpers shared by the Alien Farms environment shaders.

float FarmHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float2 FarmHash22(float2 p)
{
    float n = FarmHash21(p);
    return float2(n, FarmHash21(p + n + 17.17));
}

// Smooth value noise in [0, 1].
float FarmValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = FarmHash21(i);
    float b = FarmHash21(i + float2(1.0, 0.0));
    float c = FarmHash21(i + float2(0.0, 1.0));
    float d = FarmHash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Three-octave fractal value noise in [0, 1].
float FarmFbm3(float2 p)
{
    const float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8);
    float value = 0.0;
    float amplitude = 0.5;
    [unroll]
    for (int octave = 0; octave < 3; octave++)
    {
        value += amplitude * FarmValueNoise(p);
        p = mul(rot, p) * 2.03 + 11.7;
        amplitude *= 0.5;
    }
    return value / 0.875;
}

// Four-octave fractal value noise in [0, 1] (used by the sky clouds).
float FarmFbm4(float2 p)
{
    const float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8);
    float value = 0.0;
    float amplitude = 0.5;
    [unroll]
    for (int octave = 0; octave < 4; octave++)
    {
        value += amplitude * FarmValueNoise(p);
        p = mul(rot, p) * 2.03 + 11.7;
        amplitude *= 0.5;
    }
    return value / 0.9375;
}

#endif
