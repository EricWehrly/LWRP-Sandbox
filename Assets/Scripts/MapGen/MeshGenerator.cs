using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = levelOfDetail * 2;
        int verticesPerLine = meshSettings.numVerticesPerLine;
        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;

        if (levelOfDetail == 0) skipIncrement = 1;

        MeshData meshData = new MeshData(verticesPerLine, skipIncrement, meshSettings.useFlatShading);
        int[,] vertexIndicesMap = new int[verticesPerLine, verticesPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;

        for (int z = 0; z < verticesPerLine; z++)
        {
            for (int x = 0; x < verticesPerLine; x ++)
            {
                bool isOutOfMeshVertex = z == 0 || z == verticesPerLine - 1 || x == 0 || x == verticesPerLine - 1;
                bool isSkippedVertex = MeshGenerator.isSkippedVertex(skipIncrement, verticesPerLine, z, x);

                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, z] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, z] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int z = 0; z < verticesPerLine; z ++)
        {
            for (int x = 0; x < verticesPerLine; x ++)
            {
                bool isSkippedVertex = MeshGenerator.isSkippedVertex(skipIncrement, verticesPerLine, z, x);

                if (!isSkippedVertex)
                {
                    bool isOutOfMeshVertex = z == 0 || z == verticesPerLine - 1 || x == 0 || x == verticesPerLine - 1;
                    bool isMeshEdgeVertex = (z == 1 || z == verticesPerLine - 2 || x == 1 || x == verticesPerLine - 2)
                        && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (z - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (z == 2 || z == verticesPerLine - 3 || x == 2 || x == verticesPerLine - 3)
                        && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, z];
                    Vector2 percent = new Vector2(x - 1, z - 1) / (verticesPerLine - 3);
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;
                    float height = heightMap[x, z];

                    if(isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == verticesPerLine - 3;
                        int distanceToMainVertexA = isVertical ? z : x;
                        distanceToMainVertexA = (distanceToMainVertexA - 2) % skipIncrement;
                        int distanceToMainVertexB = skipIncrement - distanceToMainVertexA;
                        float distancePercentFromAToB = distanceToMainVertexA / (float)skipIncrement;

                        float heightMainVertexA = heightMap[(isVertical) ? x : x - distanceToMainVertexA,
                            (isVertical) ? z - distanceToMainVertexA : z];
                        float heightMainVertexB = heightMap[(isVertical) ? x : x + distanceToMainVertexB,
                            (isVertical) ? z + distanceToMainVertexB : z];

                        height = heightMainVertexA * (1 - distancePercentFromAToB) + heightMainVertexB * distancePercentFromAToB;
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    bool shouldCreateTriangle = x < verticesPerLine - 1 && z < verticesPerLine - 1 &&
                        (!isEdgeConnectionVertex || (x != 2 && z != 2));

                    if(shouldCreateTriangle)
                    {
                        int currentIncrement = 1;
                        if(isMainVertex && x != verticesPerLine - 3 && z != verticesPerLine -3)
                        {
                            currentIncrement = skipIncrement;
                        }


                        int a = vertexIndicesMap[x, z];
                        int b = vertexIndicesMap[x + currentIncrement, z];
                        int c = vertexIndicesMap[x, z + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, z + currentIncrement];

                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        meshData.ProcessMesh();

        return meshData;
    }

    private static bool isSkippedVertex(int skipIncrement, int verticesPerLine, int z, int x)
    {
        return x > 2 && x < verticesPerLine - 3 && z > 2 && z < verticesPerLine - 3
                            && ((x - 2) % skipIncrement != 0 || (z - 2) % skipIncrement != 0);
    }
}
