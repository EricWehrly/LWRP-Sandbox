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

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] outOfMeshVertices;
    int[] outOfMeshTriangles;

    int triangleIndex;
    int outOfMeshTriangleIndex;

    bool useFlatShading;

    public MeshData(int verticesPerLine, int skipIncrement, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;

        int numMeshEdgeVertices = (verticesPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertcies = (skipIncrement - 1) * (verticesPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (verticesPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertcies + numMainVertices];
        uvs = new Vector2[vertices.Length];

        int numMeshEdgeTriangles = 8 * (verticesPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        triangles = new int[numMeshEdgeTriangles + numMainTriangles * 3];

        outOfMeshVertices = new Vector3[verticesPerLine * 4 - 4];
        outOfMeshTriangles = new int[24 * (verticesPerLine - 2)];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if(vertexIndex < 0)
        {
            outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        } else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int vertexA, int vertexB, int vertexC)
    {
        if (vertexA < 0 || vertexB < 0 || vertexC < 0)
        {
            outOfMeshTriangles[outOfMeshTriangleIndex] = vertexA;
            outOfMeshTriangles[outOfMeshTriangleIndex + 1] = vertexB;
            outOfMeshTriangles[outOfMeshTriangleIndex + 2] = vertexC;

            outOfMeshTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = vertexA;
            triangles[triangleIndex + 1] = vertexB;
            triangles[triangleIndex + 2] = vertexC;

            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? outOfMeshVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? outOfMeshVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? outOfMeshVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh() {
        if(useFlatShading)
        {
            FlatShading();
        } else
        {
            BakeNormals();
        }
    }

    void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = bakedNormals;

        if(useFlatShading)
        {
            mesh.RecalculateNormals();
        } else
        {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}
