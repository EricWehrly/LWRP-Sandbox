using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdateableData
{
    public float noiseScale = 0.3f;

    public int octaves = 4;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    public int seed = 21;
    public Vector2 offset = new Vector2(0, 0);

    public Noise.NormalizeMode normalizeMode = Noise.NormalizeMode.Global;

    protected override void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;

        base.OnValidate();
    }
}
