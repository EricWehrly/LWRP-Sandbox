using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdateableData
{
    public const int numSupportedLODs = 5;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 167, 192, 216, 240 };
    public const int numSupportedChunkSizes = 9;
    public const int numSupportedFlatShadedChunkSizes = 3;

    public float meshScale = 2.5f;

    public bool useFlatShading = false;

    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex;

    [Range(0, numSupportedFlatShadedChunkSizes - 1)]
    public int flatShadedSizeIndex;

    // number of vertices per line of a mesh rendered at highest LOD (0). 
    // Includes 2 extra vertices used for calculating normals, but excluded from final mesh.
    public int numVerticesPerLine
    {
        get
        {
            if (useFlatShading)
            {
                return supportedChunkSizes[flatShadedSizeIndex] + 5;
            }
            else
            {
                return supportedChunkSizes[chunkSizeIndex] + 5;
            }
        }
    }

    public float meshWorldSize
    {
        get
        {
            return (numVerticesPerLine - 3) * meshScale;
        }
    }
}
