using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class NoiseUtils
{

    /// <summary>
    /// Creates a flat array of floats from Perlin Noise algorithm to be used for height maps to meshes.
    /// </summary>
    /// <param name="size">The width and height of the noise map. The result will be a size x size grid.</param>
    /// <param name="scale">Controls the zoom level of the noise. Higher values zoom out, lower values zoom in.</param>
    /// <param name="octaves">Number of layers of noise to combine. More octaves add more detail.</param>
    /// <param name="persistance">Controls how the amplitude of each octave diminishes. Lower values reduce higher frequency impact.</param>
    /// <param name="lacunarity">Controls how the frequency increases per octave. Higher values increase detail and complexity.</param>
    /// <param name="offset">Offset applied to the noise sample positions. Useful for generating different terrain chunks.</param>
    /// <returns>A NativeArray of floats representing normalized height values (0–1) for the terrain mesh.</returns>
    public static NativeArray<float> GenerateNoiseMap(int size, float scale, int octaves, float persistance, float lacunarity, float2 offset)
    {
        NativeArray<float> noiseMap = new NativeArray<float>(size * size, Allocator.Temp);
        float amplitude = 1;
        float frequency = 1;
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        float halfSize = size / 2f;
        float maxPossibleHeight = 0;

        NativeArray<float2> octaveOffsets = new NativeArray<float2>(octaves, Allocator.Temp);
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = offset.x;
            float offsetY = offset.y;
            octaveOffsets[i] = new float2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfSize) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfSize) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                int index = x + y * size;
                noiseMap[index] = noiseHeight;
            }
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = x + y * size;
                float normalizedHeight = (noiseMap[index] + 1) / (2f * maxPossibleHeight / 1.75f);
                noiseMap[index] = normalizedHeight;
            }
        }

        octaveOffsets.Dispose();

        return noiseMap;
    }
}